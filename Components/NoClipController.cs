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

    public bool UseNoClipCamera { get; private set; }

    private float _noClipSpeed = 20f;

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
        UseNoClipCamera = false;
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
        NoClipEnabled = false;
        if (GameManager.GM == null) return;

        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();
        if (jumpingScript != null && jumpingScript.noClip)
        {
            Destroy(jumpingScript.instanceCameraNoClip);
            jumpingScript.noClip = false;
        }
    }

    private void Update()
    {
        if (GameManager.GM.player != null && GameManager.GM.playerCamera != null && NoClipEnabled && !UseNoClipCamera)
        {
            var playerMotor = GameManager.GM.player.GetComponent<CharacterMotor>();
            var playerCam = GameManager.GM.playerCamera;

            if (playerMotor == null) return;
            playerMotor.enabled = false;

            Vector3 input = new(GameManager.GM.playerInput.GetAxis("Move Horizontal"),
                                GameManager.GM.playerInput.GetAxis("Move Vertical"),
                                ((GameManager.GM.playerInput.GetButton("Jump") ? 1 : 0) + (Input.GetKey(KeyCode.C) ? -1 : 0)));

            var dir = (playerCam.transform.right * input.x) +
                      (playerCam.transform.forward * input.y) +
                      (playerCam.transform.up * input.z);

            if (dir.sqrMagnitude > 1f) dir.Normalize();

            playerMotor.transform.Translate(dir * Time.deltaTime * _noClipSpeed, Space.World);
        }
    }

    public void ToggleNoClip()
    {
        if (GameManager.GM.player == null)
            return;

        NoClipEnabled = !NoClipEnabled;

        if (UseNoClipCamera)
        {
            var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();
            if (jumpingScript == null) return;

            if (NoClipEnabled)
            {
                StartNoClipCamera(jumpingScript);
            }
            else
            {
                EndNoClipCamera(jumpingScript);
            }

            GameManager.GM.player.SetActive(true);
        }
        else
        {
            SetPlayerComponentsEnabled(true);
        }
    }


    public void ToggleNoClipStyle()
    {
        UseNoClipCamera = !UseNoClipCamera;

        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();

        if (!UseNoClipCamera && NoClipEnabled)
        {
            if (GameManager.GM.player != null || jumpingScript != null)
                EndNoClipCamera(jumpingScript);
        }
        else if (UseNoClipCamera && NoClipEnabled)
        {
            if (GameManager.GM.player != null || jumpingScript != null)
                StartNoClipCamera(jumpingScript);
        }
    }

    private void StartNoClipCamera(LevelJumpingScript jumpingScript)
    {
#if LEGACY
        jumpingScript.CreateNoClipCamera();
#else
        _createNoClipCamera.Invoke(jumpingScript, null);
#endif

        SetUpNoClipCamera(jumpingScript);
        SetPlayerComponentsEnabled(false);
    }

    private void EndNoClipCamera(LevelJumpingScript jumpingScript)
    {
#if LEGACY
        jumpingScript.EndNoClip();
#else
        _endNoClip.Invoke(jumpingScript, null);
#endif
        SetPlayerComponentsEnabled(true);
    }

    private void SetPlayerComponentsEnabled(bool newActive)
    {
        GameManager.GM.player.SetActive(true);

        var inputController = GameManager.GM.player.GetComponent<FPSInputController>();
        if (inputController != null)
        {
            inputController.enabled = newActive;
            inputController.motor.enabled = newActive;
            inputController.motor.grounded = true;
            inputController.motor.movement.velocity = Vector3.zero;
        }

        var charController = GameManager.GM.player.GetComponent<CharacterController>();

        if (charController != null)
        {
            charController.Move(Vector3.zero);
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

    private void SetUpNoClipCamera(LevelJumpingScript jumpingScript)
    {
        var cam = jumpingScript.instanceCameraNoClip.GetComponentInChildren<Camera>();
        cam.backgroundColor = new Color(.1f, .1f, .1f);
        cam.farClipPlane = 100000;
        cam.fieldOfView = 90;
        cam.cullingMatrix = new Matrix4x4(Vector4.positiveInfinity,
            Vector4.positiveInfinity,
            Vector4.positiveInfinity,
            Vector4.positiveInfinity);

        var noclipinput = jumpingScript.instanceCameraNoClip.GetComponent<NoClipInputController>();

        if (noclipinput != null)
        {
            noclipinput.moveSpeed = _noClipSpeed;
        }
    }

    public void ChangeSpeed(float amount)
    {
        float factor = 1f + (amount * 0.1f);
        var newSpeed = Mathf.Clamp(_noClipSpeed * factor, 0.3f, 1000f);

        SetSpeed(newSpeed);
    }

    public void SetSpeed(float speed)
    {
        if (GameManager.GM == null) return;

        _noClipSpeed = speed;

        var jumpingScript = GameManager.GM.GetComponent<LevelJumpingScript>();
        if (jumpingScript != null && jumpingScript.noClip)
        {
            var noclipinput = jumpingScript.instanceCameraNoClip.GetComponent<NoClipInputController>();

            if (noclipinput != null)
            {
                noclipinput.moveSpeed = _noClipSpeed;
            }
        }
    }
}
