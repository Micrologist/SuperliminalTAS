using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SuperliminalTAS.Patches;

#region Rewired Input Patches

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

[HarmonyPatch]
public class RewiredDeltaTimePatch
{
    private static FieldInfo fieldInfo;

    static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            "Rewired.ReInput+YADQJtjjsJnFpIRXsWZbJAPaLFd+WJEYbcppSteIUfAbygiJwLxOmigk:AgiDzcfIDplWkWrlRVaBMFfZJjUH");
    }

    static void Postfix()
    {
        if (fieldInfo == null)
        {
            fieldInfo = AccessTools.Field(typeof(Rewired.ReInput), "unscaledDeltaTime");
        }

        fieldInfo?.SetValue(null, Time.fixedDeltaTime);
    }
}
#endregion

#region RNG Patches

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

[HarmonyPatch(typeof(System.Random), "InternalSample")]
public class InternalSamplePatch
{
    static void Postfix(ref int __result)
    {
        __result = 0;
    }
}

#endregion

[HarmonyPatch(typeof(PauseMenu), "OnApplicationFocus")]
public class ApplicationFocusPatch
{
    static void Prefix(bool focus, PauseMenu __instance)
    {
        __instance.pauseWhenAltTabbed = false;
    }
}

[HarmonyPatch(typeof(PlayerLerpMantle), "LerpPlayer")]
public class LerpPlayerMantlePatch
{
    static void Prefix()
    {
        Debug.Log(Time.time + ": LerpPlayer()");
    }
}

[HarmonyPatch(typeof(SaveAndCheckpointManager), "_SaveGame", [typeof(CheckPoint)])]
public class SaveGamePatch
{
    public static CheckPoint lastCheckpoint;

    static void Prefix(CheckPoint checkpoint)
    {
        Debug.Log(Time.time + ": _SaveGame() " + checkpoint?.name);
        lastCheckpoint = checkpoint;
    }
}

[HarmonyPatch(typeof(UnityEngine.Time), nameof(UnityEngine.Time.time))]
[HarmonyPatch(MethodType.Getter)]
public class TimeTimePatch
{
    static void Postfix(ref float __result)
    {
        __result = Time.timeSinceLevelLoad;
    }
}

[HarmonyPatch(typeof(LevelInformation), nameof(LevelInformation.GetLoadingSceneIndex))]
[HarmonyPatch([typeof(string), typeof(int)])]
public class LoadingScenePatch
{
    static void Prefix(string scenePath, ref int? debugOverride)
    {
        debugOverride = -1;
    }
}

[HarmonyPatch(typeof(FMODUnity.StudioEventEmitter),"OnEnable")]
public class EventEmitterPlayPatch
{
    static void Prefix(FMODUnity.StudioEventEmitter __instance)
    {
        if(__instance.name == "AlarmSound")
        {
            GameObject.Destroy(__instance.gameObject);
        }
        return;
    }
}

[HarmonyPatch(typeof(UnityEngine.Collider), nameof(UnityEngine.Collider.enabled))]
[HarmonyPatch(MethodType.Setter)]
public class ColliderEnabledPatch
{
    static void Prefix(ref bool value, UnityEngine.Collider __instance)
    {
        var name = __instance.gameObject.name;
        if (name == "SecretTriggerObject") return;
        //Debug.Log($"{Time.time}: {name} collider enabled set to {value}");
    }
}

[HarmonyPatch(typeof(UnityEngine.Collider), nameof(UnityEngine.Collider.isTrigger))]
[HarmonyPatch(MethodType.Setter)]
public class ColliderIsTriggerPatch
{
    static void Prefix(ref bool value, UnityEngine.Collider __instance)
    {
        //Debug.Log($"{Time.time}: {__instance.gameObject.name} isTrigger set to {value}");
    }
}

[HarmonyPatch(typeof(UnityEngine.Rigidbody), nameof(UnityEngine.Rigidbody.isKinematic))]
[HarmonyPatch(MethodType.Setter)]
public class GBKinematicPatch
{
    static void Prefix(ref bool value, UnityEngine.Rigidbody __instance)
    {
        //Debug.Log($"{Time.time}: {__instance.gameObject.name} isKinematic set to {value}");
    }
}