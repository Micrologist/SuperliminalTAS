using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components.Visual;

class FlashlightController : MonoBehaviour
{
#if LEGACY
    public FlashlightController(System.IntPtr ptr) : base(ptr) { }
#endif

    public static FlashlightController Instance { get; private set; }

    public bool FlashlightEnabled { get; private set; }

    private GameObject _flashlight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate FlashlightController");
            return;
        }

        Instance = this;
        FlashlightEnabled = false;

#if LEGACY
        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)OnSceneLoaded;
#else
        SceneManager.sceneLoaded += OnSceneLoaded;
#endif
    }

    public void SetEnabled(bool enabled)
    {
        FlashlightEnabled = enabled;

        if (_flashlight != null)
        {
            _flashlight.SetActive(FlashlightEnabled);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (GameManager.GM.player != null)
        {
            if (_flashlight != null)
                Destroy(_flashlight);
            _flashlight = new GameObject("Flashlight");
            _flashlight.transform.parent = GameManager.GM.player.transform;
            _flashlight.transform.localPosition = new(0f, .85f, 0f);
            var light = _flashlight.AddComponent<Light>();
            light.range = 10000f;
            light.intensity = 0.5f;
            light.color = new(1, 1, .9f);

            _flashlight.SetActive(FlashlightEnabled);
        }
    }
}