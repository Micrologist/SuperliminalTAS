using UnityEngine;

namespace SuperliminalTools.Components.Visual;

public class PathProjector : MonoBehaviour
{
#if LEGACY
    public PathProjector(System.IntPtr ptr) : base(ptr) { }
#endif
    private float maxDistance = 100f;
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

        if (characterController == null)
        {
            Debug.LogWarning("PathProjector was added to a gameobject without a charactercontroller!");
        }

        visualizationMaterial = SuperliminalTools.Components.Utility.GetTransparentMaterial(hitColor);
        CreateVisualCapsule();
    }

    void CreateVisualCapsule()
    {
        // Create capsule primitive
        visualCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visualCapsule.name = "PathProjector_Visual";
        visualCapsule.layer = LayerMask.NameToLayer("NoClipCamera");

        // Remove collider
        var col = visualCapsule.GetComponent<Collider>();
        col.enabled = false;
        Destroy(col);

        // Get renderer
        capsuleRenderer = visualCapsule.GetComponent<MeshRenderer>();

        capsuleRenderer.material = visualizationMaterial;
    }

    void Update()
    {
        if (characterController != null)
        {
            ProjectPath();
            UpdateVisualization();
        }
    }

    public void ProjectPath()
    {
        float currentRadius = characterController.radius * transform.lossyScale.x * radiusMultiplier;

        Vector3 dir = transform.forward;
        dir.y = 0;
        dir.Normalize();

        Vector3 scaledCenter = Vector3.Scale(characterController.center, transform.lossyScale);
        Vector3 origin = transform.position + scaledCenter;

        origin += dir * (currentRadius + 0.02f);

        bool hit = Physics.SphereCast(
            origin,
            currentRadius,
            dir,
            out lastHit,
            maxDistance,
            (LayerMask)0x6BBDFFFF, // Exclude layers 17, 22, 26, 28, 31
            QueryTriggerInteraction.Ignore
        );

        var col = hit ? lastHit.collider : null;

        if (!hit || col == null)
        {
            hasHit = false;
            return;
        }

        hasHit = true;
        projectedHitPoint = lastHit.point;

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

    private void OnEnable()
    {
        if (visualCapsule != null)
        {
            visualCapsule.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (visualCapsule != null)
        {
            visualCapsule.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (visualCapsule != null)
        {
            Destroy(visualCapsule);
        }
    }
}
