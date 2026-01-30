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

#if LEGACY
        /**
         * UnityPlayer.dll+500 - E8 FB F4 77 00       - call UnityPlayer.GetTimeSinceStartup
         * ...
         * Code cave at UnityPlayer.dll+500, detour from UnityPlayer.dll+53F43C
        **/
        var codeCavePtr = unityPlayerPtr + 0x500;
        proc.VirtualProtect(codeCavePtr, 0x128, MemPageProtect.PAGE_EXECUTE_READWRITE);
        var code = StrToBytes("E8 FB F4 77 00 F2 0F 10 43 70 F2 0F 58 83 E0 00 00 00 F3 0F 5A 53 48 F2 0F 58 C2 E9 22 EF 53 00");
        proc.WriteBytes(codeCavePtr, code);

        var detourPtr = unityPlayerPtr + 0x53F43C;
        var jumpCode = StrToBytes("E9 BF 10 AC FF");
        proc.WriteBytes(detourPtr, jumpCode);
#else
        /**
         * UnityPlayer.dll+500 - E8 0B377B00           - call UnityPlayer.GetTimeSinceStartup
         * ...
         * Code cave at UnityPlayer.dll+500, detour from UnityPlayer.dll+555E6D
        **/
        var codeCavePtr = unityPlayerPtr + 0x500;
        proc.VirtualProtect(codeCavePtr, 0x128, MemPageProtect.PAGE_EXECUTE_READWRITE);
        var code = StrToBytes("E8 0B 37 7B 00 F2 0F 10 43 70 F2 0F 58 83 E8 00 00 00 F3 0F 5A 53 48 F2 0F 58 C2 E9 52 59 55 00");
        proc.WriteBytes(codeCavePtr, code);

        var detourPtr = unityPlayerPtr + 0x555E6D;
        var jumpCode = StrToBytes("E9 8E A6 AA FF");
        proc.WriteBytes(detourPtr, jumpCode);
#endif
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
