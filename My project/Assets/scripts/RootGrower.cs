using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RootGrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject rootPrefab;

    [Header("Placement")]
    [SerializeField] private float normalGrowDistance = 20f;
    [SerializeField] private float zoomGrowDistance = 110f;

    [Tooltip("Only surfaces on these layers can grow roots.")]
    [SerializeField] private LayerMask growableLayer;

    [Header("Quick Time")]
    [Range(0.05f, 1f)]
    [SerializeField] private float quickTimeScale = 0.25f;

    [Header("Root Limit")]
    [SerializeField] private int maximumRoots = 5;

    [Header("Growth")]
    [SerializeField] private float growthSpeed = 3f;
    [SerializeField] private float maxRootLength = 8f;
    [SerializeField] private float pointSpacing = 0.15f;

    [Header("Curve")]
    [SerializeField] private float curveStrength = 0.25f;
    [SerializeField] private float curveSpeed = 1.5f;

    [Header("Thickness")]
    [SerializeField] private float baseThickness = 0.4f;
    [SerializeField] private float tipThickness = 0.08f;

    [Header("Movable Object Interaction")]
    [Tooltip("Radius around a new root point that can push movable objects.")]
    [SerializeField] private float movableInteractionRadius = 0.35f;
    [SerializeField] private float movablePushForce = 2.5f;
    [SerializeField] private LayerMask movableObjectLayer = ~0;

    [Header("Retraction")]
    [SerializeField] private float retractSpeed = 12f;

    private PlayerInput playerInput;

    private InputAction growRootAction;
    private InputAction zoomModeAction;
    private PlayerCamera playerCameraController;

    private ProceduralRoot currentRoot;

    private Vector3 currentGrowPosition;
    private Vector3 currentDirection;

    private Vector3 surfaceNormal;
    private Vector3 surfaceRight;
    private Vector3 surfaceForward;

    private float totalRootLength;
    private float growthProgress;
    private float curveSeed;

    private bool isGrowing;
    private bool zoomActive;
    private float normalFixedDeltaTime;

    private readonly Queue<ProceduralRoot> placedRoots =
        new Queue<ProceduralRoot>();

    private void Awake()
    {
        playerInput =
            GetComponent<PlayerInput>();

        growRootAction =
            playerInput.actions["GrowRoot"];

        zoomModeAction =
            playerInput.actions["QuickGrowMode"];

        playerCameraController =
            playerCamera.GetComponentInParent<PlayerCamera>();

        normalFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        UpdateZoomMode();

        if (growRootAction.WasPressedThisFrame())
        {
            StartGrowing();
        }

        if (growRootAction.WasReleasedThisFrame())
        {
            StopGrowing();
        }

        if (isGrowing)
        {
            GrowRoot();
        }
    }

    private void UpdateZoomMode()
    {
        bool wantsZoom =
            zoomModeAction.IsPressed();

        if (wantsZoom == zoomActive)
            return;

        zoomActive = wantsZoom;

        Time.timeScale =
            zoomActive
                ? quickTimeScale
                : 1f;

        Time.fixedDeltaTime =
            zoomActive
                ? normalFixedDeltaTime * quickTimeScale
                : normalFixedDeltaTime;

        if (playerCameraController != null)
        {
            playerCameraController.SetZoomActive(
                zoomActive
            );
        }
    }

    public bool TryGetGrowablePoint(
        Ray ray,
        float radius,
        float distance,
        out RaycastHit hit
    )
    {
        return Physics.SphereCast(
            ray,
            radius,
            out hit,
            distance,
            growableLayer,
            QueryTriggerInteraction.Ignore
        );
    }

