using System.Collections.Generic;
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

    [Header("Attachment Stability")]
    [Min(0f)]
    [SerializeField] private float maximumSpeed = 8f;
    [Min(0f)]
    [SerializeField] private float maximumAngularSpeed = 3f;

    private sealed class RootAttachment
    {
        public Vector3 localContactPoint;
        public Vector3 pendingImpulse;
        public Vector3 pendingImpulsePoint;
        public ulong interactionOrder;
    }

    private Rigidbody rb;
    private ConfigurableJoint sharedRootJoint;
    private bool hasBeenActivatedByRoot;
    private ulong nextInteractionOrder;

    // A dictionary guarantees one interaction per vine, even if multiple
    // colliders or repeated overlap checks report the same vine.
    private readonly Dictionary<ProceduralRoot, RootAttachment> rootAttachments =
        new Dictionary<ProceduralRoot, RootAttachment>();

    private readonly List<ProceduralRoot> attachmentsToRemove =
        new List<ProceduralRoot>();

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
        RemoveInvalidAttachments();
        UpdateKinematicState();

        if (!rb.isKinematic)
        {
            ApplyPendingRootImpulses();
            UpdateSharedRootJoint();
            ClampMotion();
        }
        else
        {
            RemoveSharedRootJoint();
        }
    }

    public void PushFromRoot(
        ProceduralRoot root,
        Vector3 rootPoint,
        Vector3 rootDirection,
        float force
    )
    {
        if (!canBeMovedByRoots || root == null || root.IsRetracting)
            return;

        if (!rootsAreTheOnlyMover && rb.isKinematic)
            return;

        hasBeenActivatedByRoot = true;
        rb.useGravity = true;

        Collider objectCollider = GetComponent<Collider>();
        Vector3 contactPoint = objectCollider.ClosestPoint(rootPoint);
        Vector3 awayFromRoot = contactPoint - rootPoint;

        Vector3 pushDirection =
            awayFromRoot.sqrMagnitude > 0.0001f
                ? awayFromRoot.normalized
                : rootDirection.normalized;

        if (!rootAttachments.TryGetValue(root, out RootAttachment attachment))
        {
            attachment = new RootAttachment
            {
                localContactPoint = transform.InverseTransformPoint(contactPoint)
            };

            rootAttachments.Add(root, attachment);
        }

        attachment.interactionOrder = ++nextInteractionOrder;

        // Growth can report several points before the next physics tick. Store
        // one bounded combined impulse and apply it later in FixedUpdate.
        float scaledForce = Mathf.Max(0f, force) * rootForceMultiplier;
        attachment.pendingImpulse += pushDirection * scaledForce;
        attachment.pendingImpulse = Vector3.ClampMagnitude(
            attachment.pendingImpulse,
            scaledForce * 2f
        );
        attachment.pendingImpulsePoint = contactPoint;
    }

    private void RemoveInvalidAttachments()
    {
        attachmentsToRemove.Clear();

        foreach (KeyValuePair<ProceduralRoot, RootAttachment> pair in rootAttachments)
        {
            // A retracting vine remains attached so the platform follows its
            // shrinking tip. Remove the entry only after the vine is destroyed.
            if (pair.Key == null)
            {
                attachmentsToRemove.Add(pair.Key);
            }
        }

        foreach (ProceduralRoot root in attachmentsToRemove)
        {
            rootAttachments.Remove(root);
        }
    }

    private void UpdateKinematicState()
    {
        if (!rootsAreTheOnlyMover)
            return;

        // Roots-only objects begin kinematic so the player cannot move them.
        // After a vine has moved one, it remains dynamic when detached so
        // gravity can make it fall instead of freezing in midair.
        bool shouldBeKinematic =
            !hasBeenActivatedByRoot && rootAttachments.Count == 0;

        if (rb.isKinematic == shouldBeKinematic)
            return;

        rb.isKinematic = shouldBeKinematic;
    }

    private void ApplyPendingRootImpulses()
    {
        foreach (RootAttachment attachment in rootAttachments.Values)
        {
            if (attachment.pendingImpulse.sqrMagnitude <= Mathf.Epsilon)
                continue;

            rb.AddForceAtPosition(
                attachment.pendingImpulse,
                attachment.pendingImpulsePoint,
                ForceMode.Impulse
            );

            attachment.pendingImpulse = Vector3.zero;
        }
    }

    private void UpdateSharedRootJoint()
    {
        if (!stayAttachedToRoot || rootAttachments.Count == 0)
        {
            RemoveSharedRootJoint();
            return;
        }

        if (sharedRootJoint == null)
        {
            sharedRootJoint = gameObject.AddComponent<ConfigurableJoint>();
            sharedRootJoint.autoConfigureConnectedAnchor = false;
            sharedRootJoint.connectedBody = null;
            sharedRootJoint.xMotion = ConfigurableJointMotion.Locked;
            sharedRootJoint.yMotion = ConfigurableJointMotion.Locked;
            sharedRootJoint.zMotion = ConfigurableJointMotion.Locked;
            sharedRootJoint.angularXMotion = ConfigurableJointMotion.Locked;
            sharedRootJoint.angularYMotion = ConfigurableJointMotion.Locked;
            sharedRootJoint.angularZMotion = ConfigurableJointMotion.Locked;
            sharedRootJoint.enableCollision = false;
            sharedRootJoint.breakForce = Mathf.Infinity;
            sharedRootJoint.breakTorque = Mathf.Infinity;
        }

        ProceduralRoot mostRecentRoot = null;
        RootAttachment mostRecentAttachment = null;

        foreach (KeyValuePair<ProceduralRoot, RootAttachment> pair in rootAttachments)
        {
            if (
                mostRecentAttachment == null ||
                pair.Value.interactionOrder > mostRecentAttachment.interactionOrder
            )
            {
                mostRecentRoot = pair.Key;
                mostRecentAttachment = pair.Value;
            }
        }

        if (mostRecentRoot == null || mostRecentAttachment == null)
            return;

        sharedRootJoint.anchor = mostRecentAttachment.localContactPoint;
        sharedRootJoint.connectedAnchor =
            mostRecentRoot.GetWorldPoint(mostRecentRoot.PointCount - 1);
    }

    private void RemoveSharedRootJoint()
    {
        if (sharedRootJoint == null)
            return;

        Destroy(sharedRootJoint);
        sharedRootJoint = null;
    }

    private void ClampMotion()
    {
        if (maximumSpeed > 0f)
        {
            rb.linearVelocity = Vector3.ClampMagnitude(
                rb.linearVelocity,
                maximumSpeed
            );
        }

        if (maximumAngularSpeed > 0f)
        {
            rb.angularVelocity = Vector3.ClampMagnitude(
                rb.angularVelocity,
                maximumAngularSpeed
            );
        }
    }

    private void OnDisable()
    {
        RemoveSharedRootJoint();
        rootAttachments.Clear();
        attachmentsToRemove.Clear();
    }
}
