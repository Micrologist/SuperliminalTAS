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

		var extensionList = new[] {
			new SFB.ExtensionFilter("Superliminal TAS Recording (*.slt)", "slt"),
			new SFB.ExtensionFilter("All Files", "*")
		};
		/**
		StandaloneFileBrowserWindows standaloneFileBrowserWindows = new StandaloneFileBrowserWindows();
		var saveLocation = standaloneFileBrowserWindows.SaveFilePanel("Save Recording as", Application.dataPath, $"SuperliminalTAS-{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}.slt", extensionList);
		Logger.LogInfo(saveLocation);
		
		
		var file = File.OpenWrite(saveLocation.Name);

		using (StreamWriter writer = new StreamWriter(file))
		{
			writer.WriteLine("TestABC");
		}
		**/

		
	}

	private bool ConfirmOverwrite(string fileName)
	{
		return MessageBox.Show($"{fileName} already exists.\nDo you want to replace it?", "Confirm Save As",
	MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
	}
}

