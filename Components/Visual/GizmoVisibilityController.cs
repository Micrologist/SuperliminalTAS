using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components.Visual;

class GizmoVisibilityController : MonoBehaviour
{
#if LEGACY
    public GizmoVisibilityController(System.IntPtr ptr) : base(ptr) { }
#endif

    public static GizmoVisibilityController Instance { get; private set; }

    public bool ShowGizmos { get; private set; }

    private Material _triggerBoxMaterial;

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
        // -32969 == ~(1 << 3 | 1 << 6 | 1 << 7 | 1 << 15): hides layers 3, 6, 7, and 15 (NoClipCamera)
        var cullingMask = ShowGizmos ? -1 : ~(1 << 3 | 1 << 6 | 1 << 7 | 1 << 15);

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
        if (_triggerBoxMaterial == null)
        {
            var color = Color.yellow;
            color.a = 0.05f;
            _triggerBoxMaterial = Utility.GetTransparentMaterial(color);
            _triggerBoxMaterial.EnableKeyword("_EMISSION");
            _triggerBoxMaterial.SetColor("_EmissionColor", color);
        }

        var mat = _triggerBoxMaterial;

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