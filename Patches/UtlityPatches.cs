using HarmonyLib;
using UnityEngine;
#if LEGACY
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#endif

namespace SuperliminalTools.Patches;

#if LEGACY
[HarmonyPatch(typeof(PlayerLerpMantle._LerpPlayer_d__12), "MoveNext")]
public class LerpPlayerMantlePatch
{
    static void Prefix(PlayerLerpMantle._LerpPlayer_d__12 __instance)
    {
        if(__instance.__1__state == 0)
            Debug.Log(Time.time + ": LerpPlayer()");
    }
}
#else
[HarmonyPatch(typeof(PlayerLerpMantle), "LerpPlayer")]

public class LerpPlayerMantlePatch
{
    static void Prefix()
    {
        Debug.Log(Time.time + ": LerpPlayer()");
    }
}
#endif

[HarmonyPatch(typeof(SaveAndCheckpointManager), "_SaveGame", [typeof(CheckPoint)])]
public class SaveGamePatch
{
    public static CheckPoint currentCheckpoint;

    static void Prefix(CheckPoint checkpoint)
    {
        Debug.Log(Time.time + ": _SaveGame() " + checkpoint?.transform.parent.name);
        currentCheckpoint = checkpoint;
    }
}

/// <summary>
/// Force every loading screen to be the "normal" one.
/// </summary>
#if LEGACY
[HarmonyPatch(typeof(LevelInformation), nameof(LevelInformation.Start))]
public class NormalLoadingScreensPatch
{
    static void Postfix(LevelInformation __instance)
    {
#if HAS_LEVELINFO
        LevelInfo levelInfo = __instance.LevelInfo;
#else
        LevelInformation levelInfo = __instance;
#endif
        for (int i = 0; i < levelInfo.RandomLoadingScreens.Count; i++)
        {
            levelInfo.RandomLoadingScreens[i] = levelInfo.NormalLoadingScreen;
        }
    }
}
#else
[HarmonyPatch(typeof(LevelInformation), nameof(LevelInformation.GetLoadingSceneIndex))]
[HarmonyPatch([typeof(string), typeof(int?)])]
public class NormalLoadingScreensPatch
{
    static void Prefix(string scenePath, ref int? debugOverride)
    {
        debugOverride = -1;
    }
}
#endif

[HarmonyPatch(typeof(FMODUnity.StudioEventEmitter), "OnEnable")]
public class DisableAlarmSoundPatch
{
    static void Prefix(FMODUnity.StudioEventEmitter __instance)
    {
        if (__instance.name == "AlarmSound")
        {
            GameObject.Destroy(__instance.gameObject);
        }
        return;
    }
}


#if HAS_WARNING_CONTROLLER
[HarmonyPatch(typeof(WarningController), "Start")]
public class DisableWarningScreenPatch
{
    static void Prefix()
    {
        AccessTools.Field(typeof(WarningController), "ShowedWarning").SetValue(null, true);
    }
}
#endif

[HarmonyPatch(typeof(PauseMenu), "OnApplicationFocus")]
public class DontPauseOnLostFocusPatch
{
#if LEGACY
    static bool Prefix(bool focus)
    {
        // Only run when focus is true
        return focus;
    }
#else
    static void Prefix(PauseMenu __instance)
    {
        __instance.pauseWhenAltTabbed = false;
    }
#endif
}

#if LEGACY
/// <summary>
/// Legacy loads checkpoints async by default, this forces a sync load
/// like in modern game versions.
/// </summary>
[HarmonyPatch(typeof(SaveAndCheckpointManager), nameof(SaveAndCheckpointManager.ResetToLastCheckpoint))]
public class LegacyResetCheckpointPatch
{
    static bool Prefix(SaveAndCheckpointManager __instance)
    {
        if (__instance.lastSaveGameState != null)
        {
            GameObject gameObject = GameObject.Find("UI_PAUSE_MENU");
            if (gameObject)
            {
                gameObject.transform.Find("Canvas").gameObject.transform.Find("ResettingToCheckpoint").gameObject.GetComponent<Text>().enabled = true;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
        }
        return false;
    }
}

/// <summary>
///  Hot Coffee Mod spams Debug.LogError in menu/loadingscreens.
///  This filters that message out.
/// </summary>
[HarmonyPatch(typeof(UnityEngine.Debug))]
[HarmonyPatch(nameof(Debug.LogError), new[] { typeof(Il2CppSystem.Object) })]
public class HotCofeeErrorPatch
{
    private const string SuppressedMessage = "No save name found for current scene.";

    private static bool Prefix(Il2CppSystem.Object message)
    {
        // Only suppress if the message is a string and matches exactly
        if (message != null && message.ToString() == SuppressedMessage)
        {
            return false; // skip original => don't print
        }

        return true;
    }
}
#endif
