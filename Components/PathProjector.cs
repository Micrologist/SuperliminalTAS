using System;
using UnityEngine;

public class PathProjector : MonoBehaviour
{
#if LEGACY
    public PathProjector(IntPtr ptr) : base(ptr) { }
#endif
    private float maxDistance = 100f;
    private LayerMask collisionLayers = -1;
    private float radiusMultiplier = 1f;

    private Color hitColor = new Color(1f, 0f, 1f, .25f);
    private Color noHitColor = new Color(0f, 1f, 0f, 0f);
    private Material visualizationMaterial;


    private CharacterController characterController;
    private GameObject visualCapsule;
    private MeshRenderer capsuleRenderer;
    private Vector3 projectedHitPoint;
    private bool hasHit;
    private RaycastHit lastHit;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        collisionLayers = GetCharacterControllerCollisionLayers();

        CreateVisualCapsule();
    }

    void CreateVisualCapsule()
    {
        // Create capsule primitive
        visualCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visualCapsule.name = "PathProjector_Visual";
        visualCapsule.layer = LayerMask.NameToLayer("NoClipCamera");

        // Remove collider - we don't want it to interfere
        Destroy(visualCapsule.GetComponent<Collider>());

        // Get renderer
        capsuleRenderer = visualCapsule.GetComponent<MeshRenderer>();

        // Setup material
        if (visualizationMaterial != null)
        {
            capsuleRenderer.material = visualizationMaterial;
        }
        else
        {
            // Create transparent material if none provided
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            capsuleRenderer.material = mat;
        }
    }

    void Update()
    {
        ProjectPath();
        UpdateVisualization();
    }

    public void ProjectPath()
    {
        // Account for scale changes
        float currentRadius = characterController.radius * transform.lossyScale.x * radiusMultiplier;

        // Get forward direction on XZ plane (no Y change)
        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0;
        forwardDirection.Normalize();

        // Origin at character controller's center (accounting for scale)
        Vector3 scaledCenter = Vector3.Scale(characterController.center, transform.lossyScale);
        Vector3 origin = transform.position + scaledCenter;

        // Perform sphere cast
        hasHit = Physics.SphereCast(
            origin,
            currentRadius,
            forwardDirection,
            out lastHit,
            maxDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hasHit)
        {
            projectedHitPoint = lastHit.point;
        }
    }

    void UpdateVisualization()
    {
        if (visualCapsule == null)
        {
            return;
        }

        // Account for current scale
        float currentRadius = characterController.radius * transform.lossyScale.x * radiusMultiplier;
        float currentHeight = characterController.height * transform.lossyScale.y;
        Vector3 scaledCenter = Vector3.Scale(characterController.center, transform.lossyScale);

        Vector3 forwardDirection = transform.forward;
        forwardDirection.y = 0;
        forwardDirection.Normalize();

        float projectionDistance;

        if (hasHit)
        {
            // Use the actual hit distance
            projectionDistance = lastHit.distance;
            capsuleRenderer.material.color = hitColor;
            capsuleRenderer.enabled = true;
        }
        else
        {
            // Use max distance
            projectionDistance = maxDistance;
            capsuleRenderer.material.color = noHitColor;
            capsuleRenderer.enabled = false;
        }

        // Position capsule projected forward from character's position
        Vector3 characterPosition = transform.position + scaledCenter;
        visualCapsule.transform.position = characterPosition + forwardDirection * projectionDistance;


        // Match rotation (capsules are Y-up by default, keep it upright)
        visualCapsule.transform.rotation = Quaternion.identity;

        // Scale capsule to match character controller dimensions
        // Unity capsule primitive is 2 units tall and 1 unit diameter
        float scaleX = currentRadius * 2f; // Diameter
        float scaleY = currentHeight / 2f; // Half height since primitive is 2 units
        float scaleZ = currentRadius * 2f; // Diameter

        visualCapsule.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
    }

    private LayerMask GetCharacterControllerCollisionLayers()
    {
        // Get the layer of the character controller's game object
        int characterLayer = gameObject.layer;

        // Build a layer mask of all layers this character can collide with
        LayerMask mask = 0;

        for (int i = 0; i < 32; i++)
        {
            // Check if physics collision is enabled between character layer and layer i
            if (!Physics.GetIgnoreLayerCollision(characterLayer, i))
            {
                mask |= (1 << i);
            }
        }

        return mask;
    }

    public bool WillCollide()
    {
        return hasHit;
    }

    public Vector3 GetCollisionPoint()
    {
        return hasHit ? projectedHitPoint : Vector3.zero;
    }

    public float GetDistanceToCollision()
    {
        return hasHit ? lastHit.distance : maxDistance;
    }

    public GameObject GetCollisionObject()
    {
        return hasHit ? lastHit.collider.gameObject : null;
    }

    public Vector3 GetCollisionNormal()
    {
        return hasHit ? lastHit.normal : Vector3.zero;
    }

    public RaycastHit GetHitInfo()
    {
        return lastHit;
    }

    void OnDestroy()
    {
        if (visualCapsule != null)
        {
            Destroy(visualCapsule);
        }
    }
}
