using SuperliminalTAS.MemUtil;
using System;
using System.Diagnostics;

namespace SuperliminalTAS.Patches;

internal static class TimeManagerPatcher
{
    public static void Patch(Process proc)
    {
        var unityPlayerPtr = IntPtr.Zero;
        foreach (ProcessModule module in proc.Modules)
        {
            if (module.FileName.Contains("UnityPlayer"))
            {
                unityPlayerPtr = module.BaseAddress;
            }
        }

        // This nasty hack will convince the TimeManager that Time.fixedTimeStep amount of time has elapsed
        // between the previous frame and this frame.
        // Effectively this should (and in most cases does!) force Unity to run Update() and FixedUpdate() in perfect sync.

        /**
			 * UnityPlayer.dll+500 - E8 0B377B00           - call UnityPlayer.GetTimeSinceStartup
			 * UnityPlayer.dll+505 - F2 0F10 43 70         - movsd xmm0,[rbx+70]
			 * UnityPlayer.dll+50A - F2 0F58 83 E8000000   - addsd xmm0,[rbx+000000E8]
			 * UnityPlayer.dll+512 - F3 0F5A 53 48         - cvtss2sd xmm2,[rbx+48]
			 * UnityPlayer.dll+517 - F2 0F58 C2            - addsd xmm0,xmm2
			 * UnityPlayer.dll+51B - E9 52595500           - jmp UnityPlayer.TimeManager::Update+32
		**/
        var codeCavePtr = unityPlayerPtr + 0x500;
        proc.VirtualProtect(codeCavePtr, 0x128, MemPageProtect.PAGE_EXECUTE_READWRITE);
        var code = StrToBytes("E8 0B 37 7B 00 F2 0F 10 43 70 F2 0F 58 83 E8 00 00 00 F3 0F 5A 53 48 F2 0F 58 C2 E9 52 59 55 00");
        proc.WriteBytes(codeCavePtr, code);

        /**
			 * UnityPlayer.TimeManager::Update+2D - E8 9EDD2500           - call UnityPlayer.GetTimeSinceStartup
			 * 
			 * REPLACED BY
			 * 
			 * UnityPlayer.TimeManager::Update+2D - E9 8EA6AAFF           - jmp UnityPlayer.dll+500
		**/
        var detourPtr = unityPlayerPtr + 0x555E6D;
        var jumpCode = StrToBytes("E9 8E A6 AA FF");
        proc.WriteBytes(detourPtr, jumpCode);
    }

    private static byte[] StrToBytes(string input)
    {
        string[] byteStringArray = input.Split(' ');
        byte[] output = new byte[byteStringArray.Length];
        for (int i = 0; i < byteStringArray.Length; i++)
        {
            output[i] = byte.Parse(byteStringArray[i], System.Globalization.NumberStyles.HexNumber);
        }
        return output;
    }
}
