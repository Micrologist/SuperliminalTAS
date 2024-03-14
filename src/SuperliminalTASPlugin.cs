using BepInEx;
using HarmonyLib;
using SuperliminalTAS.src.MemUtil;
using System.Diagnostics;
using System.Reflection;

namespace SuperliminalTAS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SuperliminalTASPlugin : BaseUnityPlugin
{
	private void Awake()
	{
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
		Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
		TimeManagerPatcher.Patch(Process.GetCurrentProcess());
		this.gameObject.AddComponent<DemoRecorder>();
	}
}

