using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class KillPlayer : MonoBehaviour
{
    [Tooltip("Optional delay before restarting the level.")]
    [Min(0f)]
    [SerializeField] private float restartDelay;

    [Header("Root Burning")]
    [SerializeField] private float rootBurnSpeed = 20f;

    private bool playerKilled;

    private void Awake()
    {
        Rigidbody hazardBody = GetComponent<Rigidbody>();
        hazardBody.isKinematic = true;
        hazardBody.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryKillPlayer(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKillPlayer(collision.collider);
    }

    private void TryKillPlayer(Collider other)
    {
        ProceduralRoot root =
            other.GetComponentInParent<ProceduralRoot>();

        if (root != null)
        {
            root.BeginRetraction(rootBurnSpeed);
            return;
        }

        if (playerKilled)
            return;

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        playerKilled = true;

        if (restartDelay <= 0f)
        {
            RestartLevel();
        }
        else
        {
            Invoke(
                nameof(RestartLevel),
                restartDelay
            );
        }
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;

        Scene activeScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            activeScene.buildIndex
        );
    }
}
