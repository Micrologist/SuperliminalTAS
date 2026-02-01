using BepInEx;
using HarmonyLib;
using SuperliminalTools.Demo;
using SuperliminalTools.Patches;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SuperliminalTools.Components;
using UnityEngine;
#if LEGACY
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
#endif

namespace SuperliminalTools;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
#if LEGACY
public class SuperliminalToolsPlugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;

        var args = Environment.GetCommandLineArgs();
        bool tasMode = args.Contains("--tas");

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Mode: {(tasMode ? "TAS" : "Practice")}");

        // Create a persistent GameObject that survives scene transitions
        var go = new GameObject("SuperliminalTools");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;

        SuperliminalTools.Components.Utility.RegisterAllIL2CPPTypes();
        SuperliminalTools.Components.Utility.AddAllSharedComponents(go);
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        if (tasMode)
        {
            go.AddComponent<TASModController>();
            UnityEngineTimePatcher.Patch(Process.GetCurrentProcess());
            harmony.PatchAll();
        }
        else
        {
            go.AddComponent<PracticeModController>();
            harmony.PatchAll(typeof(LerpPlayerMantlePatch));
            harmony.PatchAll(typeof(SaveGamePatch));
            harmony.PatchAll(typeof(NormalLoadingScreensPatch));
            harmony.PatchAll(typeof(DisableAlarmSoundPatch));
#if HAS_WARNING_CONTROLLER
            harmony.PatchAll(typeof(DisableWarningScreenPatch));
#endif
            harmony.PatchAll(typeof(DontPauseOnLostFocusPatch));
            harmony.PatchAll(typeof(LegacyResetCheckpointPatch));
            harmony.PatchAll(typeof(HotCofeeErrorPatch));

        }
    }
}
#else
public class SuperliminalToolsPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        bool tasMode = args.Contains("--tas");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Mode: {(tasMode ? "TAS" : "Practice")}");

        // Create a persistent GameObject that survives scene transitions
        var go = new GameObject("SuperliminalTools");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;

        SuperliminalTools.Components.Utility.AddAllSharedComponents(go);
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        if (tasMode)
        {
            go.AddComponent<TASModController>();
            UnityEngineTimePatcher.Patch(Process.GetCurrentProcess());
            harmony.PatchAll();
        }
        else
        {
            go.AddComponent<PracticeModController>();
            harmony.PatchAll(typeof(LerpPlayerMantlePatch));
            harmony.PatchAll(typeof(SaveGamePatch));
            harmony.PatchAll(typeof(NormalLoadingScreensPatch));
            harmony.PatchAll(typeof(DisableAlarmSoundPatch));
#if HAS_WARNING_CONTROLLER
            harmony.PatchAll(typeof(DisableWarningScreenPatch));
#endif
            harmony.PatchAll(typeof(DontPauseOnLostFocusPatch));
        }
    }
}
#endif