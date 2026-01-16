using HarmonyLib;
using System;
using UnityEngine;

namespace SuperliminalTAS
{
	[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButton))]
	[HarmonyPatch([typeof(string)])]
	public class GetButtonPatch
	{
		static void Postfix(string actionName, ref bool __result)
		{
			__result = TASInput.GetButton(actionName, __result);
		}
	}

	[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButtonDown))]
    [HarmonyPatch([typeof(string)])]
	public class GetButtonDownPatch
	{
		static void Postfix(string actionName, ref bool __result)
		{
			__result = TASInput.GetButtonDown(actionName, __result);
		}
	}

	[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButtonUp))]
	[HarmonyPatch([typeof(string)])]
	public class GetButtonUpPatch
	{
		static void Postfix(string actionName, ref bool __result)
		{
			__result = TASInput.GetButtonUp(actionName, __result);
		}
	}

	[HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetAxis))]
	[HarmonyPatch([typeof(string)])]
	public class GetAxisPatch
	{
		static void Postfix(string actionName, ref float __result)
		{
			__result = TASInput.GetAxis(actionName, __result);
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

    [HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.insideUnitSphere))]
    [HarmonyPatch(MethodType.Getter)]
    public class InUnitSpherePatch
    {
        static void Postfix(ref Vector3 __result)
        {
            __result = Vector3.zero;
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.value))]
    [HarmonyPatch(MethodType.Getter)]
    public class ValuePatch
    {
        static void Postfix(ref float __result)
        {
			__result = .5f;
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range))]
    [HarmonyPatch([typeof(int), typeof(int)])]
	public class RandomRangeIntPatch
	{
		static void Postfix(int min, int max, ref int __result)
		{
			__result = Mathf.FloorToInt((min + max) / 2f);
		}
	}

	[HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range))]
	[HarmonyPatch([typeof(float), typeof(float)])]
	public class RandomRangeFloatPatch
	{
		static void Postfix(float min, float max, ref float __result)
		{
			__result = (min + max) / 2f;
		}
	}

    [HarmonyPatch(typeof(System.Random), "InternalSample")]
    public class InternalSamplePatch
    {
        static void Postfix(ref int __result)
        {
            __result = 0;
        }
    }

    [HarmonyPatch(typeof(PauseMenu))]
	[HarmonyPatch("OnApplicationFocus")]
	public class ApplicationFocusPatch
	{
		static void Prefix(bool focus, PauseMenu __instance)
		{
			__instance.pauseWhenAltTabbed = false;
		}
	}
}
