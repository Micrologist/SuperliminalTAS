using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components;

class FadeController : MonoBehaviour
{
#if LEGACY
    public FadeController(System.IntPtr ptr) : base(ptr) { }
#endif

    public static FadeController Instance;

    public bool DisableFades { get; set; }


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate FadeController");
            return;
        }

        Instance = this;
        DisableFades = true;

#if LEGACY
        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
#else
        SceneManager.sceneLoaded += OnSceneLoaded;
#endif
    }

    private void OnSceneLoaded(Scene scnee, LoadSceneMode loadSceneMode)
    {
        if (GameManager.GM == null) return;

        var cam = GameManager.GM.guiCamera;

        if (cam != null && DisableFades)
        {
            var fade = cam.transform.Find("Canvas/Fade");

            if (fade != null)
                fade.localScale = Vector3.zero;
        }
    }
}