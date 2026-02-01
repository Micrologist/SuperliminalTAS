using SuperliminalTools.Components;
using SuperliminalTools.Demo;
using System;
using UnityEngine;

namespace SuperliminalTools;

public sealed class PracticeModController : MonoBehaviour
{
#if LEGACY
    public PracticeModController(IntPtr ptr) : base(ptr) { }
#endif

    public static PracticeModController Instance { get; private set; }

    private bool _unlimitedRenderDistance = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate PracticeModController");
            return;
        }
    }

    private void Update()
    {
        HandleHotkeys();

        SetInfoText();
        SetHotkeyText();
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

        if (Input.GetKeyDown(KeyCode.F4))
        {
            NoClipController.Instance.ToggleNoClip();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            TeleportAndScaleController.Instance.StorePosition();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            TeleportAndScaleController.Instance.TeleportToStoredPosition();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            GameManager.GM.GetComponent<SaveAndCheckpointManager>()?.ResetToLastCheckpoint();
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            GameManager.GM.GetComponent<SaveAndCheckpointManager>()?.RestartLevel();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            FlashlightController.Instance.SetEnabled(!FlashlightController.Instance.FlashlightEnabled);
        }

        if (NoClipController.Instance.NoClipEnabled)
        {
            NoClipController.Instance.ChangeSpeed(Input.mouseScrollDelta.y);
        }
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
            output += "F4  NoClip\n";
            output += "F5  Store\n";
            output += "F6  Teleport\n";
            output += "F7  Reload CP\n";
            output += "F8  Restart Map\n";
            output += "F9  Flashlight\n";
        }

        HUDController.Instance.HotkeyLines = output;
    }
    #endregion

    private void SetInfoText()
    {
        var output = "";

        if (_unlimitedRenderDistance) output += "R ";

        if (GizmoVisibilityController.Instance.ShowGizmos) output += "G ";

        if (NoClipController.Instance.NoClipEnabled) output += "N ";

        if (FlashlightController.Instance.FlashlightEnabled) output += "F ";

        output += "\n";
        HUDController.Instance.InfoLines = output;
    }

}
