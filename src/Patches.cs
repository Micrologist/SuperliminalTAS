using HarmonyLib;
using System;
using UnityEngine;

namespace SuperliminalTAS
{
    [HarmonyPatch(typeof(Rewired.Player), nameof(Rewired.Player.GetButton))]
    [HarmonyPatch(new Type[] { typeof(string) })]
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

    [HarmonyPatch(typeof(Time), nameof(Time.deltaTime))]
    [HarmonyPatch(MethodType.Getter)]
    public class DeltaTimePatch
    {
        static void Postfix(ref float __result)
        {
            //__result = __result == 0.02f ? 0.02f : 0.0166f;
            //__result = 0.02f;
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
        }
    }

    [HarmonyPatch(typeof(UnityEngine.Random), nameof(UnityEngine.Random.Range))]
    [HarmonyPatch(new Type[] { typeof(float), typeof(float) })]
    public class RandomRangeFloatPatch
    {
        static void Postfix(float min, float max, ref float __result)
        {
            __result = (min + max) / 2f;
        }
    }
}
