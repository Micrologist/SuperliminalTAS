using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components.Visual;

class PathProjectorController : MonoBehaviour
{
#if LEGACY
    public PathProjectorController(System.IntPtr ptr) : base(ptr) { }
#endif

    public static PathProjectorController Instance { get; private set; }

    public bool ProjectorEnabled { get; private set; }

    private PathProjector _pathProjector;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate PathProjectorController");
            return;
        }

        Instance = this;
        ProjectorEnabled = true;
#if LEGACY
        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
#else
        SceneManager.sceneLoaded += OnSceneLoaded;
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (GameManager.GM == null) return;

        if (GameManager.GM.player != null && GameManager.GM.player.GetComponent<PathProjector>() == null)
        {
            _pathProjector = GameManager.GM.player.AddComponent<PathProjector>();
            _pathProjector.enabled = ProjectorEnabled;
        }
    }

    public void SetEnabled(bool enabled)
    {
        ProjectorEnabled = enabled;

        if (_pathProjector != null)
        {
            _pathProjector.enabled = enabled;
        }
    }
}