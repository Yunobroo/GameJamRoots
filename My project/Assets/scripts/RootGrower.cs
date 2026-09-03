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
