using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform cameraTransform;

    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float controllerSensitivity = 140f;
    [Tooltip("Lower values feel more responsive; higher values feel smoother.")]
    [SerializeField] private float lookSmoothTime = 0.045f;

    [Header("Camera Settings")]
    [SerializeField] private float distance = 7.5f;
    [SerializeField] private float heightOffset = 0.5f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask cameraCollisionLayers = ~0;
    [SerializeField] private float cameraCollisionRadius = 0.25f;
    [SerializeField] private float cameraCollisionPadding = 0.1f;
    [SerializeField] private float minimumCameraDistance = 0.3f;
    [SerializeField] private float cameraReturnSpeed = 15f;

    [Header("Pitch")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Speed FOV")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float maxFOV = 78f;

    [Tooltip("Player speed required to reach Max FOV.")]
    [SerializeField] private float speedForMaxFOV = 20f;

    [Tooltip("How quickly the normal speed FOV changes.")]
    [SerializeField] private float fovSmoothSpeed = 5f;

    [Header("Zoom")]
    [Tooltip("Lower FOV values create a stronger optical zoom.")]
    [SerializeField] private float zoomFOV = 25f;
    [SerializeField] private float zoomSpeed = 14f;

    [Header("Swing Release FOV Kick")]
    [Tooltip("Extra FOV added when releasing a swing at high speed.")]
    [SerializeField] private float releaseFOVKick = 8f;

    [Tooltip("Minimum speed required for the release kick.")]
    [SerializeField] private float releaseKickMinSpeed = 8f;

    [Tooltip("Speed at which the full release kick is applied.")]
    [SerializeField] private float releaseKickFullSpeed = 20f;

    [Tooltip("How quickly the release kick fades away.")]
    [SerializeField] private float releaseKickRecoverySpeed = 5f;

    private Vector2 lookInput;

    private float yaw;
    private float pitch;
    private float targetYaw;
    private float targetPitch;
    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;

    private float currentReleaseKick;

    private Camera playerCamera;
    private Rigidbody playerRigidbody;
    private RootSwing rootSwing;

    private bool wasSwinging;
    private bool zoomActive;

    public void SetZoomActive(bool active)
    {
        zoomActive = active;
    }

    private void Awake()
    {
        playerCamera =
            cameraTransform.GetComponent<Camera>();

        playerRigidbody =
            GetComponent<Rigidbody>();

        rootSwing =
            GetComponent<RootSwing>();
    }

    private void Start()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;

        Vector3 startingAngles =
            cameraTransform.eulerAngles;

        yaw = startingAngles.y;
        pitch = NormalizeAngle(startingAngles.x);
        targetYaw = yaw;
        targetPitch = pitch;

        if (playerCamera != null)
        {
            playerCamera.fieldOfView =
                normalFOV;
        }

        if (rootSwing != null)
        {
            wasSwinging =
                rootSwing.IsSwinging;
        }
    }

    public void OnLook(InputValue value)
    {
        lookInput =
            value.Get<Vector2>();
    }

    private void LateUpdate()
    {
        UpdateCameraRotation();
        UpdateCameraPosition();
        CheckSwingRelease();
        UpdateSpeedFOV();
    }

    private void UpdateCameraRotation()
    {
        bool controllerLooking =
            Gamepad.current != null &&
            Gamepad.current.rightStick
                .ReadValue()
                .sqrMagnitude > 0.001f;

        if (controllerLooking)
        {
            Vector2 controllerInput =
                Gamepad.current.rightStick
                    .ReadValue();

            targetYaw +=
                controllerInput.x *
                controllerSensitivity *
                Time.unscaledDeltaTime;

            targetPitch -=
                controllerInput.y *
                controllerSensitivity *
                Time.unscaledDeltaTime;
        }
        else
        {
            targetYaw +=
                lookInput.x *
                mouseSensitivity;

            targetPitch -=
                lookInput.y *
                mouseSensitivity;
        }

        targetPitch =
            Mathf.Clamp(
                targetPitch,
                minPitch,
                maxPitch
            );

        yaw = Mathf.SmoothDampAngle(
            yaw,
            targetYaw,
            ref yawSmoothVelocity,
            lookSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        pitch = Mathf.SmoothDampAngle(
            pitch,
            targetPitch,
            ref pitchSmoothVelocity,
            lookSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        cameraTransform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );
    }

    private float NormalizeAngle(float angle)
    {
        return angle > 180f
            ? angle - 360f
            : angle;
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition =
            cameraTarget.position +
            Vector3.up *
            heightOffset;

        Vector3 desiredCameraPosition =
            targetPosition -
            cameraTransform.rotation *
            Vector3.forward *
            distance;

        Vector3 cameraDirection =
            desiredCameraPosition - targetPosition;

        float desiredDistance =
            cameraDirection.magnitude;

        if (desiredDistance <= Mathf.Epsilon)
        {
            cameraTransform.position = targetPosition;
            return;
        }

        cameraDirection /= desiredDistance;

        RaycastHit[] hits = Physics.SphereCastAll(
            targetPosition,
            cameraCollisionRadius,
            cameraDirection,
            desiredDistance,
            cameraCollisionLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = desiredDistance;
        bool obstructionFound = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            obstructionFound = true;
            closestDistance = Mathf.Min(
                closestDistance,
                hit.distance
            );
        }

        Vector3 cameraPosition;

        if (obstructionFound)
        {
            float safeDistance = Mathf.Clamp(
                closestDistance - cameraCollisionPadding,
                minimumCameraDistance,
                desiredDistance
            );

            cameraPosition =
                targetPosition +
                cameraDirection * safeDistance;
        }
        else
        {
            cameraPosition = Vector3.Lerp(
                cameraTransform.position,
                desiredCameraPosition,
                cameraReturnSpeed * Time.unscaledDeltaTime
            );
        }

        cameraTransform.position =
            cameraPosition;
    }

    private void CheckSwingRelease()
    {
        if (
            rootSwing == null ||
            playerRigidbody == null
        )
        {
            return;
        }

        bool currentlySwinging =
            rootSwing.IsSwinging;

        /*
         * Previous frame:
         * swinging
         *
         * Current frame:
         * NOT swinging
         *
         * = player just released the vine.
         */
        if (
            wasSwinging &&
            !currentlySwinging
        )
        {
            ApplyReleaseFOVKick();
        }

        wasSwinging =
            currentlySwinging;
    }

    private void ApplyReleaseFOVKick()
    {
        float speed =
            playerRigidbody
                .linearVelocity
                .magnitude;

        if (
            speed <
            releaseKickMinSpeed
        )
        {
            return;
        }

        float speedPercentage =
            Mathf.InverseLerp(
                releaseKickMinSpeed,
                releaseKickFullSpeed,
                speed
            );

        currentReleaseKick =
            releaseFOVKick *
            speedPercentage;
    }

    private void UpdateSpeedFOV()
    {
        if (
            playerCamera == null ||
            playerRigidbody == null
        )
        {
            return;
        }

        if (zoomActive)
        {
            playerCamera.fieldOfView =
                Mathf.Lerp(
                    playerCamera.fieldOfView,
                    zoomFOV,
                    zoomSpeed * Time.unscaledDeltaTime
                );

            return;
        }

        float speed =
            playerRigidbody
                .linearVelocity
                .magnitude;

        float speedPercentage =
            Mathf.Clamp01(
                speed /
                speedForMaxFOV
            );

        float speedFOV =
            Mathf.Lerp(
                normalFOV,
                maxFOV,
                speedPercentage
            );

        /*
         * Release kick fades independently
         * from the regular speed FOV.
         */
        currentReleaseKick =
            Mathf.MoveTowards(
                currentReleaseKick,
                0f,
                releaseKickRecoverySpeed *
                Time.unscaledDeltaTime
            );

        float targetFOV =
            speedFOV +
            currentReleaseKick;

        /*
         * Prevent absurd FOV values if
         * we're moving extremely fast.
         */
        targetFOV =
            Mathf.Min(
                targetFOV,
                maxFOV +
                releaseFOVKick
            );

        playerCamera.fieldOfView =
            Mathf.Lerp(
                playerCamera.fieldOfView,
                targetFOV,
                fovSmoothSpeed *
                Time.unscaledDeltaTime
            );
    }
}
