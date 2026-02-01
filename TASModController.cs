using System.Collections.Generic;
using System.Reflection;
using SuperliminalTAS.Demo;
using UnityEngine;
using UnityEngine.SceneManagement;
using SuperliminalTAS.Components;

#if LEGACY
using System;
using UnityEngine.Events;
#else
using HarmonyLib;
#endif

namespace SuperliminalTAS;

public sealed class TASModController : MonoBehaviour
{
#if LEGACY
    public TASModController(IntPtr ptr) : base(ptr) { }
#endif

    public static TASModController Instance { get; private set; }

    private bool _unlimitedRenderDistance;

    private void Awake()
    {
        Instance = this;

        if(RenderDistanceController.Instance == null)
        {
            gameObject.AddComponent<RenderDistanceController>();
        }

        if(NoClipController.Instance == null)
        {
            gameObject.AddComponent<NoClipController>();
        }

        if(GizmoVisibilityController.Instance == null)
        {
            gameObject.AddComponent<GizmoVisibilityController>();
        }

        if(ColliderVisualizerController.Instance == null)
        {
            gameObject.AddComponent<ColliderVisualizerController>();
        }

        if(FadeController.Instance == null)
        {
            gameObject.AddComponent<FadeController>();
        }

        if(FlashlightController.Instance == null)
        {
            gameObject.AddComponent<FlashlightController>();
        }

        if(PathProjectorController.Instance == null)
        {
            gameObject.AddComponent<PathProjectorController>();
        }

        if (TeleportAndScaleController.Instance == null)
        {
            gameObject.AddComponent<TeleportAndScaleController>();
        }
    }

    private void LateUpdate()
    {
        HandleHotkeys();
    }

    #region Hotkeys

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _unlimitedRenderDistance = !_unlimitedRenderDistance;
            RenderDistanceController.Instance.SetRenderDistance(_unlimitedRenderDistance ? 999999f : 1000f);
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            GizmoVisibilityController.Instance.ToggleGizmosVisible();
        }

        if (Input.GetKeyDown(KeyCode.F4) && DemoRecorder.Instance.State == PlaybackState.Stopped)
        {
            NoClipController.Instance.ToggleNoClip();
        }
    }

    #endregion
}
