using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MovableObjects : MonoBehaviour
{
    [Header("Root Interaction")]
    [SerializeField] private bool canBeMovedByRoots = true;
    [SerializeField] private bool stayAttachedToRoot = true;
    [Tooltip("Prevents the player and other physics objects from moving this object.")]
    [SerializeField] private bool rootsAreTheOnlyMover = true;
    [SerializeField] private float rootForceMultiplier = 1f;
    [SerializeField] private float maximumSpeed = 8f;

    private Rigidbody rb;
    private ProceduralRoot attachedRoot;
    private ConfigurableJoint rootJoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rootsAreTheOnlyMover)
        {
            rb.isKinematic = true;
        }
    }

    private void FixedUpdate()
    {
        UpdateRootAttachment();

        if (maximumSpeed <= 0f)
            return;

        if (rb.linearVelocity.sqrMagnitude > maximumSpeed * maximumSpeed)
        {
            rb.linearVelocity =
                Vector3.ClampMagnitude(
                    rb.linearVelocity,
                    maximumSpeed
                );
        }
    }

    public void PushFromRoot(
        ProceduralRoot root,
        Vector3 rootPoint,
        Vector3 rootDirection,
        float force
    )
    {
        if (!canBeMovedByRoots)
            return;

        if (rootsAreTheOnlyMover)
        {
            rb.isKinematic = false;
        }
        else if (rb.isKinematic)
        {
            return;
        }

        Collider objectCollider =
            GetComponent<Collider>();

        Vector3 contactPoint =
            objectCollider.ClosestPoint(
                rootPoint
            );

        Vector3 awayFromRoot =
            contactPoint - rootPoint;

        Vector3 pushDirection =
            awayFromRoot.sqrMagnitude > 0.0001f
                ? awayFromRoot.normalized
                : rootDirection.normalized;

        rb.AddForceAtPosition(
            pushDirection * force * rootForceMultiplier,
            contactPoint,
            ForceMode.Impulse
        );

        if (stayAttachedToRoot && attachedRoot == null)
        {
            AttachToRoot(
                root,
                rootPoint,
                contactPoint
            );
        }
    }

    private void AttachToRoot(
        ProceduralRoot root,
        Vector3 rootPoint,
        Vector3 contactPoint
    )
    {
        if (root == null)
            return;

        attachedRoot = root;

        rootJoint =
            gameObject.AddComponent<ConfigurableJoint>();

        rootJoint.autoConfigureConnectedAnchor = false;
        rootJoint.connectedBody = null;
        rootJoint.anchor =
            transform.InverseTransformPoint(
                contactPoint
            );
        rootJoint.connectedAnchor = rootPoint;
        rootJoint.xMotion = ConfigurableJointMotion.Locked;
        rootJoint.yMotion = ConfigurableJointMotion.Locked;
        rootJoint.zMotion = ConfigurableJointMotion.Locked;
        rootJoint.angularXMotion = ConfigurableJointMotion.Locked;
        rootJoint.angularYMotion = ConfigurableJointMotion.Locked;
        rootJoint.angularZMotion = ConfigurableJointMotion.Locked;
        rootJoint.enableCollision = false;
        rootJoint.breakForce = Mathf.Infinity;
        rootJoint.breakTorque = Mathf.Infinity;
    }

    private void UpdateRootAttachment()
    {
        if (rootJoint == null)
            return;

        if (attachedRoot == null)
        {
            Destroy(rootJoint);
            rootJoint = null;

            if (rootsAreTheOnlyMover)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            return;
        }

        rootJoint.connectedAnchor =
            attachedRoot.GetWorldPoint(
                attachedRoot.PointCount - 1
            );
    }
}
