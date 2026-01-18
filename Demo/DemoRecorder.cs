using Rewired;
using SuperliminalTAS.Patches;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Screen = UnityEngine.Screen;

namespace SuperliminalTAS.Demo;

public sealed class DemoRecorder : MonoBehaviour
{
    private int _demoStartFrame;
    private bool _recording;
    private bool _playingBack;
    private bool _resetting;
    private bool _lastUpdateWasFixed;

    // Available playback speeds in FPS (base game is 50 FPS)
    private static readonly int[] PlaybackSpeeds = { 1, 2, 5, 10, 25, 50, 100, 200, 400 };
    private int _playbackSpeedIndex = 5; // Start at 50 FPS (1x speed)

    // Track if we're using a custom speed from CSV
    private bool _usingCustomSpeed;
    private float _customSpeedMultiplier = 1f;

    private int CurrentDemoFrame => Time.renderedFrameCount - _demoStartFrame;


    private Text _statusText;
    private DemoData _data;
    private DemoFileDialog _fileDialog;
    private string _lastOpenedFile;

    private void Awake()
    {
        _fileDialog = new DemoFileDialog();
        _data = DemoData.CreateEmpty();

        Application.targetFrameRate = 50;
    }

    private void Update()
    {
        if (!_lastUpdateWasFixed && (_recording || _playingBack))
            Debug.Log(Time.timeSinceLevelLoad + ": Double Update() during recording/playback");

        _lastUpdateWasFixed = false;

        EnsureStatusText();
        UpdateStatusText();
    }

    private void FixedUpdate()
    {
        if (_lastUpdateWasFixed && _recording && _playingBack)
            Debug.Log(Time.timeSinceLevelLoad + ": Double FixedUpdate() during recording/playback");

        _lastUpdateWasFixed = true;
    }

    private void LateUpdate()
    {
        if (_resetting) return;

        HandleHotkeys();

        if (_recording)
        {
            _data.RecordFrameFrom(GameManager.GM.playerInput);
        }
        else if (_playingBack)
        {
            // Check for speed change from CSV at current frame
            var speed = _data.GetSpeed(CurrentDemoFrame);
            if (speed.HasValue)
            {
                _usingCustomSpeed = true;
                _customSpeedMultiplier = speed.Value;
                ApplyCustomSpeed(speed.Value);
            }

            if (CurrentDemoFrame + 1 >= _data.FrameCount)
                StopPlayback();
        }
    }

    #region Hotkeys / UI