#if false
    public void UnlockVineBoostPad()
    {
        vineBoostPadUnlocked = true;
    }

    private void TryPlaceVineBoostPad()
    {
        float placementDistance =
            zoomActive
                ? zoomGrowDistance
                : normalGrowDistance;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!TryGetGrowablePoint(
            ray,
            0.05f,
            placementDistance,
            out RaycastHit hit
        ))
        {
            return;
        }

        CreateVineBoostPad(hit.point, hit.normal);
    }

    private void CreateVineBoostPad(
        Vector3 position,
        Vector3 surfaceNormal
    )
    {
        MakeRoomForNewRoot();

        Vector3 vineDirection = Vector3.ProjectOnPlane(
            playerCamera.transform.forward,
            surfaceNormal
        ).normalized;

        if (vineDirection.sqrMagnitude < 0.01f)
        {
            vineDirection = Vector3.ProjectOnPlane(
                playerCamera.transform.up,
                surfaceNormal
            ).normalized;
        }

        Vector3 sideDirection = Vector3.Cross(
            surfaceNormal,
            vineDirection
        ).normalized;

        GameObject rootObject = Instantiate(
            rootPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        ProceduralRoot padRoot =
            rootObject.GetComponent<ProceduralRoot>();

        padRoot.SetThickness(
            boostPadThickness,
            boostPadThickness
        );

        const int vineCount = 5;

        for (int vine = 0; vine < vineCount; vine++)
        {
            float sideOffset = Mathf.Lerp(
                -boostPadWidth * 0.5f,
                boostPadWidth * 0.5f,
                (float)vine / (vineCount - 1)
            );

            Vector3 vineStart =
                position + sideDirection * sideOffset;

            Vector3 vineEnd =
                vineStart + vineDirection * boostPadLength;

            bool forward = vine % 2 == 0;

            AddBoostPadSegment(
                padRoot,
                forward ? vineStart : vineEnd,
                forward ? vineEnd : vineStart
            );

            if (vine < vineCount - 1)
            {
                float nextOffset = Mathf.Lerp(
                    -boostPadWidth * 0.5f,
                    boostPadWidth * 0.5f,
                    (float)(vine + 1) / (vineCount - 1)
                );

                Vector3 connectorEnd =
                    position + sideDirection * nextOffset +
                    (forward ? vineDirection * boostPadLength : Vector3.zero);

                AddBoostPadSegment(
                    padRoot,
                    forward ? vineEnd : vineStart,
                    connectorEnd
                );
            }
        }

        placedRoots.Enqueue(padRoot);

        GameObject triggerObject =
            new GameObject("Vine Boost Pad Trigger");

        triggerObject.transform.SetParent(
            rootObject.transform,
            true
        );

        triggerObject.transform.position =
            position +
            vineDirection * (boostPadLength * 0.5f) +
            surfaceNormal * 0.25f;

        triggerObject.transform.rotation =
            Quaternion.LookRotation(
                vineDirection,
                surfaceNormal
            );

        VineBoostPad boostPad =
            triggerObject.AddComponent<VineBoostPad>();

        Vector3 launchDirection =
            (vineDirection +
             surfaceNormal * boostPadLift).normalized;

        boostPad.Configure(
            launchDirection,
            boostPadLaunchSpeed,
            new Vector3(
                boostPadWidth,
                0.5f,
                boostPadLength
            )
        );
    }

    private void AddBoostPadSegment(
        ProceduralRoot root,
        Vector3 start,
        Vector3 end
    )
    {
        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(
                Vector3.Distance(start, end) /
                Mathf.Max(0.05f, boostPadPointSpacing)
            )
        );

        if (root.PointCount == 0)
        {
            root.AddPoint(start);
        }

        for (int i = 1; i <= steps; i++)
        {
            root.AddPoint(
                Vector3.Lerp(
                    start,
                    end,
                    (float)i / steps
                )
            );
        }
    }

