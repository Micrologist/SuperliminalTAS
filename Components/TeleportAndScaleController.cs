using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SuperliminalTools.Components;

class TeleportAndScaleController : MonoBehaviour
{
#if LEGACY
    public TeleportAndScaleController(IntPtr ptr) : base(ptr) { }
#endif

    public static TeleportAndScaleController Instance;

    private Vector3 _storedPosition = Vector3.zero;
    private Quaternion _storedCapsuleRotation = Quaternion.identity;
    private float _storedCamRotation = 0f;
    private float _storedScale = 1f;
    private int _storedMapIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            Debug.LogError("Duplicate TeleportAndScaleController");
            return;
        }

        Instance = this;
    }

    public void StorePosition()
    {
        if (GameManager.GM == null || GameManager.GM.player == null || GameManager.GM.playerCamera == null) return;

        var player = GameManager.GM.player;

        _storedPosition = player.transform.position;
        _storedCapsuleRotation = player.transform.rotation;
        _storedCamRotation = GameManager.GM.playerCamera.GetComponent<MouseLook>().rotationY;
        _storedScale = player.transform.localScale.x;
        _storedMapIndex = SceneManager.GetActiveScene().buildIndex;
    }

    public void TeleportToStoredPosition()
    {
        if (GameManager.GM == null || GameManager.GM.player == null || GameManager.GM.playerCamera == null) return;

        var player = GameManager.GM.player;

        if (SceneManager.GetActiveScene().buildIndex != _storedMapIndex) return;

        player.transform.position = _storedPosition;
        player.transform.rotation = _storedCapsuleRotation;
        GameManager.GM.playerCamera.GetComponent<MouseLook>().SetRotationY(_storedCamRotation);
        ScalePlayer(_storedScale);
    }

    public void ScalePlayer(float factor)
    {
        factor = Mathf.Clamp(factor, 0.0000001f, 999999999f);
        if (GameManager.GM != null && GameManager.GM.player != null)
        {
            GameManager.GM.player.transform.localScale *= factor;
            GameManager.GM.player.GetComponent<PlayerResizer>().Poke();
        }
    }

    public void SetPlayerScale(float newScale)
    {
        newScale = Mathf.Clamp(newScale, 0.0000001f, 999999999f);

        if (GameManager.GM != null && GameManager.GM.player != null)
        {
            GameManager.GM.player.transform.localScale = new(newScale, newScale, newScale);
            GameManager.GM.player.GetComponent<PlayerResizer>().Poke();
        }
    }
}