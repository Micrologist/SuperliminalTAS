using BepInEx;
using HarmonyLib;
using SFB;
using SuperliminalTAS.src.MemUtil;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Application = UnityEngine.Application;

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

