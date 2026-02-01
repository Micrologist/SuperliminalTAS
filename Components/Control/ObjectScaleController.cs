using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SuperliminalTools.Components.Control;

class ObjectScaleController : MonoBehaviour
{
#if LEGACY
    public ObjectScaleController(System.IntPtr ptr) : base(ptr) { }
#else
    private FieldInfo _grabbedObjectField;
#endif

    public static ObjectScaleController Instance;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate ObjectScaleController");
            return;
        }

        Instance = this;

#if !LEGACY
        _grabbedObjectField = AccessTools.Field(typeof(ResizeScript), "grabbedObject");
#endif
    }

    public bool ScaleHeldObject(float factor)
    {
        var playerCamera = GameManager.GM.playerCamera;

        var resizeScript = playerCamera != null ? playerCamera.GetComponent<ResizeScript>() : null;
        if (resizeScript == null)
            return false;
#if LEGACY
        var grabbedObject = resizeScript.grabbedObject;
#else
        var grabbedObject = (GameObject)_grabbedObjectField.GetValue(resizeScript);
#endif

        if (grabbedObject != null && resizeScript.isGrabbing)
        {
#if LEGACY
            resizeScript.grabbedMinGrabDistance *= factor;
#else
            resizeScript.ScaleObject(factor);
#endif
            return true;
        }

        return false;
    }
}
