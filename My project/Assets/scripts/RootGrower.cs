using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RootGrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject rootPrefab;

    [Header("Placement")]
    [SerializeField] private float maxGrowDistance = 20f;
    [SerializeField] private LayerMask growableLayer;

    [Header("Growth")]
    [SerializeField] private float growthSpeed = 3f;
    [SerializeField] private float maxRootLength = 8f;

    [Tooltip("Smaller segments create smoother curves and taper.")]
    [SerializeField] private float segmentLength = 0.2f;

    [Tooltip("Makes neighbouring segments overlap slightly to hide gaps.")]
    [SerializeField] private float segmentOverlap = 0.04f;

    [Header("Curve")]
    [SerializeField] private float curveStrength = 0.25f;
    [SerializeField] private float curveSpeed = 1.5f;

    [Header("Taper")]
    [SerializeField] private float baseThickness = 0.4f;
    [SerializeField] private float tipThickness = 0.1f;

    [Tooltip("Short roots keep a thicker tip.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float shortRootTipRatio = 0.75f;

    [Tooltip("Length needed before the root gets the full taper.")]
    [SerializeField] private float fullTaperLength = 5f;

    [Tooltip("Higher values keep the root thick longer before tapering.")]
    [Range(0.5f, 4f)]
    [SerializeField] private float taperPower = 1.7f;

    private PlayerInput playerInput;
    private InputAction growRootAction;

    private readonly List<GameObject> currentSegments =
        new List<GameObject>();

    private Vector3 currentGrowPosition;
    private Vector3 currentDirection;

    private float totalRootLength;
    private float growthProgress;
    private float curveSeed;

    private bool isGrowing;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        growRootAction = playerInput.actions["GrowRoot"];
    }

    private void Update()
    {
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

    private void StartGrowing()
    {
        if (isGrowing)
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            maxGrowDistance,
            growableLayer
        ))
        {
            return;
        }

        currentSegments.Clear();

        currentGrowPosition = hit.point;
        currentDirection = Vector3.up;

        totalRootLength = 0f;
        growthProgress = 0f;

        curveSeed = Random.Range(0f, 100f);

        isGrowing = true;
    }

    private void GrowRoot()
    {
        growthProgress += growthSpeed * Time.deltaTime;

        while (
            growthProgress >= segmentLength &&
            totalRootLength < maxRootLength
        )
        {
            CreateSegment();

            growthProgress -= segmentLength;
            totalRootLength += segmentLength;
        }

        if (totalRootLength >= maxRootLength)
        {
            StopGrowing();
        }
    }

    private void CreateSegment()
    {
        float curveX = Mathf.Sin(
            curveSeed + totalRootLength * curveSpeed
        ) * curveStrength;

        float curveZ = Mathf.Cos(
            curveSeed + totalRootLength * curveSpeed * 0.8f
        ) * curveStrength;

        Vector3 targetDirection = new Vector3(
            curveX,
            1f,
            curveZ
        ).normalized;

        // Gentler direction changes.
        currentDirection = Vector3.Slerp(
            currentDirection,
            targetDirection,
            0.15f
        ).normalized;

        Vector3 nextPosition =
            currentGrowPosition +
            currentDirection * segmentLength;

        Vector3 middlePosition =
            (currentGrowPosition + nextPosition) / 2f;

        GameObject segment = Instantiate(
            rootPrefab,
            middlePosition,
            Quaternion.identity
        );

        segment.transform.up = currentDirection;

        float visualLength =
            segmentLength + segmentOverlap;

        segment.transform.localScale = new Vector3(
            baseThickness,
            visualLength / 2f,
            baseThickness
        );

        currentSegments.Add(segment);

        currentGrowPosition = nextPosition;

        ApplySmoothTaper();
    }

    private void ApplySmoothTaper()
    {
        int segmentCount = currentSegments.Count;

        if (segmentCount == 0)
            return;

        float actualRootLength =
            segmentCount * segmentLength;

        float rootLengthFactor =
            Mathf.Clamp01(
                actualRootLength / fullTaperLength
            );

        float shortTipThickness =
            baseThickness * shortRootTipRatio;

        float actualTipThickness =
            Mathf.Lerp(
                shortTipThickness,
                tipThickness,
                rootLengthFactor
            );

        for (int i = 0; i < segmentCount; i++)
        {
            float progress;

            if (segmentCount <= 1)
            {
                progress = 0f;
            }
            else
            {
                progress =
                    (float)i /
                    (segmentCount - 1);
            }

            // Keeps the base thick and makes the
            // reduction happen gradually toward the tip.
            float curvedProgress =
                Mathf.Pow(progress, taperPower);

            // Smooth the curve again.
            curvedProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    curvedProgress
                );

            float thickness =
                Mathf.Lerp(
                    baseThickness,
                    actualTipThickness,
                    curvedProgress
                );

            Transform segment =
                currentSegments[i].transform;

            float visualLength =
                segmentLength + segmentOverlap;

            segment.localScale = new Vector3(
                thickness,
                visualLength / 2f,
                thickness
            );
        }
    }

    private void StopGrowing()
    {
        if (!isGrowing)
            return;

        isGrowing = false;

        ApplySmoothTaper();

        currentSegments.Clear();
    }
}