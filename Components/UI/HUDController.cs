using HarmonyLib;
using SuperliminalTools.TASMod.Demo;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Time = UnityEngine.Time;

namespace SuperliminalTools.Components.UI;

class HUDController : MonoBehaviour
{
#if LEGACY
    public HUDController(System.IntPtr ptr) : base(ptr) { }
#else
    // Modern versions require Reflection to get some of the relevant private fields
    private static readonly Dictionary<string, FieldInfo> _mantleFields = new()
    {
        ["currentlyMantling"] = AccessTools.Field(typeof(PlayerLerpMantle), "currentlyMantling"),
        ["staying"] = AccessTools.Field(typeof(PlayerLerpMantle), "staying"),
        ["canJumpLerp"] = AccessTools.Field(typeof(PlayerLerpMantle), "canJumpLerp"),
        ["playerJumpedWithoutMantle"] = AccessTools.Field(typeof(PlayerLerpMantle), "playerJumpedWithoutMantle"),
        ["playerMSC"] = AccessTools.Field(typeof(PlayerLerpMantle), "playerMSC"),
    };

    private static readonly Dictionary<string, FieldInfo> _resizeFields = new()
    {
        ["grabbedObject"] = AccessTools.Field(typeof(ResizeScript), "grabbedObject"),
        ["scaleAtMinDistance"] = AccessTools.Field(typeof(ResizeScript), "scaleAtMinDistance"),
    };

    private static readonly FieldInfo _canLerpField = AccessTools.Field(
        AccessTools.Field(typeof(PlayerLerpMantle), "canJumpLerp").FieldType,
        "canLerp"
    );

    private static readonly FieldInfo _onGroundTimeField = AccessTools.Field(
        AccessTools.Field(typeof(PlayerLerpMantle), "playerMSC").FieldType,
        "onGroundTime"
    );
#endif
    public static HUDController Instance { get; private set; }

    public bool ShowLess { get; set; } = true;
    public string InfoLines { get; set; } = "";
    public string HotkeyLines { get; set; } = "";

