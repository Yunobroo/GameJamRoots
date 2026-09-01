using UnityEngine;
using UnityEngine.InputSystem;

public class RootSwing : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform ropeStartPoint;
    [SerializeField] private LineRenderer ropeLine;

    [Header("Targeting")]
    [SerializeField] private float maxSwingDistance = 30f;
    [SerializeField] private LayerMask rootLayer;

    [Range(0f, 0.15f)]
    [SerializeField] private float aimAssistRadius = 0.045f;

    [SerializeField] private float allowedTargetBelowPlayer = 0.5f;

    [Header("Swing")]
    [SerializeField] private float swingAcceleration = 18f;

    [Tooltip("How strongly the rope corrects the player when they exceed its length.")]
    [SerializeField] private float ropeCorrectionStrength = 1f;

    [Header("Swing Boost")]
    [SerializeField] private float upwardBoost = 7f;
    [SerializeField] private float forwardBoost = 5f;
    [SerializeField] private float boostCooldown = 0.35f;

    [Header("Rope Visual")]
    [SerializeField] private float previewWidth = 0.035f;
    [SerializeField] private float activeWidth = 0.06f;

    private Rigidbody rb;
    private PlayerInput playerInput;

    private InputAction swingAction;
    private InputAction jumpAction;

    private Vector3 swingPoint;

    private float ropeLength;
    private float lastBoostTime = -999f;

    public bool IsSwinging { get; private set; }

    public bool HasSwingTarget { get; private set; }

    public Vector3 CurrentSwingTarget { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        playerInput =
            GetComponent<PlayerInput>();

        swingAction =
            playerInput.actions["Swing"];

        jumpAction =
            playerInput.actions["Jump"];

        if (ropeLine != null)
        {
            ropeLine.positionCount = 2;
            ropeLine.enabled = false;
        }
    }

    private void Update()
    {
        FindSwingTarget();

        if (swingAction.WasPressedThisFrame())
        {
            StartSwing();
        }

        if (swingAction.WasReleasedThisFrame())
        {
            StopSwing();
        }

        if (
            IsSwinging &&
            jumpAction.WasPressedThisFrame()
        )
        {
            SwingBoost();
        }

        UpdateRopeVisual();
    }

    private void FixedUpdate()
    {
        if (!IsSwinging)
            return;

        ApplySwingAcceleration();
        ApplyRopeConstraint();
    }

    private void FindSwingTarget()
    {
        HasSwingTarget = false;

        float bestDistanceFromCenter =
            Mathf.Infinity;

        Vector2[] offsets =
        {
            new Vector2(0f, 0f),

            new Vector2(1f, 0f),
            new Vector2(-1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),

            new Vector2(0.7f, 0.7f),
            new Vector2(-0.7f, 0.7f),
            new Vector2(0.7f, -0.7f),
            new Vector2(-0.7f, -0.7f)
        };

        foreach (Vector2 offset in offsets)
        {
            Vector3 viewportPoint =
                new Vector3(
                    0.5f +
                    offset.x *
                    aimAssistRadius,

                    0.5f +
                    offset.y *
                    aimAssistRadius,

                    0f
                );

            Ray ray =
                playerCamera
                .ViewportPointToRay(
                    viewportPoint
                );

            if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxSwingDistance,
                rootLayer
            ))
            {
                continue;
            }

            if (
                hit.point.y <
                transform.position.y -
                allowedTargetBelowPlayer
            )
            {
                continue;
            }

            Vector3 viewportHit =
                playerCamera
                .WorldToViewportPoint(
                    hit.point
                );

            float distanceFromCenter =
                Vector2.Distance(
                    new Vector2(
                        viewportHit.x,
                        viewportHit.y
                    ),
                    new Vector2(
                        0.5f,
                        0.5f
                    )
                );

            if (
                distanceFromCenter <
                bestDistanceFromCenter
            )
            {
                bestDistanceFromCenter =
                    distanceFromCenter;

                CurrentSwingTarget =
                    hit.point;

                HasSwingTarget = true;
            }
        }
    }

    private void StartSwing()
    {
        if (IsSwinging)
            return;

        if (!HasSwingTarget)
            return;

        swingPoint =
            CurrentSwingTarget;

        ropeLength =
            Vector3.Distance(
                transform.position,
                swingPoint
            );

        IsSwinging = true;
    }

    private void ApplyRopeConstraint()
    {
        Vector3 fromAnchor =
            rb.position -
            swingPoint;

        float currentDistance =
            fromAnchor.magnitude;

        if (currentDistance <= ropeLength)
            return;

        Vector3 ropeDirection =
            fromAnchor.normalized;

        Vector3 correctedPosition =
            swingPoint +
            ropeDirection *
            ropeLength;

        rb.position =
            Vector3.Lerp(
                rb.position,
                correctedPosition,
                ropeCorrectionStrength
            );

        Vector3 velocity =
            rb.linearVelocity;

        float outwardSpeed =
            Vector3.Dot(
                velocity,
                ropeDirection
            );

        if (outwardSpeed > 0f)
        {
            velocity -=
                ropeDirection *
                outwardSpeed;

            rb.linearVelocity =
                velocity;
        }
    }

    private void ApplySwingAcceleration()
    {
        Vector3 ropeDirection =
            (
                swingPoint -
                transform.position
            ).normalized;

        Vector3 tangentVelocity =
            Vector3.ProjectOnPlane(
                rb.linearVelocity,
                ropeDirection
            );

        if (
            tangentVelocity.sqrMagnitude <
            0.01f
        )
        {
            return;
        }

        rb.AddForce(
            tangentVelocity.normalized *
            swingAcceleration,
            ForceMode.Acceleration
        );
    }

    private void SwingBoost()
    {
        if (
            Time.time <
            lastBoostTime +
            boostCooldown
        )
        {
            return;
        }

        lastBoostTime =
            Time.time;

        Vector3 ropeDirection =
            (
                swingPoint -
                transform.position
            ).normalized;

        Vector3 forward =
            Vector3.ProjectOnPlane(
                rb.linearVelocity,
                ropeDirection
            );

        if (forward.sqrMagnitude < 0.1f)
        {
            forward =
                Vector3.ProjectOnPlane(
                    playerCamera
                    .transform
                    .forward,
                    ropeDirection
                );
        }

        if (forward.sqrMagnitude > 0.01f)
        {
            forward.Normalize();
        }

        Vector3 boost =
            Vector3.up *
            upwardBoost +
            forward *
            forwardBoost;

        rb.AddForce(
            boost,
            ForceMode.VelocityChange
        );
    }

    private void StopSwing()
    {
        if (!IsSwinging)
            return;

        IsSwinging = false;
    }

    private void UpdateRopeVisual()
    {
        if (ropeLine == null)
            return;

        if (IsSwinging)
        {
            ropeLine.enabled = true;

            ropeLine.startWidth =
                activeWidth;

            ropeLine.endWidth =
                activeWidth;

            ropeLine.SetPosition(
                0,
                GetRopeStartPosition()
            );

            ropeLine.SetPosition(
                1,
                swingPoint
            );

            return;
        }

        if (HasSwingTarget)
        {
            ropeLine.enabled = true;

            ropeLine.startWidth =
                previewWidth;

            ropeLine.endWidth =
                previewWidth;

            ropeLine.SetPosition(
                0,
                GetRopeStartPosition()
            );

            ropeLine.SetPosition(
                1,
                CurrentSwingTarget
            );
        }
        else
        {
            ropeLine.enabled = false;
        }
    }

    private Vector3 GetRopeStartPosition()
    {
        if (ropeStartPoint != null)
        {
            return ropeStartPoint.position;
        }

        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        if (HasSwingTarget)
        {
            Gizmos.DrawSphere(
                CurrentSwingTarget,
                0.15f
            );
        }

        if (IsSwinging)
        {
            Gizmos.DrawLine(
                transform.position,
                swingPoint
            );
        }
    }
}