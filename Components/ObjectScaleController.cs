using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SuperliminalTAS.Components;


class ObjectScaleController : MonoBehaviour
{
#if LEGACY
    public ObjectScaleController(IntPtr ptr) : base(ptr) { }
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

    public void ScaleHeldObject(float factor)
    {
        var playerCamera = GameManager.GM.playerCamera;

        var resizeScript = playerCamera != null ? playerCamera.GetComponent<ResizeScript>() : null;
        if (resizeScript == null)
            return;
#if LEGACY
        var grabbedObject = resizeScript.grabbedObject;
#else
        var grabbedObject = (GameObject)_grabbedObjectField.GetValue(resizeScript);
#endif

        if (grabbedObject != null)
        {
            resizeScript.ScaleObject(factor);
        }
    }
}
