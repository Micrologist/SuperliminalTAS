#if HAS_VO_PLAYER

using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SuperliminalTools.Patches;

/// <summary>
/// Proxy class for making labyrinth voice lines run synced to game time.
/// Only required in non-legacy versions.
/// </summary>
internal static class VoiceOverProxy
{
    public static readonly Dictionary<string, int> VoicelineDurations = new()
    {
        // Labyrinth
        ["VoiceTrigger_Current_13A"] = 887, // 17.74s
        ["VoiceTrigger_Current_13B"] = 744, // 14.88s
        ["VoiceTrigger_Current_13C"] = 556, // 11.12s
        ["VoiceTrigger_Current_13D"] = 703, // 14.06s
        ["VoiceTrigger_Current_13E"] = 716, // 14.32s
    };

    internal static Dictionary<string, int> VoicelineEndFrames = [];
}

[HarmonyPatch(typeof(MOSTEventVOPlayer), "StartVOandSubtitles")]
public class VoiceOverStartProxyPatch
{
    static void Prefix(MOSTEventVOPlayer __instance)
    {
        if (VoiceOverProxy.VoicelineDurations.TryGetValue(__instance.name, out int frameDuration))
        {
            var endFrame = Time.renderedFrameCount + frameDuration;

            if (VoiceOverProxy.VoicelineEndFrames.ContainsKey(__instance.name))
            {
                VoiceOverProxy.VoicelineEndFrames[__instance.name] = endFrame;
            }
            else
            {
                VoiceOverProxy.VoicelineEndFrames.Add(__instance.name, Time.renderedFrameCount + frameDuration);
            }

            Debug.Log(
                $"{__instance.name} started at {Time.timeSinceLevelLoad} will run for {frameDuration} frames via proxy."
            );

        }
        else
        {
            Debug.Log($"{__instance.name} started at {Time.timeSinceLevelLoad} has no proxy and will run in real time!");
        }
    }
}

[HarmonyPatch(typeof(MOSTEventVOPlayer), nameof(MOSTEventVOPlayer.HasEnded))]
public class VoiceOverHasEndedProxyPatch
{
    static void Postfix(MOSTEventVOPlayer __instance, ref bool __result)
    {
        if (!__instance.VOBeganPlaying || !VoiceOverProxy.VoicelineEndFrames.TryGetValue(__instance.name, out int endFrame))
        {
            return;
        }
        if (Time.renderedFrameCount >= endFrame)
        {
            __result = true;
            VoiceOverProxy.VoicelineEndFrames.Remove(__instance.name);

            Debug.Log($"{__instance.name} proxy finished at {Time.timeSinceLevelLoad}.");
        }
        else
        {
            __result = false;
        }
    }
}
#endif
