using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace SuperliminalTAS;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SuperliminalTAS : BaseUnityPlugin
{
    private void Awake()
    {
		Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
		Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

		this.gameObject.AddComponent<DemoRecording>();
    }

	private void Update()
	{
		Debug.Log("UPDATE");
	}

	private void FixedUpdate()
	{
		Debug.Log("FIXEDUPDATE");
	}
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButton))]
[HarmonyPatch(new Type[] {typeof(string)})]
public class GetButtonPatch
{
	static void Postfix(string actionName, ref bool __result)
	{
		__result = TASInput.GetButton(actionName, __result);
	}
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButtonDown))]
[HarmonyPatch(new Type[] { typeof(string) })]
public class GetButtonDownPatch
{
	static void Postfix(string actionName, ref bool __result)
	{
		__result = TASInput.GetButtonDown(actionName, __result);
	}
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButtonUp))]
[HarmonyPatch(new Type[] { typeof(string) })]
public class GetButtonUpPatch
{
	static void Postfix(string actionName, ref bool __result)
	{
		__result = TASInput.GetButtonUp(actionName, __result);
	}
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetAxis))]
[HarmonyPatch(new Type[] { typeof(string) })]
public class GetAxisPatch
{
	static void Postfix(string actionName, ref float __result)
	{
		__result = TASInput.GetAxis(actionName, __result);
	}
}

[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetAnyButtonDown))]
public class GetAnyButtonDownPatch
{
	static void Postfix(ref bool __result)
	{
		__result = TASInput.GetAnyButtonDown(__result);
	}
}

[HarmonyPatch(typeof(UnityEngine.Time), nameof(UnityEngine.Time.deltaTime))]
[HarmonyPatch(MethodType.Getter)]
public class DeltaTimePatch
{
	static void Postfix(ref float __result)
	{
		//__result = __result == 0.02f ? 0.02f : 0.0166f;
		__result = 0.02f;
	}
}

[HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.onUnitSphere))]
[HarmonyPatch(MethodType.Getter)]
public class OnUnitSpherePatch
{
	static void Postfix(ref Vector3 __result)
	{
		__result = Vector3.up;
	}
}

[HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range))]
[HarmonyPatch(new Type[] { typeof(int), typeof(int) })]
public class RandomRangeIntPatch
{
	static void Postfix(int min, int max, ref int __result)
	{
		__result = Mathf.FloorToInt((min + max) / 2f);
		Debug.Log($"Random.Range({min},{max}) = {__result}");
	}
}

[HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range))]
[HarmonyPatch(new Type[] { typeof(float), typeof(float) })]
public class RandomRangeFloatPatch
{
	static void Postfix(float min, float max, ref float __result)
	{
		__result = (min + max) / 2f;
		Debug.Log($"Random.Range({min},{max}) = {__result}");
	}
}



