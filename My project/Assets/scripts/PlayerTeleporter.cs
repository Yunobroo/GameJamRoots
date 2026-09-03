using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerTeleporter : MonoBehaviour
{
    [Header("Teleport")]
    [SerializeField] private Transform destination;

    [Tooltip("Keep the player's velocity after teleporting.")]
    [SerializeField] private bool keepVelocity = false;

    private void Awake()
    {
        Collider trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null || destination == null)
            return;

        Rigidbody playerRb =
            player.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            if (!keepVelocity)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            playerRb.position = destination.position;
            playerRb.rotation = destination.rotation;
        }
        else
        {
            player.transform.SetPositionAndRotation(
                destination.position,
                destination.rotation
            );
        }

        Physics.SyncTransforms();
    }
}