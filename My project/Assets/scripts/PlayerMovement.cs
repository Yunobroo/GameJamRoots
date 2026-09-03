using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform cameraTransform;

    [Header("Player Model Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run_N";
    [SerializeField] private string jumpStartStateName = "JumpStart_N";
    [SerializeField] private float animationFadeTime = 0.15f;

    [Tooltip("Stops animations from rotating/moving the entire visual model.")]
    [SerializeField] private bool lockAnimatedModelTransform = true;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Swing Movement")]
    [SerializeField] private float swingAirControl = 8f;

    [Header("Developer Flight")]
    [SerializeField] private bool enableDeveloperFlight = true;
    [SerializeField] private float jumpDoubleTapWindow = 0.3f;
    [SerializeField] private float flightSpeed = 12f;
    [SerializeField] private float flightVerticalSpeed = 9f;

    [Header("Welcome Tutorial")]
    [SerializeField] private bool showWelcomeTutorial = true;
    [SerializeField] private float welcomeDuration = 12f;
    [SerializeField] private string welcomeTitle = "Welcome to Roots";

    private Rigidbody rb;
    private RootSwing rootSwing;

    private Vector2 moveInput;
    private bool jumpHeld;
    private bool developerFlightActive;
    private float lastJumpPressTime = -999f;

    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private bool welcomeVisible;
    private float welcomeHideTime;

    private string currentAnimationState;

    private Transform animatedModelTransform;
    private Vector3 animatedModelLocalPosition;
    private Quaternion animatedModelLocalRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rootSwing = GetComponent<RootSwing>();

        respawnPosition = transform.position;
        respawnRotation = transform.rotation;

        welcomeVisible = showWelcomeTutorial;

        welcomeHideTime =
            Time.unscaledTime + welcomeDuration;

        if (playerAnimator != null)
        {
            animatedModelTransform =
                playerAnimator.transform;

            animatedModelLocalPosition =
                animatedModelTransform.localPosition;

            animatedModelLocalRotation =
                animatedModelTransform.localRotation;
        }
    }

    private void Start()
    {
        PlayAnimation(idleStateName);
    }

    private void Update()
    {
        UpdateAnimations();

        if (!welcomeVisible)
            return;

        bool durationExpired =
            welcomeDuration > 0f &&
            Time.unscaledTime >= welcomeHideTime;

        bool enterPressed =
            Keyboard.current != null &&
            Keyboard.current.enterKey.wasPressedThisFrame;

        if (durationExpired || enterPressed)
        {
            welcomeVisible = false;
        }
    }

    private void LateUpdate()
    {
        if (
            !lockAnimatedModelTransform ||
            animatedModelTransform == null
        )
        {
            return;
        }

        animatedModelTransform.localPosition =
            animatedModelLocalPosition;

        animatedModelTransform.localRotation =
            animatedModelLocalRotation;
    }

    private void UpdateAnimations()
    {
        if (playerAnimator == null)
            return;

        if (developerFlightActive)
        {
            PlayMovementAnimation();
            return;
        }

        bool grounded =
            IsGrounded();

        /*
         * Only treat the player as airborne when the
         * ground check actually says we're airborne.
         *
         * We DON'T use vertical velocity here because
         * walking uphill can create positive Y velocity.
         */
        if (!grounded)
        {
            PlayAnimation(
                jumpStartStateName
            );

            return;
        }

        PlayMovementAnimation();
    }

    private void PlayMovementAnimation()
    {
        bool moving =
            moveInput.sqrMagnitude > 0.01f;

        if (moving)
        {
            PlayAnimation(
                runStateName
            );
        }
        else
        {
            PlayAnimation(
                idleStateName
            );
        }
    }

    private void PlayAnimation(
        string stateName
    )
    {
        if (playerAnimator == null)
            return;

        if (currentAnimationState == stateName)
            return;

        currentAnimationState =
            stateName;

        playerAnimator.CrossFade(
            stateName,
            animationFadeTime
        );
    }

    private void OnGUI()
    {
        if (!welcomeVisible)
            return;

        float panelWidth =
            Mathf.Min(
                720f,
                Screen.width - 40f
            );

        float panelHeight =
            Mathf.Min(
                470f,
                Screen.height - 40f
            );

        Rect panel =
            new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight
            );

        GUI.Box(
            panel,
            GUIContent.none
        );

        GUIStyle titleStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                alignment =
                    TextAnchor.MiddleCenter,

                fontSize = 32,

                fontStyle =
                    FontStyle.Bold
            };

        GUIStyle controlsStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                alignment =
                    TextAnchor.UpperLeft,

                fontSize = 20,

                wordWrap = true,

                padding =
                    new RectOffset(
                        25,
                        25,
                        10,
                        10
                    )
            };

        GUIStyle footerStyle =
            new GUIStyle(
                GUI.skin.label
            )
            {
                alignment =
                    TextAnchor.MiddleCenter,

                fontSize = 17,

                fontStyle =
                    FontStyle.Italic
            };

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.y + 20f,
                panel.width - 40f,
                60f
            ),
            welcomeTitle,
            titleStyle
        );

        const string controls =
            "WASD        Move\n" +
            "Mouse       Look around\n" +
            "Space       Jump / boost while swinging\n" +
            "Left Mouse  Grow a root on growable ground\n" +
            "Right Mouse Swing from a root\n" +
            "Middle Mouse  Root Pull toward growable ground (once per airtime)\n" +
            "Left Shift  Zoom, slow time, and extend root reach";

        GUI.Label(
            new Rect(
                panel.x + 45f,
                panel.y + 95f,
                panel.width - 90f,
                300f
            ),
            controls,
            controlsStyle
        );

        GUI.Label(
            new Rect(
                panel.x + 20f,
                panel.yMax - 55f,
                panel.width - 40f,
                35f
            ),
            "Press Enter to continue",
            footerStyle
        );
    }

    private void FixedUpdate()
    {
        /*
         * Prevent collisions from physically spinning
         * the Rigidbody.
         */
        rb.angularVelocity =
            Vector3.zero;

        if (developerFlightActive)
        {
            FlyPlayer();
            return;
        }

        if (
            rootSwing != null &&
            rootSwing.IsRootMovementActive
        )
        {
            SwingMovement();
        }
        else
        {
            MovePlayer();
        }
    }

    private void MovePlayer()
    {
        Vector3 cameraForward =
            cameraTransform.forward;

        Vector3 cameraRight =
            cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 movement =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        rb.linearVelocity =
            new Vector3(
                movement.x * moveSpeed,
                rb.linearVelocity.y,
                movement.z * moveSpeed
            );

        RotatePlayer(movement);
    }

    private void SwingMovement()
    {
        Vector3 cameraForward =
            cameraTransform.forward;

        Vector3 cameraRight =
            cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 movement =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        if (movement.sqrMagnitude > 0.01f)
        {
            rb.AddForce(
                movement * swingAirControl,
                ForceMode.Acceleration
            );

            RotatePlayer(movement);
        }
    }

    private void RotatePlayer(
        Vector3 movement
    )
    {
        if (
            movement.sqrMagnitude <= 0.01f
        )
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                movement,
                Vector3.up
            );

        Quaternion smoothRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed *
                Time.fixedDeltaTime
            );

        rb.MoveRotation(
            smoothRotation
        );
    }

    public void OnMove(
        InputValue value
    )
    {
        moveInput =
            value.Get<Vector2>();
    }

    public void OnJump(
        InputValue value
    )
    {
        jumpHeld =
            value.isPressed;

        if (!value.isPressed)
            return;

        if (
            enableDeveloperFlight &&
            Time.unscaledTime -
            lastJumpPressTime <=
            jumpDoubleTapWindow
        )
        {
            ToggleDeveloperFlight();

            lastJumpPressTime =
                -999f;

            return;
        }

        lastJumpPressTime =
            Time.unscaledTime;

        if (developerFlightActive)
            return;

        if (
            rootSwing != null &&
            rootSwing.IsSwinging
        )
        {
            return;
        }

        if (IsGrounded())
        {
            /*
             * Immediately start jump animation.
             * Once we actually leave the floor,
             * UpdateAnimations keeps it active.
             */
            PlayAnimation(
                jumpStartStateName
            );

            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );
    }

    private void FlyPlayer()
    {
        Vector3 movement =
            cameraTransform.forward *
            moveInput.y +
            cameraTransform.right *
            moveInput.x;

        if (movement.sqrMagnitude > 1f)
        {
            movement.Normalize();
        }

        float verticalInput =
            jumpHeld ? 1f : 0f;

        if (
            Keyboard.current != null &&
            Keyboard.current
                .leftCtrlKey
                .isPressed
        )
        {
            verticalInput -= 1f;
        }

        rb.linearVelocity =
            movement * flightSpeed +
            Vector3.up *
            (
                verticalInput *
                flightVerticalSpeed
            );

        RotatePlayer(
            Vector3.ProjectOnPlane(
                movement,
                Vector3.up
            )
        );
    }

    private void ToggleDeveloperFlight()
    {
        developerFlightActive =
            !developerFlightActive;

        rb.useGravity =
            !developerFlightActive;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }

    public bool IsGroundedNow()
    {
        return IsGrounded();
    }

    public void SetCheckpoint(
        Transform checkpoint
    )
    {
        respawnPosition =
            checkpoint.position;

        respawnRotation =
            checkpoint.rotation;
    }

    public void Respawn()
    {
        developerFlightActive =
            false;

        rb.useGravity =
            true;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;

        rb.position =
            respawnPosition;

        rb.rotation =
            respawnRotation;

        currentAnimationState =
            "";

        PlayAnimation(
            idleStateName
        );
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.useGravity = true;
        }

        developerFlightActive =
            false;
    }
}