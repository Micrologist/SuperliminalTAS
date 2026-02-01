using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


namespace SuperliminalTAS.Components;

class PathProjectorController : MonoBehaviour
{
#if LEGACY
    public PathProjectorController(IntPtr ptr) : base(ptr) { }
#endif

    public static PathProjectorController Instance;

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
        SceneManager.sceneLoaded += (UnityAction<Scene,LoadSceneMode>)OnSceneLoaded;
#else
        SceneManager.sceneLoaded += OnSceneLoaded;
#endif
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (GameManager.GM == null) return;

        if(GameManager.GM.player != null && GameManager.GM.player.GetComponent<PathProjector>() == null)
        {
            _pathProjector = GameManager.GM.player.AddComponent<PathProjector>();
            _pathProjector.enabled = ProjectorEnabled;
        }
    }

    public void SetEnabled(bool enabled)
    {
        ProjectorEnabled = enabled;

        if(_pathProjector != null)
        {
            _pathProjector.enabled = enabled;   
        }
    }
}