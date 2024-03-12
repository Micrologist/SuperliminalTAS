using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace SuperliminalTAS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SuperliminalTASPlugin : BaseUnityPlugin
{
    private void Awake()
    {
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
		Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

		this.gameObject.AddComponent<DemoRecording>();
    }
}
