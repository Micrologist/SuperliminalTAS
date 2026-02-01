using HarmonyLib;
using Mono.Cecil.Cil;
using SuperliminalTools.MemUtil;
using System;
using System.Diagnostics;
using UnityEngine;

namespace SuperliminalTools.Patches;

/// <summary>
/// Patches the getter for UnityEngine.Time.time to return the time since the current level was loaded instead of the
/// application's total runtime.
/// This fixes desyncs related to floating point accuracy when scripts use Time.time.
/// </summary>
[HarmonyPatch(typeof(UnityEngine.Time), nameof(UnityEngine.Time.time))]
[HarmonyPatch(MethodType.Getter)]
public class TimeTimePatch
{
    static void Postfix(ref float __result)
    {
        __result = Time.timeSinceLevelLoad;
    }
}

/// <summary>
/// This nasty hack will convince the TimeManager that Time.fixedTimeStep amount of time has elapsed
/// between each frame.
/// Effectively this should (and in most cases does!) force Unity to run Update() and FixedUpdate() in perfect sync.
/// </summary>
internal static class UnityEngineTimePatcher
{
    public static void Patch(Process proc)
    {
        var unityPlayerPtr = IntPtr.Zero;
        var unityPlayerSize = 0x0;

        foreach (ProcessModule module in proc.Modules)
        {
            if (module.FileName.Contains("UnityPlayer"))
            {
                unityPlayerPtr = module.BaseAddress;
                unityPlayerSize = module.ModuleMemorySize;
            }
        }

        Int32 injectOffset = 0x0;
        byte[] jumpBytes = [];
        byte[] caveBytes = [];

        switch(unityPlayerSize)
        {
            case 0x180B000: // 1.0.2019.11.12
                injectOffset = 0x53F43C;
                jumpBytes = StrToBytes("E9 BF 10 AC FF");
                caveBytes = StrToBytes("E8 FB F4 77 00 F2 0F 10 43 70 F2 0F 58 83 E0 00 00 00 F3 0F 5A 53 48 F2 0F 58 C2 E9 22 EF 53 00");
                break;
            case 0x1865000: // 1.10.2020.7.6
                injectOffset = 0x53FAAC;
                jumpBytes = StrToBytes("E9 4F 0A AC FF");
                caveBytes = StrToBytes("E8 8B 13 78 00 F2 0F 10 43 70 F2 0F 58 83 E0 00 00 00 F3 0F 5A 53 48 F2 0F 58 C2 E9 91 F5 53 00");
                break;
            case 0x199E000: // 1.10.2020.12.10
                injectOffset = 0x54B73D;
                jumpBytes = StrToBytes("E9 BE 4D AB FF");
                caveBytes = StrToBytes("E8 FB 11 7A 00 F2 0F 10 43 70 F2 0F 58 83 E8 00 00 00 F3 0F 5A 53 48 F2 0F 58 C2 E9 22 B2 54 00");
                break;
            case 0x19D2000: // 1.10.2023.2.17
                injectOffset = 0x555E6D;
                jumpBytes = StrToBytes("E9 8E A6 AA FF");
                caveBytes = StrToBytes("E8 0B 37 7B 00 F2 0F 10 43 70 F2 0F 58 83 E8 00 00 00 F3 0F 5A 53 48 F2 0F 58 C2 E9 52 59 55 00");
                break;
            default:
                UnityEngine.Debug.LogError($"UnityEngine TimeManager was not patched because of an unknown UnityPlayer.dll version! (0x{unityPlayerSize:X})");
                break;
        }

        if (caveBytes != null)
        {
            var codeCavePtr = unityPlayerPtr + 0x500;
            proc.VirtualProtect(codeCavePtr, 0x128, MemPageProtect.PAGE_EXECUTE_READWRITE);
            proc.WriteBytes(codeCavePtr, caveBytes);

            var detourPtr = unityPlayerPtr + injectOffset;
            proc.WriteBytes(detourPtr, jumpBytes);
        }

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
