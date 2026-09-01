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
    [SerializeField] private float quickGrowDistance = 85f;

    [Tooltip("Only surfaces on these layers can grow roots.")]
    [SerializeField] private LayerMask growableLayer;

    [Header("Quick Time Mode")]
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

    [Header("Retraction")]
    [SerializeField] private float retractSpeed = 12f;

    private PlayerInput playerInput;

    private InputAction growRootAction;
    private InputAction quickGrowModeAction;

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
    private bool quickTimeActive;

    private float normalFixedDeltaTime;

    private readonly Queue<ProceduralRoot> placedRoots =
        new Queue<ProceduralRoot>();

    private void Awake()
    {
        playerInput =
            GetComponent<PlayerInput>();

        growRootAction =
            playerInput.actions["GrowRoot"];

        quickGrowModeAction =
            playerInput.actions["QuickGrowMode"];

        normalFixedDeltaTime =
            Time.fixedDeltaTime;
    }

    private void Update()
    {
        UpdateQuickTimeMode();

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

    private void UpdateQuickTimeMode()
    {
        bool wantsQuickTime =
            quickGrowModeAction.IsPressed();

        if (
            wantsQuickTime &&
            !quickTimeActive
        )
        {
            StartQuickTime();
        }
        else if (
            !wantsQuickTime &&
            quickTimeActive
        )
        {
            StopQuickTime();
        }
    }

    private void StartQuickTime()
    {
        quickTimeActive = true;

        Time.timeScale =
            quickTimeScale;

        Time.fixedDeltaTime =
            normalFixedDeltaTime *
            quickTimeScale;
    }

    private void StopQuickTime()
    {
        quickTimeActive = false;

        Time.timeScale = 1f;

        Time.fixedDeltaTime =
            normalFixedDeltaTime;
    }

    private void StartGrowing()
    {
        if (isGrowing)
            return;

        float currentGrowDistance =
            quickTimeActive
                ? quickGrowDistance
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
        if (quickTimeActive)
        {
            Time.timeScale = 1f;

            Time.fixedDeltaTime =
                normalFixedDeltaTime;

            quickTimeActive = false;
        }
    }
}
