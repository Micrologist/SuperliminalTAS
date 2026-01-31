#if LEGACY
using BepInEx.Unity.IL2CPP.Utils;
using Il2CppInterop.Runtime.Attributes;
#endif
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using SuperliminalTAS.Demo;
using UnityEngine;
#if LEGACY
using UnityEngine.Events;
#endif
using UnityEngine.SceneManagement;

namespace SuperliminalTAS;

public sealed class TASTools : MonoBehaviour
{
#if LEGACY
    public TASTools(IntPtr ptr) : base(ptr) { }
#endif

    public static TASTools Instance { get; private set; }

    private bool _unlimitedRenderDistance;
    private bool _showGizmos;

    private PathProjector _pathProjector;
    private readonly List<ColliderVisualizer> _colliders = [];

    private MethodInfo _createNoClipCamera;
    private MethodInfo _endNoClip;

    private void Awake()
    {
        Instance = this;

#if LEGACY
        SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>)OnLoadSetup;
        SceneManager.sceneUnloaded += (UnityAction<Scene>)OnUnloadCleanUp;
#else
        SceneManager.sceneLoaded += OnLoadSetup;
        SceneManager.sceneUnloaded += OnUnloadCleanUp;
#endif
    }

    private void LateUpdate()
    {
        HandleHotkeys();
        SetGizmos();
    }

    #region Hotkeys

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            _unlimitedRenderDistance = !_unlimitedRenderDistance;
            SetRenderDistance();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            _showGizmos = !_showGizmos;
            SetGizmos();
        }

        if (Input.GetKeyDown(KeyCode.F4) && DemoRecorder.Instance.State == PlaybackState.Stopped)
        {
            ToggleNoclip();
        }
    }

    #endregion

    #region Noclip

    private void ToggleNoclip()
    {
#if LEGACY
        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();
        if (GameManager.GM.player == null || jumpingScript == null) return;

        if (!jumpingScript.noClip)
        {
            jumpingScript.CreateNoClipCamera();
#else
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
#endif
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
#if LEGACY
            jumpingScript.EndNoClip();
#else
            _endNoClip.Invoke(jumpingScript, null);
#endif
            TogglePlayerComponents(true);
        }
    }

    private static void TogglePlayerComponents(bool newActive)
    {
        var inputController = GameManager.GM.player.GetComponent<FPSInputController>();
        if (inputController != null)
        {
            inputController.enabled = newActive;
            inputController.motor.enabled = newActive;
            inputController.motor.grounded = true;
            inputController.motor.movement.velocity = Vector3.zero;
        }

        var mouseLookP = GameManager.GM.player.GetComponent<MouseLook>();
        if (mouseLookP != null)
        {
            mouseLookP.enabled = newActive;
        }

        var mouseLookC = GameManager.GM.playerCamera.GetComponent<MouseLook>();
        if (mouseLookC != null)
        {
            mouseLookC.enabled = newActive;
        }
    }

    #endregion

    #region Render Distance

    public void SetRenderDistance()
    {
        var playerCamera = GameManager.GM.playerCamera;
        if (playerCamera == null) return;

        playerCamera.GetComponent<CameraSettingsLayer>().enabled = false;

        var state = DemoRecorder.Instance.State;

        if (Application.targetFrameRate > 999 && state == PlaybackState.Playing)
        {
            playerCamera.farClipPlane = .1f;
            playerCamera.ResetCullingMatrix();
        }
        else if ((state == PlaybackState.Stopped || Application.targetFrameRate <= 50) && _unlimitedRenderDistance)
        {
            playerCamera.cullingMatrix = new Matrix4x4(Vector4.positiveInfinity, Vector4.positiveInfinity, Vector4.positiveInfinity, Vector4.positiveInfinity);
            playerCamera.farClipPlane = 100000f;

            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = new Color(.15f, .15f, .15f, 15f);
        }
        else
        {
            playerCamera.clearFlags = CameraClearFlags.Skybox;

            playerCamera.farClipPlane = 1000f;
            playerCamera.ResetCullingMatrix();
        }
    }

    #endregion

    #region Gizmos

    private void SetGizmos()
    {
        var pm = PortalInstanceTracker.instance.PortalManager;
        if (pm != null)
        {
            var prti = pm.GetComponentInChildren<PortalRenderTextureImplementation>();
            if (prti != null)
            {
#if LEGACY
                prti.defaultMainCameraCullingMask = _showGizmos ? -1 : -32969;
#else
                var cullingMaskField = AccessTools.Field(typeof(PortalRenderTextureImplementation), "defaultMainCameraCullingMask");
                var cullingMask = _showGizmos ? -1 : -32969;
                cullingMaskField.SetValue(prti, cullingMask);
                GameManager.GM.playerCamera.cullingMask = cullingMask;
#endif
            }
        }

        if (_pathProjector != null)
        {
            _pathProjector.enabled = _showGizmos;
        }
    }

    #endregion

    #region Scene Setup

    private void OnLoadSetup(Scene scene, LoadSceneMode mode)
    {
        _colliders.Clear();

        var player = GameManager.GM.player;

        if (player != null && player.GetComponent<ColliderVisualizer>() == null)
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

        SetTriggerBoxMaterial();
        SetRenderDistance();
        SetGizmos();

        QualitySettings.vSyncCount = 0;

        var guiCam = GameManager.GM.guiCamera;

        if (guiCam == null) return;

        var fade = guiCam.transform.Find("Canvas/Fade");

        if (fade != null)
            fade.localScale = Vector3.zero;
    }

    private void OnUnloadCleanUp(Scene arg0)
    {
#if LEGACY
        if (GameManager.GM == null) return;
        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();
        if(jumpingScript != null && jumpingScript.noClip)
#else
        if(GameManager.GM.TryGetComponent<LevelJumpingScript>(out var jumpingScript) && jumpingScript.noClip)
#endif
        {
            Destroy(jumpingScript.instanceCameraNoClip);
            jumpingScript.noClip = false;
        }
    }

    private void SetTriggerBoxMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));

        // Set rendering mode to transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        var color = UnityEngine.Color.yellow;
        color.a = 0.05f;
        mat.color = color;

        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color);

        var objs = GameObject.FindGameObjectsWithTag("_Interactive");

        foreach (var obj in objs)
        {
            foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.gameObject.layer == LayerMask.NameToLayer("NoClipCamera"))
                {
                    renderer.material = mat;
                }
            }
        }
    }

    #endregion
}
