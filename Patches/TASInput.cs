using SuperliminalTAS.Demo;
using System.Linq;

namespace SuperliminalTAS.Patches;

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
