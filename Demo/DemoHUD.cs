using HarmonyLib;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Time = UnityEngine.Time;

namespace SuperliminalTAS.Demo
{
    class DemoHUD : MonoBehaviour
    {
        private const float FRAME_TIME = 0.02f; // 1/50 seconds
        private const int TARGET_FPS = 50;

        private Text _hudText;
        private DemoRecorder _recorder;
        private bool _showLess = true;
        private readonly StringBuilder _hudBuilder = new StringBuilder(1024);

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

            _hudText = CreateStatusText(
                parent: gameObject.transform,
                fontName: "NotoMono-Regular",
                fontSize: 26,
                anchoredPosition: new Vector2(25f, -25f),
                size: new Vector2(Screen.currentResolution.width / 3f, Screen.currentResolution.height)
            );
        }

        private void Update()
        {
            if (DemoRecorder.Instance == null || _hudText == null) return;

            if (Input.GetKeyDown(KeyCode.F1))
                _showLess = !_showLess;

            _recorder = DemoRecorder.Instance;

            _hudBuilder.Clear();

            GetPlaybackLines(_hudBuilder);
            _hudBuilder.Append('\n');
            GetInputLines(_hudBuilder);
            _hudBuilder.Append('\n');
            GetPlayerLines(_hudBuilder);
            _hudBuilder.Append('\n');
            GetObjectLines(_hudBuilder);
            _hudBuilder.Append('\n');
            GetHotkeyLines(_hudBuilder);
            _hudBuilder.Append('\n');

            _hudText.text = _hudBuilder.ToString();
        }

        private void GetPlaybackLines(StringBuilder sb)
        {
            // For IGT consistency: The speedrun timer increases on the first frame
            var currentTime = TimeSpan.FromSeconds((_recorder.CurrentFrame + 1) * FRAME_TIME);
            var totalTime = TimeSpan.FromSeconds((_recorder.DemoTotalFrames + 1) * FRAME_TIME);

            var currentTimeString = currentTime.ToString(@"mm\:ss\.ff");
            var totalTimeString = totalTime.ToString(@"mm\:ss\.ff");

            var gameSpeedString = "";

            if (Application.targetFrameRate != TARGET_FPS)
            {
                var timeMult = Application.targetFrameRate / (double)TARGET_FPS;
                gameSpeedString = $"{timeMult:0.00}x";
            }

            switch (_recorder.State)
            {
                case PlaybackState.Stopped:
                    sb.Append($"00:00.00 / {totalTimeString} ■ {gameSpeedString}\n");
                    break;
                case PlaybackState.Playing:
                    sb.Append($"{currentTimeString} / {totalTimeString} ▶ {gameSpeedString}\n");
                    break;
                case PlaybackState.Recording:
                    sb.Append($"{currentTimeString} ● {gameSpeedString}\n");
                    break;
            }

            sb.Append(_recorder.State == PlaybackState.Stopped ? "0" : _recorder.CurrentFrame.ToString());
            if (_showLess)
            {
                sb.Append('\n');
                return;
            }
            sb.Append(" / ").Append(_recorder.DemoTotalFrames);
            sb.Append(' ').Append(Time.time.ToString("0.00")).Append(' ').Append(Time.renderedFrameCount).Append('\n');
        }

        private void GetInputLines(StringBuilder sb)
        {
            var playerInput = GameManager.GM.playerInput;

            if (playerInput == null) return;

            var moveH = playerInput.GetAxis("Move Horizontal");
            var moveV = playerInput.GetAxis("Move Vertical");
            sb.Append($"M {moveV:0.000} {moveH:0.000}\n");

            var lookH = GameManager.GM.playerInput.GetAxis("Look Horizontal");
            var lookV = GameManager.GM.playerInput.GetAxis("Look Vertical");
            sb.Append($"L {lookH:000.000} {lookV:000.000}\n");

            sb.Append("B ");
            if (playerInput.GetButton("Jump")) sb.Append("Jump ");
            if (playerInput.GetButton("Grab")) sb.Append("Grab ");
            if (playerInput.GetButton("Rotate")) sb.Append("Rotate ");
            sb.Append('\n');
        }