    private Text _hudText;
    private Font _notoMonoFont;
    private DemoRecorder _recorder;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate HUDController");
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _notoMonoFont = UI.FontAssetLoader.GetFontOrDefault();
        _hudText = Components.Utility.CreateHUD(
            parent: gameObject.transform,
            font: _notoMonoFont,
            fontSize: 26,
            anchoredPosition: new Vector2(25f, -25f),
            size: new Vector2(Screen.currentResolution.width / 3f, Screen.currentResolution.height)
        );
    }

    private void LateUpdate()
    {
        if (_hudText == null) return;

        _recorder = DemoRecorder.Instance;

        var hudLines = "";

        if (_recorder != null)
        {
            hudLines += GetPlaybackLines() + "\n";
            hudLines += GetInputLines() + "\n";
        }

        if (GameManager.GM.player != null)
        {
            hudLines += GetPlayerLines() + "\n";
            hudLines += GetObjectLines() + "\n";
        }

        if (_recorder == null && !ShowLess)
            hudLines += GetInputLines() + "\n";

        if (!String.IsNullOrEmpty(InfoLines))
            hudLines += InfoLines + "\n\n";

        hudLines += HotkeyLines + "\n";

        _hudText.text = hudLines;
    }

    private string GetPlaybackLines()
    {
        var output = "";

        // For IGT consistency: The speedrun timer increases on the first frame
        var currentTime = TimeSpan.FromSeconds((_recorder.CurrentFrame + 1) * 0.02);
        var totalTime = TimeSpan.FromSeconds((_recorder.DemoTotalFrames) * 0.02);

        var currentTimeString = currentTime.ToString(@"mm\:ss\.ff");
        var totalTimeString = totalTime.ToString(@"mm\:ss\.ff");

        var gameSpeedString = "";

        if (Application.targetFrameRate != 50)
        {
            var timeMult = Application.targetFrameRate / 50.0;
            gameSpeedString = $"{timeMult:0.00}x";
        }

        switch (_recorder.State)
        {
            case PlaybackState.Stopped:
                output += $"■ 00:00.00 / {totalTimeString} {gameSpeedString}\n";
                break;
            case PlaybackState.Playing:
                output += $"▶ {currentTimeString} / {totalTimeString} {gameSpeedString}\n";
                break;
            case PlaybackState.Recording:
                output += $"● {currentTimeString} {gameSpeedString}\n";
                break;
        }

        output += _recorder.State == PlaybackState.Stopped ? "0" : _recorder.CurrentFrame;
        if (ShowLess) return output + "\n";
        output += " / " + _recorder.DemoTotalFrames;
        output += " " + Time.time.ToString("0.00") + " " + Time.renderedFrameCount + "\n";

        return output;
    }

    private string GetInputLines()
    {
        var output = "";
        var playerInput = GameManager.GM.playerInput;

        if (playerInput == null) return output;

        var moveH = playerInput.GetAxis("Move Horizontal");
        var moveV = playerInput.GetAxis("Move Vertical");
        output += $"M {moveV:0.000} {moveH:0.000}\n";

        var lookH = GameManager.GM.playerInput.GetAxis("Look Horizontal");
        var lookV = GameManager.GM.playerInput.GetAxis("Look Vertical");
        output += $"L {lookH:000.000} {lookV:000.000}\n";

        output += "B ";
        if (playerInput.GetButton("Jump")) output += "Jump ";
        if (playerInput.GetButton("Grab")) output += "Grab ";
        if (playerInput.GetButton("Rotate")) output += "Rotate ";
        output += "\n";

        return output;
    }

    private string GetPlayerLines()
    {
        var output = "";
        var player = GameManager.GM.player;

        if (player == null) return output;

        var playerPos = player.transform.position;
        output += $"P {playerPos.x:0.0000} {playerPos.y:0.0000} {playerPos.z:0.0000}\n";

        var playerRotH = player.transform.rotation.eulerAngles.y;
        var playerRotV = GameManager.GM.playerCamera.transform.rotation.eulerAngles.x;
        output += $"R {playerRotH:000.00000} {playerRotV:000.00000}\n";

        var playerVel = player.GetComponent<CharacterController>().velocity;
        double horizontal = Math.Sqrt((playerVel.x * playerVel.x) + (playerVel.z * playerVel.z));
        output += $"V {horizontal:0.0000} {playerVel.y:0.0000}\n";

        if (!ShowLess)
            output += $"V {playerVel.x:0.0000} {playerVel.y:0.0000} {playerVel.z:0.0000}\n";

        var playerScale = player.transform.localScale.x;
        output += $"S {playerScale:0.00000}x\n";

        if (ShowLess)
            return output;

        var playerMotor = player.GetComponent<CharacterMotor>();
        if (playerMotor == null) return output;

        var isJumping = playerMotor.jumping.jumping;
        var jumpCd = playerMotor.timeOnGroundBeforeCanJump;
        output += $"\nJ {(playerMotor.grounded ? 1 : 0)} {(isJumping ? 1 : 0)} {jumpCd:0.00}\n";

        var playerMantle = player.GetComponentInChildren<PlayerLerpMantle>();
        if (playerMantle == null) return output;

#if LEGACY
        bool mantling = playerMantle.currentlyMantling;
        bool staying = playerMantle.staying;
        bool canMantle = playerMantle.canJumpLerp.canLerp;
        int jumpsWithout = playerMantle.playerJumpedWithoutMantle;
        var groundedTime = Time.time - playerMantle.playerMSC.onGroundTime;
#else
        bool mantling = (bool)_mantleFields["currentlyMantling"].GetValue(playerMantle);
        bool staying = (bool)_mantleFields["staying"].GetValue(playerMantle);
        var canJumpLerp = _mantleFields["canJumpLerp"].GetValue(playerMantle);
        bool canMantle = (bool)_canLerpField.GetValue(canJumpLerp);
        int jumpsWithout = (int)_mantleFields["playerJumpedWithoutMantle"].GetValue(playerMantle);
        var playerMSC = _mantleFields["playerMSC"].GetValue(playerMantle);
        var groundedTime = Time.time - (float)_onGroundTimeField.GetValue(playerMSC);
#endif
        var mantleResetCD = Mathf.Max(0.1f - groundedTime, 0f);

        output += $"M {(mantling ? 1 : 0)} {(staying ? 1 : 0)} {(canMantle ? 1 : 0)} {jumpsWithout} {mantleResetCD:0.00}\n";

        return output;
    }

    private string GetObjectLines()
    {
        var output = "";

        var player = GameManager.GM.player;
        var playerCamera = GameManager.GM.playerCamera;


        var resizeScript = playerCamera != null ? playerCamera.GetComponent<ResizeScript>() : null;
        if (resizeScript == null)
            return output;
#if LEGACY
        var grabbedObject = resizeScript.grabbedObject;
#else
        var grabbedObject = (GameObject)_resizeFields["grabbedObject"].GetValue(resizeScript);
#endif

        if (grabbedObject != null)
        {
            output += $"G {(resizeScript.isGrabbing ? 1 : 0)} {(resizeScript.isReadyToGrab ? 1 : 0)}\n";

            var objectDir = grabbedObject.transform.position - resizeScript.transform.position;
            var objectDistance = objectDir.magnitude;

            var objectScale = grabbedObject.transform.localScale.x;
            float objectMinScale;

            if (resizeScript.isGrabbing)
            {
#if LEGACY
                objectMinScale = resizeScript.scaleAtMinDistance.x;
#else
                objectMinScale = ((Vector3)_resizeFields["scaleAtMinDistance"].GetValue(resizeScript)).x;
#endif
            }
            else
            {
                var minGrabDistance = GameManager.GM.player.transform.localScale.x * 0.25f;
                var scaleRatio = minGrabDistance / objectDistance;
                objectMinScale = objectScale * scaleRatio;
            }

            output += $"S {objectMinScale:0.0000}x {objectScale:0.0000}x";

            if (ShowLess)
            {
                output += "\n";
            }
            else
            {
                output += $" {objectDistance:0.000}\n";

                var objectPos = grabbedObject.transform.position;
                output += $"P {objectPos.x:0.0000} {objectPos.y:0.0000} {objectPos.z:0.0000}\n";

                var objectRot = grabbedObject.transform.rotation.eulerAngles;
                output += $"R {objectRot.x:0.0000} {objectRot.y:0.0000} {objectRot.z:0.0000}\n";
            }

            if (grabbedObject.GetComponent<Collider>() != null)
            {
                Collider playerCollider = player.GetComponent<Collider>();
                Collider objectCollider = grabbedObject.GetComponent<Collider>();
                if (
                    Physics.ComputePenetration(playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
                        objectCollider, objectCollider.transform.position, objectCollider.transform.rotation,
                        out Vector3 direction, out float distance))
                {
                    Vector3 warpPrediction = player.transform.position + direction * distance;
                    if (distance > 5)
                    {
                        output += "W " + warpPrediction.x.ToString("0.0") + ", " + warpPrediction.y.ToString("0.0") + ", " + warpPrediction.z.ToString("0.0") + " (" + distance.ToString("0.0") + ")\n";
                    }
                }
            }
        }


        return output;
    }
}
