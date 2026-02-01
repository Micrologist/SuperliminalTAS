using UnityEngine;
using SuperliminalTAS.Components;

#if LEGACY
using System;
#endif

/// <summary>
/// This component can be added to a game object with a box- or capsule-collider
/// to create a visual representation "gizmo" that is only visible to the NoClip camera
/// </summary>
public class ColliderVisualizer : MonoBehaviour
{
#if LEGACY
    public ColliderVisualizer(IntPtr ptr) : base(ptr) { }
#endif
    private GameObject _visualObj;
    private float _visualAlpha = 0.2f;

    private void Awake()
    {
        GenerateColliderVisualization();
    }

    public void GenerateColliderVisualization()
    {
        if (_visualObj != null)
        {
            Destroy(_visualObj);
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            _visualObj = CreateBoxVisualization(boxCollider);
            return;
        }

        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            _visualObj = CreateCapsuleVisualization(capsuleCollider);
            return;
        }

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            _visualObj = CreateCharacterControllerVisualization(characterController);
            return;
        }

        Debug.LogWarning("No BoxCollider, CapsuleCollider, or CharacterController found on " + gameObject.name);
    }

    private GameObject CreateBoxVisualization(BoxCollider boxCollider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "BoxCollider Visualization";
        box.transform.SetParent(transform, false);
        box.layer = LayerMask.NameToLayer("NoClipCamera");

        // Match the collider's center and size
        box.transform.localPosition = boxCollider.center;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localScale = boxCollider.size;

        var collider = box.GetComponent<Collider>();
        // Disable the collider to stop it from triggering collisions this frame
        collider.enabled = false;
        // Remove the collider from the visualization object
        Destroy(collider);

        // Apply green transparent material
        ApplyTransparentMaterial(box, Color.green);

        return box;
    }

    private GameObject CreateCapsuleVisualization(CapsuleCollider capsuleCollider)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "CapsuleCollider Visualization";
        capsule.transform.SetParent(transform, false);
        capsule.layer = LayerMask.NameToLayer("NoClipCamera");

        // Unity's default capsule primitive:
        // - Height: 2 units
        // - Radius: 0.5 units (diameter: 1 unit)
        // - Oriented along Y-axis

        float colliderHeight = capsuleCollider.height;
        float colliderRadius = capsuleCollider.radius;
        int direction = capsuleCollider.direction; // 0=X, 1=Y, 2=Z

        // Calculate scale factors
        // The primitive capsule has height 2 and radius 0.5
        float radiusScale = colliderRadius / 0.5f;
        float heightScale = colliderHeight / 2.0f;

        // Apply scale based on capsule direction
        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;

        switch (direction)
        {
            case 0: // X-axis
                scale = new Vector3(heightScale, radiusScale, radiusScale);
                rotation = Quaternion.Euler(0, 0, 90);
                break;
            case 1: // Y-axis (default)
                scale = new Vector3(radiusScale, heightScale, radiusScale);
                rotation = Quaternion.identity;
                break;
            case 2: // Z-axis
                scale = new Vector3(radiusScale, radiusScale, heightScale);
                rotation = Quaternion.Euler(90, 0, 0);
                break;
        }

        capsule.transform.localPosition = capsuleCollider.center;
        capsule.transform.localRotation = rotation;
        capsule.transform.localScale = scale;

        var collider = capsule.GetComponent<Collider>();
        // Disable the collider to stop it from triggering collisions this frame
        collider.enabled = false;
        // Remove the collider from the visualization object
        Destroy(collider);

        // Apply red transparent material
        ApplyTransparentMaterial(capsule, Color.red);

        return capsule;
    }

    private GameObject CreateCharacterControllerVisualization(CharacterController characterController)
    {
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "CharacterController Visualization";
        capsule.transform.SetParent(transform, false);
        capsule.layer = LayerMask.NameToLayer("NoClipCamera");

        // CharacterController is always oriented along the Y-axis
        // Unity's default capsule primitive:
        // - Height: 2 units
        // - Radius: 0.5 units (diameter: 1 unit)
        // - Oriented along Y-axis

        float controllerHeight = characterController.height;
        float controllerRadius = characterController.radius;

        // Calculate scale factors
        float radiusScale = controllerRadius / 0.5f;
        float heightScale = controllerHeight / 2.0f;

        // CharacterController is always Y-axis oriented
        Vector3 scale = new Vector3(radiusScale, heightScale, radiusScale);

        capsule.transform.localPosition = characterController.center;
        capsule.transform.localRotation = Quaternion.identity;
        capsule.transform.localScale = scale;

        var collider = capsule.GetComponent<Collider>();
        // Disable the collider to stop it from triggering collisions this frame
        collider.enabled = false;
        // Remove the collider from the visualization object
        Destroy(collider);

        // Apply blue transparent material for CharacterController
        ApplyTransparentMaterial(capsule, Color.blue);

        return capsule;
    }

    private void ApplyTransparentMaterial(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            color.a = _visualAlpha;
            var mat = Utility.GetTransparentMaterial(color);
            renderer.material = mat;
        }
    }

    public void HideVisualization()
    {
        if (_visualObj != null)
        {
            _visualObj.SetActive(false);
        }
    }

    public void ShowVisualization()
    {
        if (_visualObj != null)
        {
            _visualObj.SetActive(true);
        }
    }

    public void DestroyVisualization()
    {
        if (_visualObj != null)
        {
            Destroy(_visualObj);
            _visualObj = null;
        }
    }

    private void OnDestroy()
    {
        DestroyVisualization();
    }

    private void OnEnable()
    {
        if (_visualObj != null)
        {
            _visualObj.SetActive(true);
        }
    }

    private void OnDiable()
    {
        if (_visualObj != null)
        {
            _visualObj.SetActive(false);
        }
    }
}
