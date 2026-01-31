using HarmonyLib;
using SuperliminalTAS.Demo;
using System.Linq;

namespace SuperliminalTAS.Patches;

/// <summary>
/// Static class that intercepts all game input polling and changes results via the hooked-up
/// DemoRecorder.
/// </summary>
internal static class TASInput
{
    public static bool blockAllInput = false;
    public static bool passthrough = true;
    public static bool disablePause = false;
    private static DemoRecorder recording;

    internal static bool GetButton(string actionName, bool originalResult)
    {
        if (blockAllInput) return false;

        return passthrough || !DemoActions.Buttons.Contains(actionName)
            ? originalResult
            : recording.GetRecordedButton(actionName);
    }
    internal static bool GetButtonDown(string actionName, bool originalResult)
    {
        if (blockAllInput) return false;

        if (actionName == "Pause" && disablePause)
            return false;

        return passthrough || !DemoActions.Buttons.Contains(actionName)
            ? originalResult
            : recording.GetRecordedButtonDown(actionName);
    }

    public static bool GetButtonUp(string actionName, bool originalResult)
    {
        if (blockAllInput) return false;

        return passthrough || !DemoActions.Buttons.Contains(actionName)
            ? originalResult
            : recording.GetRecordedButtonUp(actionName);
    }

    internal static float GetAxis(string actionName, float originalResult)
    {
        if (blockAllInput) return 0f;

        return passthrough || !DemoActions.Axes.Contains(actionName)
            ? originalResult
            : recording.GetRecordedAxis(actionName);
    }

    public static void StartPlayback(DemoRecorder recordingToPlay)
    {
        recording = recordingToPlay;
        passthrough = false;
    }

    public static void StopPlayback()
    {
        recording = null;
        passthrough = true;
    }
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButton))]
[HarmonyPatch([typeof(string)])]
public class GetButtonPatch
{
    static void Postfix(string actionName, ref bool __result)
    {
        __result = TASInput.GetButton(actionName, __result);
    }
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButtonDown))]
[HarmonyPatch([typeof(string)])]
public class GetButtonDownPatch
{
    static void Postfix(string actionName, ref bool __result)
    {
        __result = TASInput.GetButtonDown(actionName, __result);
    }
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButtonUp))]
[HarmonyPatch([typeof(string)])]
public class GetButtonUpPatch
{
    static void Postfix(string actionName, ref bool __result)
    {
        __result = TASInput.GetButtonUp(actionName, __result);
    }
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetAxis))]
[HarmonyPatch([typeof(string)])]
public class GetAxisPatch
{
    static void Postfix(string actionName, ref float __result)
    {
        __result = TASInput.GetAxis(actionName, __result);
    }
}