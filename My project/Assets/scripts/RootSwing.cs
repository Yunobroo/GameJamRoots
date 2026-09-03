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

    [Tooltip("Radius around the crosshair used to initially detect a root.")]
    [Range(0f, 0.15f)]
    [SerializeField] private float aimAssistRadius = 0.085f;

    [Tooltip("Maximum screen-space distance a root point can be from the crosshair.")]
    [Range(0.01f, 0.25f)]
    [SerializeField] private float magneticTargetRadius = 0.14f;

    [SerializeField] private float allowedTargetBelowPlayer = 0.5f;

    [Header("Swing")]
    [SerializeField] private float swingAcceleration = 22f;

    [Tooltip("How much upward direction can be added while swinging.")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalSwingInfluence = 0.45f;

    [Tooltip("How long the rope is compared to the distance from player to anchor when attaching.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float ropeLengthMultiplier = 0.6f;

    [Tooltip("How strongly the rope corrects the player when they exceed its length.")]
    [Range(0f, 1f)]
    [SerializeField] private float ropeCorrectionStrength = 1f;

    [Header("Swing Boost")]
    [SerializeField] private float upwardBoost = 4.5f;
    [SerializeField] private float forwardBoost = 7f;
    [SerializeField] private float boostCooldown = 0.45f;

    [SerializeField] private float maxUpwardSwingSpeed = 10f;

    [Header("Root Pull")]
    [SerializeField] private float pullLaunchSpeed = 22f;
    [Tooltip("Extra strength applied when pulling toward a point below the player.")]
    [SerializeField] private float downwardPullMultiplier = 1.5f;
    [SerializeField] private float pullAimRadius = 0.3f;
    [SerializeField] private float pullVisualDuration = 0.25f;

    [Header("Rope Visual")]
    [SerializeField] private float previewWidth = 0.035f;
    [SerializeField] private float activeWidth = 0.06f;

    private Rigidbody rb;
    private PlayerInput playerInput;

    private InputAction swingAction;
    private InputAction jumpAction;
    private InputAction rootPullAction;

    private Vector3 swingPoint;

    private float ropeLength;
    private float lastBoostTime = -999f;

    private Vector3 pullPoint;
    private RootGrower rootGrower;
    private PlayerMovement playerMovement;
    private float pullVisualEndTime;
    private bool pullAvailable = true;

    public bool IsSwinging { get; private set; }
    public bool IsPulling { get; private set; }
    public bool IsRootMovementActive => IsSwinging || IsPulling;

    public bool HasSwingTarget { get; private set; }

    public Vector3 CurrentSwingTarget { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        rootGrower = GetComponent<RootGrower>();
        playerMovement = GetComponent<PlayerMovement>();

        swingAction =
            playerInput.actions["Swing"];

        jumpAction =
            playerInput.actions["Jump"];

        rootPullAction =
            playerInput.actions["RootPull"];

        if (ropeLine != null)
        {
            ropeLine.positionCount = 2;
            ropeLine.enabled = false;
        }
    }

    private void Update()
    {
        if (
            playerMovement != null &&
            playerMovement.IsGroundedNow()
        )
        {
            pullAvailable = true;
        }

        if (!IsSwinging)
        {
            FindSwingTarget();
        }

        if (swingAction.WasPressedThisFrame())
        {
            StartSwing();
        }

        if (swingAction.WasReleasedThisFrame())
        {
            StopSwing();
        }

        if (rootPullAction.WasPressedThisFrame())
        {
            StartRootPull();
        }

        if (
            IsPulling &&
            Time.unscaledTime >= pullVisualEndTime
        )
        {
            StopRootPull();
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
        LimitUpwardVelocity();
    }

    private void StartRootPull()
    {
        if (
            IsSwinging ||
            IsPulling ||
            !pullAvailable ||
            playerMovement == null ||
            playerMovement.IsGroundedNow()
        )
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (
            rootGrower == null ||
            !rootGrower.TryGetGrowablePoint(
                ray,
                pullAimRadius,
                maxSwingDistance,
                out RaycastHit hit
            )
        )
        {
            return;
        }

        pullPoint = hit.point;

        Vector3 launchDirection =
            pullPoint - rb.position;

        if (launchDirection.y < 0f)
        {
            launchDirection.y *=
                downwardPullMultiplier;
        }

        launchDirection.Normalize();

        rb.linearVelocity =
            launchDirection * pullLaunchSpeed;

        pullAvailable = false;
        IsPulling = true;
        pullVisualEndTime =
            Time.unscaledTime + pullVisualDuration;
    }

    private void StopRootPull()
    {
        IsPulling = false;
    }

    private void FindSwingTarget()
    {
        HasSwingTarget = false;

        ProceduralRoot bestRoot = null;

        float bestRootDistance =
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

            ProceduralRoot root =
                hit.collider
                .GetComponentInParent<ProceduralRoot>();

            if (root == null)
                continue;

            Vector3 viewportHit =
                playerCamera
                .WorldToViewportPoint(
                    hit.point
                );

            if (viewportHit.z <= 0f)
                continue;

            float screenDistance =
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
                screenDistance <
                bestRootDistance
            )
            {
                bestRootDistance =
                    screenDistance;

                bestRoot =
                    root;
            }
        }

        if (bestRoot == null)
            return;

        FindBestPointOnRoot(bestRoot);
    }

    private void FindBestPointOnRoot(
        ProceduralRoot root
    )
    {
        float bestScreenDistance =
            Mathf.Infinity;

        Vector3 bestPoint =
            Vector3.zero;

        bool foundPoint =
            false;

        for (
            int i = 0;
            i < root.PointCount;
            i++
        )
        {
            Vector3 point =
                root.GetWorldPoint(i);

            if (
                point.y <
                transform.position.y -
                allowedTargetBelowPlayer
            )
            {
                continue;
            }

            float worldDistance =
                Vector3.Distance(
                    transform.position,
                    point
                );

            if (
                worldDistance >
                maxSwingDistance
            )
            {
                continue;
            }

            Vector3 viewportPoint =
                playerCamera
                .WorldToViewportPoint(
                    point
                );

            if (viewportPoint.z <= 0f)
                continue;

            Vector2 screenPoint =
                new Vector2(
                    viewportPoint.x,
                    viewportPoint.y
                );

            float screenDistance =
                Vector2.Distance(
                    screenPoint,
                    new Vector2(
                        0.5f,
                        0.5f
                    )
                );

            if (
                screenDistance >
                magneticTargetRadius
            )
            {
                continue;
            }

            if (
                screenDistance <
                bestScreenDistance
            )
            {
                bestScreenDistance =
                    screenDistance;

                bestPoint =
                    point;

                foundPoint =
                    true;
            }
        }

        if (!foundPoint)
            return;

        CurrentSwingTarget =
            bestPoint;

        HasSwingTarget =
            true;
    }

    private void StartSwing()
    {
        if (IsSwinging || IsPulling)
            return;

        if (!HasSwingTarget)
            return;

        swingPoint =
            CurrentSwingTarget;

        float distanceToAnchor =
            Vector3.Distance(
                transform.position,
                swingPoint
            );

        ropeLength =
            distanceToAnchor *
            ropeLengthMultiplier;

        IsSwinging = true;
    }

    private void ApplyRopeConstraint()
    {
        Vector3 fromAnchor =
            rb.position -
            swingPoint;

        float currentDistance =
            fromAnchor.magnitude;

        if (
            currentDistance <=
            ropeLength
        )
        {
            return;
        }

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

        Vector3 accelerationDirection =
            tangentVelocity.normalized;

        /*
         * Never actively accelerate downward.
         * Gravity handles the downward part
         * of the swing.
         */
        if (accelerationDirection.y < 0f)
        {
            accelerationDirection.y = 0f;
        }
        else
        {
            accelerationDirection.y *=
                verticalSwingInfluence;
        }

        if (
            accelerationDirection.sqrMagnitude <
            0.01f
        )
        {
            return;
        }

        accelerationDirection.Normalize();

        rb.AddForce(
            accelerationDirection *
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

        if (
            forward.sqrMagnitude <
            0.1f
        )
        {
            forward =
                Vector3.ProjectOnPlane(
                    playerCamera
                    .transform
                    .forward,
                    ropeDirection
                );
        }

        forward.y = 0f;

        if (
            forward.sqrMagnitude >
            0.01f
        )
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

    private void LimitUpwardVelocity()
    {
        Vector3 velocity =
            rb.linearVelocity;

        if (
            velocity.y >
            maxUpwardSwingSpeed
        )
        {
            velocity.y =
                maxUpwardSwingSpeed;

            rb.linearVelocity =
                velocity;
        }
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

        if (IsPulling)
        {
            ropeLine.enabled = true;
            ropeLine.startWidth = activeWidth;
            ropeLine.endWidth = activeWidth;
            ropeLine.SetPosition(0, GetRopeStartPosition());
            ropeLine.SetPosition(1, pullPoint);
            return;
        }

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
