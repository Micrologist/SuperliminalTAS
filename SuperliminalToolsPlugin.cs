using BepInEx;
using HarmonyLib;
using SuperliminalTools.Patches;
using SuperliminalTools.PracticeMod;
using SuperliminalTools.TASMod;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;

#if LEGACY
using BepInEx.Unity.IL2CPP;
#endif

namespace SuperliminalTools;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
#if LEGACY
public partial class SuperliminalToolsPlugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        Initialize();
    }
}
#else
public partial class SuperliminalToolsPlugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        Initialize();
    }
}
#endif

public partial class SuperliminalToolsPlugin
{
    private void Initialize()
    {
        var args = Environment.GetCommandLineArgs();
        bool tasMode = args.Contains("--tas");

        var targetVersion = MyPluginInfo.PLUGIN_GUID.Replace(".superliminaltools", "");

        if(Application.version.IndexOf(targetVersion) < 0)
        {
            Log.LogError($"Plugin {MyPluginInfo.PLUGIN_GUID} targets {targetVersion} but game version is {Application.version}.");
            return;
        }

        Log.LogInfo($"{MyPluginInfo.PLUGIN_GUID} loaded! Mode: {(tasMode ? "TAS" : "Practice")}");

        // Create a persistent GameObject that survives scene transitions
        var go = new GameObject("SuperliminalTools");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;

#if LEGACY
        SuperliminalTools.Components.Utility.RegisterAllIL2CPPTypes();
#endif
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
            harmony.PatchAll(typeof(NormalLoadingScreensPatch));
            harmony.PatchAll(typeof(DisableAlarmSoundPatch));
            harmony.PatchAll(typeof(DontPauseOnLostFocusPatch));
#if HAS_WARNING_CONTROLLER
            harmony.PatchAll(typeof(DisableWarningScreenPatch));
#endif
#if LEGACY
            harmony.PatchAll(typeof(LegacyResetCheckpointPatch));
            harmony.PatchAll(typeof(HotCofeeErrorPatch));
#endif
        }
    }
}