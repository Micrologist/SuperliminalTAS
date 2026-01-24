using System.Text.RegularExpressions;
using UnityEngine;

public class ColliderVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showOnAwake = true;

    private GameObject visualRepresentation;

    private void Awake()
    {
        if (showOnAwake)
        {
            GenerateColliderVisualization();
        }
    }

    public void GenerateColliderVisualization()
    {
        // Clean up existing visualization
        if (visualRepresentation != null)
        {
            Destroy(visualRepresentation);
        }

        // Try to get BoxCollider
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            visualRepresentation = CreateBoxVisualization(boxCollider);
            return;
        }

        // Try to get CapsuleCollider
        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            visualRepresentation = CreateCapsuleVisualization(capsuleCollider);
            return;
        }

        // Try to get CharacterController
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            visualRepresentation = CreateCharacterControllerVisualization(characterController);
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

        // Remove the collider from the visualization object
        Destroy(box.GetComponent<Collider>());

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

        // Remove the collider from the visualization object
        Destroy(capsule.GetComponent<Collider>());

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

        // Remove the collider from the visualization object
        Destroy(capsule.GetComponent<Collider>());

        // Apply blue transparent material for CharacterController
        ApplyTransparentMaterial(capsule, Color.blue);

        return capsule;
    }

    private void ApplyTransparentMaterial(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a new material with transparency
            Material mat = new Material(Shader.Find("Standard"));

            // Set rendering mode to transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            // Set the color with alpha 0.3
            color.a = 0.25f;
            mat.color = color;

            renderer.material = mat;
        }
    }

    public void HideVisualization()
    {
        if (visualRepresentation != null)
        {
            visualRepresentation.SetActive(false);
        }
    }

    public void ShowVisualization()
    {
        if (visualRepresentation != null)
        {
            visualRepresentation.SetActive(true);
        }
    }

    public void DestroyVisualization()
    {
        if (visualRepresentation != null)
        {
            Destroy(visualRepresentation);
            visualRepresentation = null;
        }
    }

    private void OnDestroy()
    {
        DestroyVisualization();
    }
}