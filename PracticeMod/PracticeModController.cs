using SuperliminalTools.Components;
using SuperliminalTools.Components.Control;
using SuperliminalTools.Components.UI;
using SuperliminalTools.Components.Visual;
using UnityEngine;

namespace SuperliminalTools.PracticeMod;

public sealed class PracticeModController : MonoBehaviour
{
#if LEGACY
    public PracticeModController(System.IntPtr ptr) : base(ptr) { }
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
        var scrollInput = Input.mouseScrollDelta.y;

        if (Input.GetKey(KeyCode.RightBracket) || Input.GetKey(KeyCode.Equals))
        {
            scrollInput = 25 * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftBracket) || Input.GetKey(KeyCode.Minus))
        {
            scrollInput = -25 * Time.deltaTime;
        }

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
            if (Input.GetKey(KeyCode.LeftShift))
            {
                NoClipController.Instance.ToggleNoClipStyle();
            }
            else
            {
                NoClipController.Instance.ToggleNoClip();
            }
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
            NoClipController.Instance.ChangeSpeed(scrollInput);
        }

        if(Input.GetKey(KeyCode.LeftControl) && scrollInput != 0f)
        {
            if (!ObjectScaleController.Instance.ScaleHeldObject(1 + (-scrollInput * 0.02f)))
                TeleportAndScaleController.Instance.ScalePlayer(1 + (scrollInput * 0.01f));
        }

        if((Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Return)))
        {
            if (Input.GetKey(KeyCode.LeftControl))
                TeleportAndScaleController.Instance.SetPlayerScale(1.0f);
            else if (NoClipController.Instance.NoClipEnabled)
                NoClipController.Instance.SetSpeed(20f);
        }
    }

    #endregion


    #region UI
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
            output += "F9  Flashlight\n\n";
            if (NoClipController.Instance.NoClipEnabled)
            {
                output += "+/- Fly Speed\n";
            }
            output += "Ctrl +/- Scale\n";
        }

        HUDController.Instance.HotkeyLines = output;
    }


    private void SetInfoText()
    {
        var output = "";

        if (_unlimitedRenderDistance) output += "V ";

        if (GizmoVisibilityController.Instance.ShowGizmos) output += "G ";

        if (NoClipController.Instance.NoClipEnabled) output += "N ";

        if (FlashlightController.Instance.FlashlightEnabled) output += "L ";

        HUDController.Instance.InfoLines = output;
    }
    #endregion
}
