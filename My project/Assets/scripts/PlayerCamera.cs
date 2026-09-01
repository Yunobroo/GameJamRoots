using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform cameraTransform;

    [Header("Camera Settings")]
    [SerializeField] private float sensitivity = 0.15f;
    [SerializeField] private float distance = 5f;

    private Vector2 lookInput;

    private float yaw;
    private float pitch;

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    private void LateUpdate()
    {
        yaw += lookInput.x * sensitivity;
        pitch -= lookInput.y * sensitivity;

        pitch = Mathf.Clamp(pitch, -30f, 70f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 cameraPosition =
            cameraTarget.position -
            rotation * Vector3.forward * distance;

        cameraTransform.position = cameraPosition;
        cameraTransform.rotation = rotation;
    }
    private void Start()
{
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}
}