        private void GetPlayerLines(StringBuilder sb)
        {
            var player = GameManager.GM.player;

            if (player == null) return;

            var playerPos = player.transform.position;
            sb.Append($"P {playerPos.x:0.0000} {playerPos.y:0.0000} {playerPos.z:0.0000}\n");

            var playerRotH = player.transform.rotation.eulerAngles.y;
            var playerRotV = GameManager.GM.playerCamera.transform.rotation.eulerAngles.x;
            sb.Append($"R {playerRotH:000.00000} {playerRotV:000.00000}\n");

            var playerVel = player.GetComponent<CharacterController>().velocity;
            double horizontal = Math.Sqrt((playerVel.x * playerVel.x) + (playerVel.z * playerVel.z));
            sb.Append($"V {horizontal:0.0000} {playerVel.y:0.0000}\n");

            if (_showLess) return;

            var playerScale = player.transform.localScale.x;
            sb.Append($"S {playerScale:0.00000}x\n");

            if (!player.TryGetComponent<CharacterMotor>(out var playerMotor)) return;

            var isJumping = playerMotor.jumping.jumping;
            var jumpCd = playerMotor.timeOnGroundBeforeCanJump;
            sb.Append($"\nJ {playerMotor.grounded} {isJumping} {jumpCd:0.00}\n");

            var playerMantle = player.GetComponentInChildren<PlayerLerpMantle>();
            if (playerMantle == null) return;

            bool mantling = (bool)_mantleFields["currentlyMantling"].GetValue(playerMantle);
            bool staying = (bool)_mantleFields["staying"].GetValue(playerMantle);
            bool canMantle = playerMantle.canJumpLerp.canLerp;
            int jumpsWithout = (int)_mantleFields["playerJumpedWithoutMantle"].GetValue(playerMantle);
            var groundedTime = Time.time - playerMantle.playerMSC.onGroundTime;
            var mantleResetCD = Mathf.Max(0.1f - groundedTime, 0f);

            sb.Append($"M {mantling} {staying} {canMantle} {jumpsWithout} {mantleResetCD:0.00}\n");
        }

        private void GetObjectLines(StringBuilder sb)
        {
            var player = GameManager.GM.player;
            var playerCamera = GameManager.GM.playerCamera;

            if (playerCamera == null || !playerCamera.TryGetComponent<ResizeScript>(out var resizeScript))
                return;

            var grabbedObject = resizeScript.GetGrabbedObject();

            if (grabbedObject != null)
            {
                sb.Append($"G {resizeScript.isGrabbing} {resizeScript.isReadyToGrab}\n");

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

                sb.Append($"S {objectMinScale:0.0000}x {objectScale:0.0000}x");

                if (_showLess)
                {
                    sb.Append('\n');
                }
                else
                {
                    sb.Append($" {objectDistance:0.000}\n");

                    var objectPos = grabbedObject.transform.position;
                    sb.Append($"P {objectPos.x:0.0000} {objectPos.y:0.0000} {objectPos.z:0.0000}\n");

                    var objectRot = grabbedObject.transform.rotation.eulerAngles;
                    sb.Append($"R {objectRot.x:0.0000} {objectRot.y:0.0000} {objectRot.z:0.0000}\n");
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
                            sb.Append("W ").Append(warpPrediction.x.ToString("0.0")).Append(", ")
                              .Append(warpPrediction.y.ToString("0.0")).Append(", ")
                              .Append(warpPrediction.z.ToString("0.0")).Append(" (")
                              .Append(distance.ToString("0.0")).Append(")\n");
                        }
                    }
                }
            }
        }

        private void GetHotkeyLines(StringBuilder sb)
        {
            var detailString = _showLess ? "Show More" : "Show Less";

            sb.Append("F1  - ").Append(detailString).Append('\n');

            if (_showLess) return;

            switch (DemoRecorder.Instance.State)
            {
                case PlaybackState.Recording:
                    sb.Append("F5  - Stop\nF7  - Reset CP\n");
                    break;
                case PlaybackState.Playing:
                    sb.Append("F5  - Stop\n");
                    break;
                case PlaybackState.Stopped:
                    sb.Append("F5  - Play\nF6  - Record\nF7  - Record from CP\n");
                    sb.Append("F11 - Open\nF12 - Save\n");
                    break;
            }

            sb.Append("+/- - Speed Up/Down");
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
