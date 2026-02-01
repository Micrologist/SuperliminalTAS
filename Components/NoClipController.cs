using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components;

class NoClipController : MonoBehaviour
{
#if LEGACY
    public NoClipController(System.IntPtr ptr) : base(ptr) { }
#else
    private static MethodInfo _createNoClipCamera;
    private static MethodInfo _endNoClip;
#endif

    public static NoClipController Instance;

    public bool NoClipEnabled { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate NoClipController");
            return;
        }

        Instance = this;
        NoClipEnabled = false;
#if LEGACY
        SceneManager.sceneUnloaded += (UnityEngine.Events.UnityAction<Scene>)OnSceneUnload;
#else
        SceneManager.sceneUnloaded += OnSceneUnload;

        _createNoClipCamera = typeof(LevelJumpingScript).GetMethod(
                "CreateNoClipCamera",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

        _endNoClip = typeof(LevelJumpingScript).GetMethod(
            "EndNoClip",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
#endif
    }

    private void OnSceneUnload(Scene scene)
    {
        if (GameManager.GM == null) return;

        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();
        if (jumpingScript != null && jumpingScript.noClip)
        {
            Destroy(jumpingScript.instanceCameraNoClip);
            jumpingScript.noClip = false;
            NoClipEnabled = false;
        }
    }

    public void ToggleNoClip()
    {
        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();

        if (GameManager.GM.player == null || jumpingScript == null) return;

        if (!NoClipEnabled)
        {
            NoClipController.StartNoClip(jumpingScript);
            NoClipController.SetUpNoClipCamera(jumpingScript);
            NoClipEnabled = true;
        }
        else
        {
            NoClipController.EndNoClip(jumpingScript);
            NoClipEnabled = false;
        }

        GameManager.GM.player.SetActive(true);
        NoClipController.SetPlayerComponents(!NoClipEnabled);
    }

    private static void StartNoClip(LevelJumpingScript jumpingScript)
    {
#if LEGACY
        jumpingScript.CreateNoClipCamera();
#else
        _createNoClipCamera.Invoke(jumpingScript, null);
#endif
    }

    private static void EndNoClip(LevelJumpingScript jumpingScript)
    {
#if LEGACY
        jumpingScript.EndNoClip();
#else
        _endNoClip.Invoke(jumpingScript, null);
#endif
    }

    private static void SetPlayerComponents(bool newActive)
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

    private static void SetUpNoClipCamera(LevelJumpingScript jumpingScript)
    {
        var cam = jumpingScript.instanceCameraNoClip.GetComponentInChildren<Camera>();
        cam.backgroundColor = new Color(.1f, .1f, .1f);
        cam.farClipPlane = 100000;
        cam.fieldOfView = 90;
        cam.cullingMatrix = new Matrix4x4(Vector4.positiveInfinity,
            Vector4.positiveInfinity,
            Vector4.positiveInfinity,
            Vector4.positiveInfinity);
    }

    public void ChangeSpeed(float amount)
    {
        if (GameManager.GM == null) return;

        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();
        if (jumpingScript != null && jumpingScript.noClip)
        {
            var noclipinput = jumpingScript.instanceCameraNoClip.GetComponent<NoClipInputController>();

            if (noclipinput != null)
            {
                noclipinput.moveSpeed += amount;
            }
        }
    }
}
