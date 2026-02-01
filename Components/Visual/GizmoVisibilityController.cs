using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components.Visual;

class GizmoVisibilityController : MonoBehaviour
{
#if LEGACY
    public GizmoVisibilityController(System.IntPtr ptr) : base(ptr) { }
#endif

    public static GizmoVisibilityController Instance;

    public bool ShowGizmos { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate GizmoVisibilityController");
            return;
        }

        Instance = this;
        ShowGizmos = false;

#if LEGACY
        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
#else
        SceneManager.sceneLoaded += OnSceneLoaded;
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        SetDefaultTriggerBoxMaterial();
        SetCameraCullingMask();
    }

    public void ToggleGizmosVisible()
    {
        ShowGizmos = !ShowGizmos;
        SetCameraCullingMask();
        SetDefaultTriggerBoxMaterial();
    }

    private void SetCameraCullingMask()
    {
        var cullingMask = ShowGizmos ? -1 : -32969;

        if (GameManager.GM.playerCamera != null)
            GameManager.GM.playerCamera.cullingMask = cullingMask;

        var pm = PortalInstanceTracker.instance.PortalManager;
        if (pm != null)
        {
            var prti = pm.GetComponentInChildren<PortalRenderTextureImplementation>();
            if (prti != null)
            {
#if LEGACY
                prti.defaultMainCameraCullingMask = cullingMask;
#else
                var cullingMaskField = HarmonyLib.AccessTools.Field(typeof(PortalRenderTextureImplementation), "defaultMainCameraCullingMask");
                cullingMaskField.SetValue(prti, cullingMask);
#endif
            }
        }
    }

    private void SetDefaultTriggerBoxMaterial()
    {
        var color = Color.yellow;
        color.a = 0.05f;
        Material mat = Utility.GetTransparentMaterial(color);

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
}