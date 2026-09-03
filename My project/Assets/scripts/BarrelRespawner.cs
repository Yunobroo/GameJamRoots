using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BarrelRespawner : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Extra acceleration applied in the barrel's current movement direction.")]
    [SerializeField] private float acceleration = 35f;

    [Tooltip("Maximum speed of the barrel.")]
    [SerializeField] private float maxSpeed = 30f;

    [Header("Root Detection")]
    [SerializeField] private LayerMask rootLayer;

    [Header("Stuck Respawn")]
    [SerializeField] private float stuckSpeedThreshold = 0.75f;

    [SerializeField] private float stuckTimeBeforeRespawn = 2f;

    [Header("Lava Respawn")]
    [SerializeField] private float lavaRespawnDelay = 0.5f;

    [Header("Player")]
    [Min(0f)]
    [SerializeField] private float playerKillDelay = 0f;

    private Rigidbody rb;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private bool touchingRoot;
    private bool respawning;
    private bool playerKilled;

    private float stuckTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void FixedUpdate()
    {
        if (respawning)
            return;

        AccelerateBarrel();
        CheckIfStuck();
    }

    private void AccelerateBarrel()
    {
        Vector3 velocity = rb.linearVelocity;

        // Only boost the barrel once it has started moving.
        if (velocity.sqrMagnitude > 0.1f)
        {
            rb.AddForce(
                velocity.normalized * acceleration,
                ForceMode.Acceleration
            );
        }

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void CheckIfStuck()
    {
        if (!touchingRoot)
        {
            stuckTimer = 0f;
            return;
        }

        if (rb.linearVelocity.magnitude <= stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;

            if (stuckTimer >= stuckTimeBeforeRespawn)
            {
                StartCoroutine(
                    RespawnAfterDelay(0f)
                );
            }
        }
        else
        {
            stuckTimer = 0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckObject(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckObject(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsRoot(collision.collider))
        {
            touchingRoot = false;
            stuckTimer = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckObject(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckObject(other);
    }

    private void CheckObject(Collider other)
    {
        if (respawning || playerKilled)
            return;

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player != null)
        {
            KillPlayer();
            return;
        }

        if (IsRoot(other))
        {
            touchingRoot = true;
        }

        if (IsLava(other))
        {
            StartCoroutine(
                RespawnAfterDelay(lavaRespawnDelay)
            );
        }
    }

    private void KillPlayer()
    {
        if (playerKilled)
            return;

        playerKilled = true;

        if (playerKillDelay <= 0f)
        {
            RestartLevel();
        }
        else
        {
            Invoke(
                nameof(RestartLevel),
                playerKillDelay
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

    private bool IsRoot(Collider other)
    {
        return
            (rootLayer.value &
            (1 << other.gameObject.layer)) != 0;
    }

    private bool IsLava(Collider other)
    {
        return
            other.GetComponentInParent<KillPlayer>() != null;
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        if (respawning)
            yield break;

        respawning = true;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Respawn();
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = spawnPosition;
        rb.rotation = spawnRotation;

        touchingRoot = false;
        stuckTimer = 0f;

        Physics.SyncTransforms();

        respawning = false;
    }
}