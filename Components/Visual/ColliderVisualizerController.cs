using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components.Visual;

class ColliderVisualizerController : MonoBehaviour
{
#if LEGACY
    public ColliderVisualizerController(System.IntPtr ptr) : base(ptr) { }
#endif

    public static ColliderVisualizerController Instance { get; private set; }

    public bool ShowColliders { get; private set; }

    private readonly List<ColliderVisualizer> _colliders = [];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate ColliderVisualizerController");
            return;
        }

        Instance = this;
        ShowColliders = true;

#if LEGACY
        SceneManager.sceneLoaded += (UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)OnLoadScene;
#else
        SceneManager.sceneLoaded += OnLoadScene;
#endif

    }

    public void SetCollidersVisible(bool enabled)
    {
        foreach (var collider in _colliders)
        {
            collider.enabled = enabled;
        }

        ShowColliders = enabled;
    }

    private void OnLoadScene(Scene scene, LoadSceneMode loadSceneMode)
    {
        _colliders.Clear();

        var player = GameManager.GM.player;

        if (player != null && player.GetComponent<ColliderVisualizer>() == null)
        {
            _colliders.Add(player.AddComponent<ColliderVisualizer>());

            var lerpMantle = player.GetComponentInChildren<PlayerLerpMantle>();
            if (lerpMantle != null)
            {
                _colliders.Add(lerpMantle.gameObject.AddComponent<ColliderVisualizer>());
                _colliders.Add(lerpMantle.transform.parent.gameObject.AddComponent<ColliderVisualizer>());
            }

            SetCollidersVisible(ShowColliders);
        }
    }
}