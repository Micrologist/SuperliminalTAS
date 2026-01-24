using HarmonyLib;
using Rewired;
using SuperliminalTAS.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperliminalTAS.Demo;

public sealed class DemoRecorder : MonoBehaviour
{
    public static DemoRecorder Instance { get; private set; }

    public PlaybackState State => _recording ? PlaybackState.Recording : _playingBack ? PlaybackState.Playing : PlaybackState.Stopped;
    public int CurrentFrame => Time.renderedFrameCount - _demoStartFrame;
    public int DemoTotalFrames => _data.FrameCount;

    private int _demoStartFrame;
    private bool _recording;
    private bool _playingBack;
    private bool _resetting;
    private bool _lastUpdateWasFixed;
    private bool _needsCheckpointReset;

    // Available playback speeds in FPS (base game is 50 FPS)
    private static readonly int[] PlaybackSpeeds = { 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000 };
    private int _playbackSpeedIndex = 5; // Start at 50 FPS (1x speed)

    private bool _unlimitedRenderDistance = false;
    private bool _showGizmos = false;

    private bool _usingCustomSpeed;
    private float _customSpeedMultiplier = 1f;

    private PathProjector _pathProjector;

    private DemoData _data;
    private DemoFileDialog _fileDialog;
    private string _lastOpenedFile;
    private DateTime _lastFileWriteTime;

    private MethodInfo _createNoClipCamera;
    private MethodInfo _endNoClip;

    private readonly List<ColliderVisualizer> _colliders = [];

    private void Awake()
    {
        _fileDialog = new DemoFileDialog();
        _data = DemoData.CreateEmpty();

        DemoRecorder.Instance = this;

        Application.targetFrameRate = 50;
        SceneManager.sceneLoaded += OnLoadSetup;
        SceneManager.sceneUnloaded += OnUnloadCleanUp;
    }

    private void OnUnloadCleanUp(Scene arg0)
    {
        if(GameManager.GM.TryGetComponent<LevelJumpingScript>(out var jumpingScript) && jumpingScript.noClip)
        {
            Destroy(jumpingScript.instanceCameraNoClip);
            jumpingScript.noClip = false;
        }
    }

    private void Update()
    {
        if (!_lastUpdateWasFixed && (_recording || _playingBack) && Time.timeSinceLevelLoad > float.Epsilon)
        {
            Debug.LogError(Time.timeSinceLevelLoad + ": Double Update() during recording/playback, aborting!");
            StopPlayback();
            StopRecording();
        }
        _lastUpdateWasFixed = false;

        CheckForFileChanges();
    }

    private void FixedUpdate()
    {
        if (_lastUpdateWasFixed && _recording && _playingBack)
            Debug.LogError(Time.timeSinceLevelLoad + ": Double FixedUpdate() during recording/playback");

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
            // Check for checkpoint reset at current frame
            if (_data.GetCheckpointReset(CurrentFrame))
            {
                Debug.Log($"{Time.time}: Checkpoint reset triggered on frame {CurrentFrame}");
                _needsCheckpointReset = true;
            }

            // Check for speed change from CSV at current frame
            var speed = _data.GetSpeed(CurrentFrame);
            if (speed.HasValue)
            {
                _usingCustomSpeed = true;
                _customSpeedMultiplier = speed.Value;
                ApplySpeed(speed.Value);
            }

            if (CurrentFrame + 1 >= _data.FrameCount)
                StopPlayback();
        }

