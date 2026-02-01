using HarmonyLib;
using UnityEngine;

namespace SuperliminalTools.Patches;

[HarmonyPatch(typeof(Random), nameof(Random.onUnitSphere))]
[HarmonyPatch(MethodType.Getter)]
public class OnUnitSpherePatch
{
    static void Postfix(ref Vector3 __result)
    {
        __result = Vector3.up;
    }
}

[HarmonyPatch(typeof(Random), nameof(Random.insideUnitSphere))]
[HarmonyPatch(MethodType.Getter)]
public class InUnitSpherePatch
{
    static void Postfix(ref Vector3 __result)
    {
        __result = Vector3.zero;
    }
}

[HarmonyPatch(typeof(Random), nameof(Random.value))]
[HarmonyPatch(MethodType.Getter)]
public class ValuePatch
{
    static void Postfix(ref float __result)
    {
        __result = .5f;
    }
}

[HarmonyPatch(typeof(Random), nameof(Random.Range))]
[HarmonyPatch([typeof(int), typeof(int)])]
public class RandomRangeIntPatch
{
    static void Postfix(int min, int max, ref int __result)
    {
        __result = Mathf.FloorToInt((min + max) / 2f);
    }
}

[HarmonyPatch(typeof(Random), nameof(Random.Range))]
[HarmonyPatch([typeof(float), typeof(float)])]
public class RandomRangeFloatPatch
{
    static void Postfix(float min, float max, ref float __result)
    {
        __result = (min + max) / 2f;
    }
}