#endif

    private void StartGrowing()
    {
        if (isGrowing)
            return;

        float currentGrowDistance =
            zoomActive
                ? zoomGrowDistance
                : normalGrowDistance;

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            currentGrowDistance,
            growableLayer
        ))
        {
            return;
        }

        MakeRoomForNewRoot();

        GameObject rootObject =
            Instantiate(
                rootPrefab,
                Vector3.zero,
                Quaternion.identity
            );

        currentRoot =
            rootObject.GetComponent<ProceduralRoot>();

        currentRoot.SetThickness(
            baseThickness,
            tipThickness
        );

        currentGrowPosition =
            hit.point;

        surfaceNormal =
            hit.normal.normalized;

        BuildSurfaceBasis();

        currentDirection =
            surfaceNormal;

        totalRootLength = 0f;
        growthProgress = 0f;

        curveSeed =
            Random.Range(
                0f,
                100f
            );

        currentRoot.AddPoint(
            currentGrowPosition -
            surfaceNormal * 0.15f
        );

        currentRoot.AddPoint(
            currentGrowPosition
        );

        placedRoots.Enqueue(
            currentRoot
        );

        isGrowing = true;
    }

    private void BuildSurfaceBasis()
    {
        Vector3 reference =
            Mathf.Abs(
                Vector3.Dot(
                    surfaceNormal,
                    Vector3.up
                )
            ) > 0.9f
                ? Vector3.forward
                : Vector3.up;

        surfaceRight =
            Vector3.Cross(
                reference,
                surfaceNormal
            ).normalized;

        surfaceForward =
            Vector3.Cross(
                surfaceNormal,
                surfaceRight
            ).normalized;
    }

    private void GrowRoot()
    {
        growthProgress +=
            growthSpeed *
            Time.unscaledDeltaTime;

        while (
            growthProgress >= pointSpacing &&
            totalRootLength < maxRootLength
        )
        {
            AddRootPoint();

            growthProgress -=
                pointSpacing;

            totalRootLength +=
                pointSpacing;
        }

        if (
            totalRootLength >=
            maxRootLength
        )
        {
            StopGrowing();
        }
    }

    private void AddRootPoint()
    {
        float curveX =
            Mathf.Sin(
                curveSeed +
                totalRootLength *
                curveSpeed
            ) *
            curveStrength;

        float curveZ =
            Mathf.Cos(
                curveSeed +
                totalRootLength *
                curveSpeed *
                0.8f
            ) *
            curveStrength;

        Vector3 targetDirection =
            surfaceNormal +
            surfaceRight * curveX +
            surfaceForward * curveZ;

        targetDirection.Normalize();

        currentDirection =
            Vector3.Slerp(
                currentDirection,
                targetDirection,
                0.15f
            ).normalized;

        currentGrowPosition +=
            currentDirection *
            pointSpacing;

        currentRoot.AddPoint(
            currentGrowPosition
        );

        PushMovableObjects();
    }

    private void PushMovableObjects()
    {
        Collider[] nearbyColliders =
            Physics.OverlapSphere(
                currentGrowPosition,
                movableInteractionRadius,
                movableObjectLayer,
                QueryTriggerInteraction.Ignore
            );

        HashSet<MovableObjects> pushedObjects =
            new HashSet<MovableObjects>();

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            MovableObjects movableObject =
                nearbyCollider.GetComponentInParent<MovableObjects>();

            if (
                movableObject == null ||
                !pushedObjects.Add(movableObject)
            )
            {
                continue;
            }

            movableObject.PushFromRoot(
                currentRoot,
                currentGrowPosition,
                currentDirection,
                movablePushForce
            );
        }
    }

    private void StopGrowing()
    {
        if (!isGrowing)
            return;

        isGrowing = false;
        currentRoot = null;
    }

    private void MakeRoomForNewRoot()
    {
        while (
            placedRoots.Count >=
            maximumRoots
        )
        {
            ProceduralRoot oldestRoot =
                placedRoots.Dequeue();

            if (oldestRoot != null)
            {
                oldestRoot.BeginRetraction(
                    retractSpeed
                );
            }
        }
    }

    private void OnDisable()
    {
        zoomActive = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = normalFixedDeltaTime;

        if (playerCameraController != null)
        {
            playerCameraController.SetZoomActive(false);
        }
    }
}
