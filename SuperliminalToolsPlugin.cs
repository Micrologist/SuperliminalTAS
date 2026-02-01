using BepInEx;
using HarmonyLib;
using SuperliminalTAS.Demo;
using SuperliminalTAS.Patches;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SuperliminalTAS.Components;
using UnityEngine;
#if LEGACY
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
#endif

namespace SuperliminalTAS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
#if LEGACY
public class SuperliminalToolsPlugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;

        var args = Environment.GetCommandLineArgs();
        bool practiceMode = args.Contains("--practice");

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Mode: {(practiceMode ? "Practice" : "TAS")}");

        // Register custom MonoBehaviour types with IL2CPP before they can be used
        ClassInjector.RegisterTypeInIl2Cpp<DemoRecorder>();
        ClassInjector.RegisterTypeInIl2Cpp<DemoHUD>();
        ClassInjector.RegisterTypeInIl2Cpp<TASModController>();
        ClassInjector.RegisterTypeInIl2Cpp<PracticeModController>();

        ClassInjector.RegisterTypeInIl2Cpp<PathProjector>();

        ClassInjector.RegisterTypeInIl2Cpp<RenderDistanceController>();
        ClassInjector.RegisterTypeInIl2Cpp<NoClipController>();
        ClassInjector.RegisterTypeInIl2Cpp<GizmoVisibilityController>();
        ClassInjector.RegisterTypeInIl2Cpp<ColliderVisualizer>();
        ClassInjector.RegisterTypeInIl2Cpp<ColliderVisualizerController>();
        ClassInjector.RegisterTypeInIl2Cpp<FadeController>();
        ClassInjector.RegisterTypeInIl2Cpp<FlashlightController>();
        ClassInjector.RegisterTypeInIl2Cpp<PathProjectorController>();
        ClassInjector.RegisterTypeInIl2Cpp<TeleportAndScaleController>();

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        UnityEngineTimePatcher.Patch(Process.GetCurrentProcess());

        // Create a persistent GameObject that survives scene transitions
        var go = new GameObject("SuperliminalTools");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;

        if (practiceMode)
        {
            go.AddComponent<PracticeModController>();
        }
        else
        {
            go.AddComponent<TASModController>();
            go.AddComponent<DemoRecorder>();
            go.AddComponent<DemoHUD>();
        }
    }
}
#else
public class SuperliminalToolsPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        bool practiceMode = args.Contains("--practice");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Mode: {(practiceMode ? "Practice" : "TAS")}");

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        UnityEngineTimePatcher.Patch(Process.GetCurrentProcess());

        // Create a persistent GameObject that survives scene transitions
        var go = new GameObject("SuperliminalTools");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;

        if (practiceMode)
        {
            go.AddComponent<PracticeModController>();
        }
        else
        {
            go.AddComponent<TASModController>();
            go.AddComponent<DemoRecorder>();
            go.AddComponent<DemoHUD>();
        }
    }
}
#endif
