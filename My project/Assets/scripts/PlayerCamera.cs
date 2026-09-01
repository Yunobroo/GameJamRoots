using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform cameraTransform;

    [Header("Camera Settings")]
    [SerializeField] private float sensitivity = 0.15f;

    [SerializeField] private float distance = 7.5f;

    [SerializeField] private float heightOffset = 0.5f;

    [Header("Pitch")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    private Vector2 lookInput;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    private void LateUpdate()
    {
        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        Vector3 targetPosition =
            cameraTarget.position +
            Vector3.up * heightOffset;

        Vector3 cameraPosition =
            targetPosition -
            rotation *
            Vector3.forward *
            distance;

        cameraTransform.position =
            cameraPosition;

        cameraTransform.rotation =
            rotation;
    }
}