using HarmonyLib;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Time = UnityEngine.Time;

namespace SuperliminalTAS.Demo
{
    class DemoHUD : MonoBehaviour
    {
        private Text _hudText;
        private DemoRecorder _recorder;
        private bool _showLess = true;

        private readonly Dictionary<string, FieldInfo> _mantleFields = [];
        private readonly Dictionary<string, FieldInfo> _resizeFields = [];

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoadEnsureStatusText;
            GetReflectedFields();
        }

        private void GetReflectedFields()
        {
            var mantleType = typeof(PlayerLerpMantle);
            _mantleFields.Add("currentlyMantling", AccessTools.Field(mantleType, "currentlyMantling"));
            _mantleFields.Add("staying", AccessTools.Field(mantleType, "staying"));
            _mantleFields.Add("playerJumpedWithoutMantle", AccessTools.Field(mantleType, "playerJumpedWithoutMantle"));

            var resizeType = typeof(ResizeScript);
            _resizeFields.Add("isLerpingToPosition", AccessTools.Field(resizeType, "isLerpingToPosition"));
            _resizeFields.Add("scaleAtMinDistance", AccessTools.Field(resizeType, "scaleAtMinDistance"));
        }

        private void OnSceneLoadEnsureStatusText(Scene _, LoadSceneMode __)
        {
            if (_hudText != null) return;

            //var mainCanvas = GameObject.Find("UI_PAUSE_MENU")?.transform.Find("Canvas");
            //if (mainCanvas == null) return;

            _hudText = CreateStatusText(
                parent: gameObject.transform,
                fontName: "NotoMono-Regular",
                fontSize: 26,
                anchoredPosition: new Vector2(25f, -25f),
                size: new Vector2(Screen.currentResolution.width / 3f, Screen.currentResolution.height)
            );
        }

        private void LateUpdate()
        {
            if (DemoRecorder.Instance == null || _hudText == null) return;

            if (Input.GetKeyDown(KeyCode.F1))
                _showLess = !_showLess;

            _recorder = DemoRecorder.Instance;

            var hudLines = "";

            hudLines += GetPlaybackLines() + "\n";
            hudLines += GetInputLines() + "\n";
            hudLines += GetPlayerLines() + "\n";
            hudLines += GetObjectLines() + "\n";
            hudLines += GetHotkeyLines() + "\n";

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
            if (_showLess) return output+"\n";
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
            if (_showLess)
                return output;


            output += $"V {playerVel.x:0.0000} {playerVel.y:0.0000} {playerVel.z:0.0000}\n";

            var playerScale = player.transform.localScale.x;
            output += $"S {playerScale:0.00000}x\n";

            if (!player.TryGetComponent<CharacterMotor>(out var playerMotor)) return output;

            var isJumping = playerMotor.jumping.jumping;
            var jumpCd = playerMotor.timeOnGroundBeforeCanJump;
            output += $"\nJ {(playerMotor.grounded ? 1 : 0)} {(isJumping ? 1 : 0)} {jumpCd:0.00}\n";

            var playerMantle = player.GetComponentInChildren<PlayerLerpMantle>();
            if (playerMantle == null) return output;

            bool mantling = (bool)_mantleFields["currentlyMantling"].GetValue(playerMantle);
            bool staying = (bool)_mantleFields["staying"].GetValue(playerMantle);
            bool canMantle = playerMantle.canJumpLerp.canLerp;
            int jumpsWithout = (int)_mantleFields["playerJumpedWithoutMantle"].GetValue(playerMantle);
            var groundedTime = Time.time - playerMantle.playerMSC.onGroundTime;
            var mantleResetCD = Mathf.Max(0.1f - groundedTime, 0f);

            output += $"M {(mantling ? 1 : 0)} {(staying ? 1 : 0)} {(canMantle ? 1 : 0)} {jumpsWithout} {mantleResetCD:0.00}\n";

            return output;
        }

        private string GetObjectLines()
        {
            var output = "";

            var player = GameManager.GM.player;
            var playerCamera = GameManager.GM.playerCamera;

            if (playerCamera == null || !playerCamera.TryGetComponent<ResizeScript>(out var resizeScript))
                return output;

            var grabbedObject = resizeScript.GetGrabbedObject();
            //var isLerping = (bool)_resizeFields["isLerpingToPosition"].GetValue(resizeScript);

            if (grabbedObject != null)
            {
                output += $"G {resizeScript.isGrabbing} {resizeScript.isReadyToGrab}\n";

                var objectDir = grabbedObject.transform.position - resizeScript.transform.position;
                var objectDistance = objectDir.magnitude;

                var objectScale = grabbedObject.transform.localScale.x;
                float objectMinScale;

                if (resizeScript.isGrabbing)
                {
                    objectMinScale = ((Vector3)(_resizeFields["scaleAtMinDistance"].GetValue(resizeScript))).x;
                }
                else
                {
                    var minGrabDistance = GameManager.GM.player.transform.localScale.x * 0.25f;
                    var scaleRatio = minGrabDistance / objectDistance;
                    objectMinScale = objectScale * scaleRatio;
                }

                output += $"S {objectMinScale:0.0000}x {objectScale:0.0000}x";

                if (_showLess)
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

        private string GetHotkeyLines()
        {
            var output = "";

            var detailString = _showLess ? "Show More" : "Show Less";

            output += "F1  " + detailString + "\n";

            if (_showLess) return output;

            output += "F2  Render Distance\n";
            output += "F3  Show Gizmos\n";

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

            return output;
        }

        public static Text CreateStatusText(
           Transform parent,
           string fontName,
           int fontSize,
           Vector2 anchoredPosition,
           Vector2 size)
        {
            var root = new GameObject("TASMod_HUD");
            root.transform.SetParent(parent, worldPositionStays: false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var child = new GameObject("TASMod_HUD_StatusText");
            child.transform.SetParent(root.transform);
            child.AddComponent<CanvasGroup>().blocksRaycasts = false;

            var text = child.AddComponent<Text>();
            text.fontSize = fontSize;

            var font = Resources.FindObjectsOfTypeAll<Font>().Where((Font font) => font.name == fontName).FirstOrDefault();
            if (font is not null) text.font = font;

            var rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;

            return text;
        }
    }
}
