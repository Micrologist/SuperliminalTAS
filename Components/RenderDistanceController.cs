using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components;

class RenderDistanceController : MonoBehaviour
{
#if LEGACY
    public RenderDistanceController(System.IntPtr ptr) : base(ptr) { }
#endif
    public static RenderDistanceController Instance;

    public float RenderDistance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate RenderDistanceController");
            return;
        }

        Instance = this;
        RenderDistance = 1000f;

#if LEGACY
        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)OnSceneLoad;
#else
        SceneManager.sceneLoaded += OnSceneLoad;
#endif  
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode loadMode)
    {
        ApplyRenderDistance();
    }

    public void SetRenderDistance(float distance)
    {
        RenderDistance = distance;
        ApplyRenderDistance();
    }

    private void ApplyRenderDistance()
    {
        var playerCamera = GameManager.GM.playerCamera;
        if (playerCamera == null) return;

        playerCamera.GetComponent<CameraSettingsLayer>().enabled = false;

        playerCamera.farClipPlane = RenderDistance;

        if (RenderDistance > 1000)
        {
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
            playerCamera.backgroundColor = new Color(.15f, .15f, .15f, 15f);

            playerCamera.cullingMatrix = new(Vector4.positiveInfinity,
                Vector4.positiveInfinity,
                Vector4.positiveInfinity,
                Vector4.positiveInfinity);
        }
        else
        {
            playerCamera.clearFlags = CameraClearFlags.Skybox;
            playerCamera.ResetCullingMatrix();
        }
    }
}