    private void HandleHotkeys()
    {
        if (_recording)
        {
            if (Input.GetKeyDown(KeyCode.F6)) StopRecording();
        }
        else if (_playingBack)
        {
            if (Input.GetKeyDown(KeyCode.F6)) StopPlayback();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F5)) StartPlayback();
            else if (Input.GetKeyDown(KeyCode.F7)) StartRecording();
        }

        if (Input.GetKeyDown(KeyCode.F12))
        {
            WithUnlockedCursor(() => SaveDemo());
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            WithUnlockedCursor(() => OpenDemo());
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            WithUnlockedCursor(() => ExportCSV());
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            StopPlayback();
            ReloadFile();
            StartPlayback();
        }

        if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Equals))
        {
            IncreasePlaybackSpeed();
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Minus))
        {
            DecreasePlaybackSpeed();
        }
    }

    private static void WithUnlockedCursor(Action action)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        try { action?.Invoke(); }
        finally
        {
            Cursor.visible = false;
        }
    }

    private void IncreasePlaybackSpeed()
    {
        _playbackSpeedIndex++;
        if (_playbackSpeedIndex >= PlaybackSpeeds.Length)
            _playbackSpeedIndex = PlaybackSpeeds.Length - 1;

        _usingCustomSpeed = false;
        ApplyPlaybackSpeed();
    }

    private void DecreasePlaybackSpeed()
    {
        _playbackSpeedIndex--;
        if (_playbackSpeedIndex < 0)
            _playbackSpeedIndex = 0;

        _usingCustomSpeed = false;
        ApplyPlaybackSpeed();
    }

    private void ApplyPlaybackSpeed()
    {
        Application.targetFrameRate = PlaybackSpeeds[_playbackSpeedIndex];
    }

    private void ApplyCustomSpeed(float multiplier)
    {
        // Base game speed is 50 FPS
        int targetFps = Mathf.RoundToInt(50f * multiplier);
        // Clamp to reasonable values (at least 1 FPS)
        targetFps = Mathf.Max(1, targetFps);
        Application.targetFrameRate = targetFps;
    }

    private void EnsureStatusText()
    {
        if (_statusText != null) return;
        if (GameObject.Find("UI_PAUSE_MENU") == null) return;

        _statusText = DemoStatusText.CreateStatusText(
            parentCanvas: GameObject.Find("UI_PAUSE_MENU").transform.Find("Canvas"),
            fontName: "NotoMono-Regular",
            fontSize: 30,
            anchoredPosition: new Vector2(25f, -25f),
            size: new Vector2(Screen.currentResolution.width / 4f, Screen.currentResolution.height)
        );
    }

    private void UpdateStatusText()
    {
        if (_statusText == null) return;

        var frame = CurrentDemoFrame;

        float currentSpeedMult;
        if (_usingCustomSpeed)
        {
            currentSpeedMult = _customSpeedMultiplier;
        }
        else
        {
            int currentFps = PlaybackSpeeds[_playbackSpeedIndex];
            currentSpeedMult = currentFps / 50f;
        }

        string speedInfo = currentSpeedMult != 1f ? $" [{currentSpeedMult}x]" : "";

        if (_playingBack)
        {
            _statusText.text = $"playback: {frame} / {_data.FrameCount}";

            _statusText.text += speedInfo;
            _statusText.text += "\n\n";
        }
        else
        {
            string status = _recording ? $"recording: {frame} / ?" : $"stopped: 0 / {_data.FrameCount}";
            _statusText.text = status + speedInfo + "\n\n";
        }

        if (GameManager.GM.player != null)
        {
            var playerPos = GameManager.GM.player.transform.position;
            _statusText.text +=
                $"P: {playerPos.x:0.00000}, " +
                $"{playerPos.y:0.00000}, " +
                $"{playerPos.z:0.00000}\n";

            var vel = GameManager.GM.player.GetComponent<CharacterController>().velocity;
            float horizontal = Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z);

            _statusText.text +=
                $"V: {horizontal: 0.00000;-0.00000}, {vel.y: 0.00000;-0.00000}\n";

        }

        /**
        if (_recording || _playingBack)
        {
            var secondsByFrames = (Time.renderedFrameCount - _demoStartFrame) * 0.02;
            _statusText.text += $"\n{Time.timeSinceLevelLoad:0.00} / {secondsByFrames:0.00}";
        }
        **/

        _statusText.text +=
            $"\nM: {TASInput.GetAxis("Move Horizontal", GameManager.GM.playerInput.GetAxis("Move Horizontal")): 0.000;-0.000} " +
            $"{TASInput.GetAxis("Move Vertical", GameManager.GM.playerInput.GetAxis("Move Vertical")): 0.000;-0.000}";

        _statusText.text +=
            $"\nL: {TASInput.GetAxis("Look Horizontal", GameManager.GM.playerInput.GetAxis("Look Horizontal")): 0.000;-0.000} " +
            $"{TASInput.GetAxis("Look Vertical", GameManager.GM.playerInput.GetAxis("Look Vertical")): 0.000;-0.000}";

        _statusText.text += "\n\nF5  - Play\nF6  - Stop\nF7  - Record";
        _statusText.text += "\nF8  - Reload & Play\nF10 - Export CSV";
        _statusText.text += "\nF11 - Open\nF12 - Save";
        _statusText.text += "\n\n+/- - Speed Up/Down";
    }

    #endregion

    #region Recording / Playback

    private void StartRecording()
    {
        if (_recording || _playingBack || _resetting) return;

        StartCoroutine(ResetLevelStateThen(() =>
        {
            _data = DemoData.CreateEmpty();
            _recording = true;
            _playingBack = false;
            _demoStartFrame = Time.renderedFrameCount;

            TASInput.disablePause = true;
            TASInput.StopPlayback();
        }));
    }

    private void StopRecording()
    {
        TASInput.disablePause = false;
        _recording = false;
    }

    private void StartPlayback()
    {
        if (_data.FrameCount < 1 || _recording || _playingBack || _resetting) return;

        StartCoroutine(ResetLevelStateThen(() =>
        {
            _recording = false;
            _playingBack = true;
            _demoStartFrame = Time.renderedFrameCount;

            _playbackSpeedIndex = 5; // Reset to 50 FPS (1x)
            _usingCustomSpeed = false;
            ApplyPlaybackSpeed();

            TASInput.disablePause = true;
            TASInput.StartPlayback(this);
        }));
    }

    private void StopPlayback()
    {
        _recording = false;
        _playingBack = false;

        ApplyPlaybackSpeed();

        TASInput.disablePause = false;
        TASInput.StopPlayback();
    }

    internal bool GetRecordedButton(string actionName) =>
        _data.GetButton(actionName, CurrentDemoFrame);

    internal bool GetRecordedButtonDown(string actionName) =>
        _data.GetButtonDown(actionName, CurrentDemoFrame);

    internal bool GetRecordedButtonUp(string actionName) =>
        _data.GetButtonUp(actionName, CurrentDemoFrame);

    internal float GetRecordedAxis(string actionName) =>
        _data.GetAxis(actionName, CurrentDemoFrame);

    #endregion

    #region Saving / Loading

    private void OpenDemo()
    {
        var path = _fileDialog.OpenPath();
        if (string.IsNullOrWhiteSpace(path)) return;

        LoadFile(path);
    }

    private void SaveDemo()
    {
        if (_data.FrameCount == 0) return;

        var path = _fileDialog.SavePath();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            if (!path.EndsWith(".slt", StringComparison.OrdinalIgnoreCase))
                path += ".slt";

            var bytes = DemoSerializer.Serialize(_data);
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save demo: {e}");
        }
    }

    private void ExportCSV()
    {
        if (_data.FrameCount == 0) return;

        var path = _fileDialog.SavePathCSV();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                path += ".csv";

            var csv = DemoCSVSerializer.Serialize(_data);
            File.WriteAllText(path, csv);
            Debug.Log($"Exported CSV to: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export CSV: {e}");
        }
    }

    private void LoadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension == ".csv")
            {
                string csv;
                using (var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    csv = sr.ReadToEnd();
                }

                _data = DemoCSVSerializer.Deserialize(csv);
                Debug.Log($"Loaded CSV from: {path} ({_data.FrameCount} frames)");
            }
            else if (extension == ".slt")
            {
                var bytes = File.ReadAllBytes(path);
                _data = DemoSerializer.Deserialize(bytes);
                Debug.Log($"Loaded SLT from: {path} ({_data.FrameCount} frames)");
            }
            else
            {
                Debug.LogError($"Unknown file type: {extension}");
                return;
            }

            _lastOpenedFile = path;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load file: {e}");
        }
    }

    private void ReloadFile()
    {
        if (string.IsNullOrWhiteSpace(_lastOpenedFile))
        {
            Debug.LogWarning("No file to reload. Open a file first.");
            return;
        }

        if (!File.Exists(_lastOpenedFile))
        {
            Debug.LogError($"File no longer exists: {_lastOpenedFile}");
            return;
        }

        Debug.Log($"Reloading: {_lastOpenedFile}");
        LoadFile(_lastOpenedFile);
    }
    #endregion

    #region Scene Reset
    private IEnumerator ResetLevelStateThen(Action afterLoaded)
    {
        if (_resetting) yield break;

        _resetting = true;
        TASInput.blockAllInput = true;

        Player player = ReInput.players.GetPlayer(0);
        Player.ControllerHelper controllers = player.controllers;

        controllers.maps.SetAllMapsEnabled(false);

        GameManager.GM.player.GetComponent<CharacterMotor>().ChangeGravity(0f);
        GameManager.GM.player.GetComponent<CharacterController>().SimpleMove(Vector3.zero);

        yield return null;

        void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnLoaded;

            player.controllers.maps.SetMapsEnabled(true, ControllerType.Mouse, "Default", "Default");
            player.controllers.maps.SetMapsEnabled(true, ControllerType.Joystick, "Default", "Default");
            player.controllers.maps.SetMapsEnabled(true, ControllerType.Keyboard, "Default");

            StartCoroutine(AfterSceneLoadedPhaseLocked(afterLoaded));
        }

        SceneManager.sceneLoaded += OnLoaded;

        GameManager.GM.GetComponent<PlayerSettingsManager>()?.SetMouseSensitivity(1.0f);

        GameManager.GM.TriggerScenePreUnload();
        GameManager.GM.GetComponent<SaveAndCheckpointManager>().RestartLevel();
    }

    private IEnumerator AfterSceneLoadedPhaseLocked(Action afterLoaded)
    {
        GameManager.GM.player.transform.Find("GUI Camera").GetComponent<FadeCameraToBlack>().enabled = false;
        GameManager.GM.player.transform.Find("GUI Camera/Canvas/Fade").gameObject.SetActive(false);

        TASInput.blockAllInput = false;
        _resetting = false;
        afterLoaded?.Invoke();

        yield break;
    }

    #endregion
}