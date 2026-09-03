using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform cameraTransform;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 1.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Swing Movement")]
    [SerializeField] private float swingAirControl = 8f;

    private Rigidbody rb;
    private RootSwing rootSwing;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rootSwing = GetComponent<RootSwing>();
    }

    private void FixedUpdate()
    {
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
        if (movement.sqrMagnitude <= 0.01f)
            return;

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
        if (!value.isPressed)
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

    public bool IsGroundedNow()
    {
        return IsGrounded();
    }

}
