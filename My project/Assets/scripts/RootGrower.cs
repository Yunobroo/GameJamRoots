using UnityEngine;
using UnityEngine.InputSystem;

public class RootGrower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject rootPrefab;

    [Header("Placement")]
    [SerializeField] private float maxGrowDistance = 20f;

    [Tooltip("Only surfaces on these layers can grow roots.")]
    [SerializeField] private LayerMask growableLayer;

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

    private PlayerInput playerInput;
    private InputAction growRootAction;

    private ProceduralRoot currentRoot;

    private Vector3 currentGrowPosition;
    private Vector3 currentDirection;

    private float totalRootLength;
    private float growthProgress;
    private float curveSeed;

    private bool isGrowing;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        growRootAction =
            playerInput.actions["GrowRoot"];
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

        GameObject rootObject = Instantiate(
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

        currentGrowPosition = hit.point;
        currentDirection = Vector3.up;

        totalRootLength = 0f;
        growthProgress = 0f;

        curveSeed = Random.Range(0f, 100f);

        currentRoot.AddPoint(
            currentGrowPosition -
            Vector3.up * 0.1f
        );

        currentRoot.AddPoint(
            currentGrowPosition
        );

        isGrowing = true;
    }

    private void GrowRoot()
    {
        growthProgress +=
            growthSpeed * Time.deltaTime;

        while (
            growthProgress >= pointSpacing &&
            totalRootLength < maxRootLength
        )
        {
            AddRootPoint();

            growthProgress -= pointSpacing;
            totalRootLength += pointSpacing;
        }

        if (totalRootLength >= maxRootLength)
        {
            StopGrowing();
        }
    }

    private void AddRootPoint()
    {
        float curveX =
            Mathf.Sin(
                curveSeed +
                totalRootLength * curveSpeed
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
            new Vector3(
                curveX,
                1f,
                curveZ
            ).normalized;

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
}