        if (_needsCheckpointReset)
        {
            _needsCheckpointReset = false;
            ResetToCheckpoint();
        }
    }

    #region Hotkeys

    private void HandleHotkeys()
    {
        if (_recording)
        {
            if (Input.GetKeyDown(KeyCode.F5)) StopRecording();
            if (Input.GetKeyDown(KeyCode.F7)) TriggerCheckpointReset();
        }
        else if (_playingBack)
        {
            if (Input.GetKeyDown(KeyCode.F5)) StopPlayback();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.F4)) ToggleNoclip();
            if (Input.GetKeyDown(KeyCode.F5)) StartPlayback();
            else if (Input.GetKeyDown(KeyCode.F6)) StartRecording();
            else if (Input.GetKeyDown(KeyCode.F7)) StartRecordingFromCheckpoint();

            if (Input.GetKeyDown(KeyCode.F12))
            {
                WithUnlockedCursor(() => SaveDemo());
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                WithUnlockedCursor(() => OpenDemo());
            }
        }

        if(Input.GetKeyDown(KeyCode.F2))
        {
            _unlimitedRenderDistance = !_unlimitedRenderDistance;
            SetRenderDistance();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            _showGizmos = !_showGizmos;
            SetGizmos();
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

    private void SetGizmos()
    {
        var pm = PortalInstanceTracker.instance.PortalManager;
        if (pm != null)
        {
            var prti = pm.GetComponentInChildren<PortalRenderTextureImplementation>();
            if(prti != null)
            {
                var cullingMaskField = AccessTools.Field(typeof(PortalRenderTextureImplementation), "defaultMainCameraCullingMask");

                var cullingMask = _showGizmos ? -1 : -32969;
                cullingMaskField.SetValue(prti, cullingMask);
                GameManager.GM.playerCamera.cullingMask = cullingMask;
            }
        }

        if (_pathProjector != null)
        {
            _pathProjector.enabled = _showGizmos;
        }
    }

    private void ToggleNoclip()
    {
        if (GameManager.GM.player == null || !GameManager.GM.TryGetComponent<LevelJumpingScript>(out var jumpingScript)) return;

        if(_createNoClipCamera == null)
        {
            _createNoClipCamera = typeof(LevelJumpingScript).GetMethod(
                "CreateNoClipCamera",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            _endNoClip = typeof(LevelJumpingScript).GetMethod(
                "EndNoClip",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
        }

        if (!jumpingScript.noClip)
        {
            _createNoClipCamera.Invoke(jumpingScript, null);
            var cam = jumpingScript.instanceCameraNoClip.GetComponentInChildren<Camera>();
            cam.backgroundColor = new Color(.1f, .1f, .1f);
            cam.farClipPlane = 100000;
            cam.fieldOfView = 90;
            cam.cullingMatrix = new Matrix4x4(Vector4.positiveInfinity, Vector4.positiveInfinity, Vector4.positiveInfinity, Vector4.positiveInfinity);

            GameManager.GM.player.SetActive(true);
            TogglePlayerComponents(false);
        }
        else
        {
            _endNoClip.Invoke(jumpingScript, null);
            TogglePlayerComponents(true);
        }
    }

    private static void TogglePlayerComponents(bool newActive)
    {
        if (GameManager.GM.player.TryGetComponent<FPSInputController>(out var inputController))
        {
            inputController.enabled = newActive;
            inputController.motor.enabled = newActive;
        }

        if (GameManager.GM.player.TryGetComponent<MouseLook>(out var mouseLookP))
        {
            mouseLookP.enabled = newActive;
        }

        if (GameManager.GM.playerCamera.TryGetComponent<MouseLook>(out var mouseLookC))
        {
            mouseLookC.enabled = newActive;
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
    #endregion

    #region Recording / Playback

    private void StartRecording()
    {
        if (_recording || _playingBack || _resetting) return;

        StartCoroutine(ResetLevelStateThen(() =>
        {
            _data = DemoData.CreateEmpty();

            _data.LevelId = SceneManager.GetActiveScene().name;
            _data.CheckpointId = -1;

            _recording = true;
            _playingBack = false;
            _demoStartFrame = Time.renderedFrameCount;

            TASInput.disablePause = true;
            TASInput.StopPlayback();
        }));
    }

    private void StartRecordingFromCheckpoint()
    {
        if (_recording || _playingBack || _resetting) return;

        int checkpointId = GetCurrentCheckpointIndex();

        StartCoroutine(ResetLevelStateThen(() =>
        {
            // Teleport to the checkpoint first
            if (checkpointId >= 0)
            {
                TeleportToCheckpoint(checkpointId);
            }

            _data = DemoData.CreateEmpty();

            _data.LevelId = SceneManager.GetActiveScene().name;
            _data.CheckpointId = checkpointId;

            _recording = true;
            _playingBack = false;
            _demoStartFrame = Time.renderedFrameCount;

            TASInput.disablePause = true;
            TASInput.StopPlayback();

            Debug.Log($"Started recording from checkpoint {checkpointId}");
        }));
    }


    private void StopRecording()
    {
        TASInput.disablePause = false;
        _playbackSpeedIndex = 5;
        ApplyPlaybackSpeed();
        _recording = false;
    }

    private void TriggerCheckpointReset()
    {
        _needsCheckpointReset = true;

        if (!_recording) return;

        Debug.Log($"Triggering checkpoint reset on frame {CurrentFrame}");

        // Mark this frame as having a checkpoint reset
        _data.SetCheckpointReset(CurrentFrame, true);
    }

    private void StartPlayback()
    {
        if (_data.FrameCount < 1 || _recording || _playingBack || _resetting) return;

        if (!string.IsNullOrEmpty(_data.LevelId) && SceneManager.GetActiveScene().name != _data.LevelId)
        {
            GameManager.GM.TriggerScenePreUnload();
            SceneManager.LoadScene(_data.LevelId);
            return;
        }

        StartCoroutine(ResetLevelStateThen(() =>
        {
            // If demo has a specific checkpoint, teleport to it
            if (_data.CheckpointId >= 0)
            {
                TeleportToCheckpoint(_data.CheckpointId);
            }

            _recording = false;
            _playingBack = true;
            _demoStartFrame = Time.renderedFrameCount;

            TASInput.disablePause = true;
            TASInput.StartPlayback(this);
        }));
    }

    private void StopPlayback()
    {
        _recording = false;
        _playingBack = false;

        _playbackSpeedIndex = 5;
        ApplyPlaybackSpeed();

        TASInput.disablePause = false;
        TASInput.StopPlayback();
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
        ApplySpeed(PlaybackSpeeds[_playbackSpeedIndex] / 50f);
    }

    private void ApplySpeed(float multiplier)
    {
        int targetFps = Mathf.RoundToInt(50f * multiplier);
        targetFps = Mathf.Max(1, targetFps);
        Application.targetFrameRate = targetFps;

        SetRenderDistance();
    }

    private void SetRenderDistance()
    {
        var playerCamera = GameManager.GM.playerCamera;
        if (playerCamera == null) return;

        playerCamera.GetComponent<CameraSettingsLayer>().enabled = false;

        if (Application.targetFrameRate > 999)
        {
            playerCamera.farClipPlane = .1f;
            playerCamera.ResetCullingMatrix();
        }
        else if (Application.targetFrameRate <= 50 && _unlimitedRenderDistance)
        {
            playerCamera.cullingMatrix = new Matrix4x4(Vector4.positiveInfinity, Vector4.positiveInfinity, Vector4.positiveInfinity, Vector4.positiveInfinity);
            playerCamera.farClipPlane = 100000f;

            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = new Color(.1f, .1f, .1f);
        }
        else
        {
            playerCamera.clearFlags = CameraClearFlags.Skybox;

            playerCamera.farClipPlane = 1000f;
            playerCamera.ResetCullingMatrix();
        }
    }

    internal bool GetRecordedButton(string actionName) =>
        _data.GetButton(actionName, Math.Min(CurrentFrame, _data.FrameCount -1));

    internal bool GetRecordedButtonDown(string actionName) =>
        _data.GetButtonDown(actionName, Math.Min(CurrentFrame, _data.FrameCount - 1));

    internal bool GetRecordedButtonUp(string actionName) =>
        _data.GetButtonUp(actionName, Math.Min(CurrentFrame, _data.FrameCount - 1));

    internal float GetRecordedAxis(string actionName) =>
        _data.GetAxis(actionName, Math.Min(CurrentFrame, _data.FrameCount - 1));

    #endregion

    #region File Saving / Loading

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
            if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                path += ".csv";

            var csv = DemoCSVSerializer.Serialize(_data);
            File.WriteAllText(path, csv);
            Debug.Log($"Saved CSV to: {path}");

            _lastOpenedFile = path;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save demo: {e}");
        }
    }



    private void CheckForFileChanges()
    {
        // Only check for file changes if we have a loaded file and we're not currently recording or resetting
        if (string.IsNullOrWhiteSpace(_lastOpenedFile) || _recording || _resetting)
            return;

        try
        {
            if (!File.Exists(_lastOpenedFile))
                return;

            var currentWriteTime = File.GetLastWriteTime(_lastOpenedFile);

            // If the file has been modified since we last loaded it
            if (_lastFileWriteTime != default && currentWriteTime > _lastFileWriteTime)
            {
                Debug.Log($"File change detected: {_lastOpenedFile}");
                StopPlayback();
                StartCoroutine(ReloadFile());
            }
        }
        catch (Exception e)
        {
            // Silently ignore errors (file might be temporarily locked during writing)
        }
    }

    private bool LoadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension != ".csv")
            {
                Debug.LogError($"Unsupported file type: {extension}. Only CSV files are supported.");
                return false;
            }

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

            if (_data.CheckpointId >= 0)
            {
                Debug.Log($"Demo Checkpoint: {_data.CheckpointId}");
            }

            _lastOpenedFile = path;
            _lastFileWriteTime = File.GetLastWriteTime(path);

            // Display level and checkpoint information
            if (!string.IsNullOrEmpty(_data.LevelId) && SceneManager.GetActiveScene().name != _data.LevelId)
            {
                Debug.Log($"Demo Level: {_data.LevelId}");
                GameManager.GM.TriggerScenePreUnload();
                SceneManager.LoadScene(_data.LevelId);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load file: {e}");
        }

        return false;
    }



    private IEnumerator ReloadFile()
    {
        if (string.IsNullOrWhiteSpace(_lastOpenedFile))
        {
            Debug.LogWarning("No file to reload. Open a file first.");
            yield break;
        }

        if (!File.Exists(_lastOpenedFile))
        {
            Debug.LogError($"File no longer exists: {_lastOpenedFile}");
            yield break;
        }

        yield return null;

        Debug.Log($"Reloading: {_lastOpenedFile}");
        if(LoadFile(_lastOpenedFile))
            StartPlayback();
    }
    #endregion

    #region Level and Checkpoint Management

    private void ResetToCheckpoint()
    {
        try
        {
            var saveManager = GameManager.GM.GetComponent<SaveAndCheckpointManager>();
            if (saveManager == null)
            {
                Debug.LogError("SaveAndCheckpointManager not found!");
                return;
            }

            saveManager.ResetToLastCheckpoint();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to reset to checkpoint: {e}");
        }
    }

    private void TeleportToCheckpoint(int checkpointId)
    {
        try
        {
            var saveManager = GameManager.GM.GetComponent<SaveAndCheckpointManager>();
            if (saveManager == null)
            {
                Debug.LogError("SaveAndCheckpointManager not found!");
                return;
            }

            // Teleport to specific checkpoint index
            saveManager.TeleportToCheckpointIndexDebug(checkpointId);
            Debug.Log($"Teleported to checkpoint {checkpointId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to teleport to checkpoint {checkpointId}: {e}");
        }
    }

    private int GetCurrentCheckpointIndex()
    {
        var saveManager = GameManager.GM.GetComponent<SaveAndCheckpointManager>();
        if (saveManager == null) return -1;

        if (SaveGamePatch.lastCheckpoint == null) return -1;

        CheckPoint[] array = global::UnityEngine.Object.FindObjectsOfType<CheckPoint>();
        RoomOrder roomOrder = global::UnityEngine.Object.FindObjectOfType<RoomOrder>();
        if (roomOrder)
        {
            Array.Sort<CheckPoint>(array, (CheckPoint x, CheckPoint y) => Array.IndexOf<Transform>(roomOrder.TopLevelRoomOrder, x.transform.root).CompareTo(Array.IndexOf<Transform>(roomOrder.TopLevelRoomOrder, y.transform.root)));
            return Array.IndexOf(array, SaveGamePatch.lastCheckpoint);
        }

        return -1;

    }

    #endregion

    #region Scene Reset

    private IEnumerator ResetLevelStateThen(Action afterLoaded)
    {
        if (_resetting) yield break;

        _resetting = true;
        TASInput.blockAllInput = true;

        Time.timeScale = 1;

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
        TASInput.blockAllInput = false;

        _resetting = false;
        afterLoaded?.Invoke();

        yield break;
    }

    private void OnLoadSetup(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _data.LevelId)
        {
            if (_playingBack)
            {
                Debug.LogWarning("Level was finished on frame " + CurrentFrame + " with " + (DemoTotalFrames - CurrentFrame - 1) + " frames remaining in the demo.");
                StopPlayback();
            }
        }

        _colliders.Clear();

        var player = GameManager.GM.player;

        if(player != null && !player.TryGetComponent<ColliderVisualizer>(out _))
        {
            player.AddComponent<ColliderVisualizer>();
            _pathProjector = player.AddComponent<PathProjector>();

            var lerpMantle = player.GetComponentInChildren<PlayerLerpMantle>();
            if (lerpMantle != null)
            {
                lerpMantle.gameObject.AddComponent<ColliderVisualizer>();
                lerpMantle.transform.parent.gameObject.AddComponent<ColliderVisualizer>();
            }
        }

        ApplyPlaybackSpeed();
        SetGizmos();

        QualitySettings.vSyncCount = 0;

        var guiCam = GameManager.GM.guiCamera;

        if (guiCam == null) return;

        var fade = guiCam.transform.Find("Canvas/Fade");

        if (fade != null)
            fade.localScale = Vector3.zero;
    }

    #endregion
}

public enum PlaybackState
{
    Stopped,
    Playing,
    Recording
}