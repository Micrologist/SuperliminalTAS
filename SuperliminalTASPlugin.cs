using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SuperliminalTAS.Demo;
using SuperliminalTAS.Patches;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
#if LEGACY
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
#endif

namespace SuperliminalTAS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
#if LEGACY
public class SuperliminalTASPlugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Register custom MonoBehaviour types with IL2CPP before they can be used
        ClassInjector.RegisterTypeInIl2Cpp<DemoRecorder>();
        ClassInjector.RegisterTypeInIl2Cpp<DemoHUD>();
        ClassInjector.RegisterTypeInIl2Cpp<ColliderVisualizer>();
        ClassInjector.RegisterTypeInIl2Cpp<PathProjector>();

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        TimeManagerPatcher.Patch(Process.GetCurrentProcess());

        // Create a persistent GameObject that survives scene transitions
        var go = new GameObject("SuperliminalTAS");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<DemoRecorder>();
        go.AddComponent<DemoHUD>();
    }
}
#else
public class SuperliminalTASPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        TimeManagerPatcher.Patch(Process.GetCurrentProcess());
        this.gameObject.AddComponent<DemoRecorder>();
        this.gameObject.AddComponent<DemoHUD>();
    }
}
#endif
