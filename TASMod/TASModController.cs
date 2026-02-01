using SuperliminalTools.TASMod.Demo;
using UnityEngine;
using SuperliminalTools.Components;

#if LEGACY
using System;
using UnityEngine.Events;
#else
using HarmonyLib;
#endif

namespace SuperliminalTools.TASMod;

public sealed class TASModController : MonoBehaviour
{
#if LEGACY
    public TASModController(IntPtr ptr) : base(ptr) { }
#endif

    public static TASModController Instance { get; private set; }

    private bool _unlimitedRenderDistance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate TASModController");
            return;
        }

        if (DemoRecorder.Instance == null)
        {
            gameObject.AddComponent<DemoRecorder>();
        }
    }

    private void Update()
    {
        HandleHotkeys();
        SetHotkeyText();
    }


    private void SetHotkeyText()
    {
        var output = "";

        var detailString = HUDController.Instance.ShowLess ? "Show More" : "Show Less";

        output += "F1  " + detailString + "\n";

        if (!HUDController.Instance.ShowLess)
        {
            output += "F2  View Distance\n";
            output += "F3  Gizmos\n";

            switch (DemoRecorder.Instance.State)
            {
                case PlaybackState.Recording:
                    output += "F5  Stop\nF7  Reset CP\n";
                    break;
                case PlaybackState.Playing:
                    output += "F5  Stop\n";
                    break;
                case PlaybackState.Stopped:
                    output += "F4  NoClip\n";
                    output += "F5  Play\nF6  Record\nF7  Record from CP\n";
                    output += "F11 Open\nF12 Save\n";
                    break;
            }

            output += "+/- Speed Up/Down";
        }

        HUDController.Instance.HotkeyLines = output;
    }


    #region Hotkeys

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            HUDController.Instance.ShowLess = !HUDController.Instance.ShowLess;
        }

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

        if (NoClipController.Instance.NoClipEnabled)
        {
            NoClipController.Instance.ChangeSpeed(Input.mouseScrollDelta.y);
        }
    }

    #endregion
}
