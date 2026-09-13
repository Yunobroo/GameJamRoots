using UnityEngine;

[DisallowMultipleComponent]
public class AutomaticSlidingDoor : MonoBehaviour
{
    [Header("Door panels (include their colliders)")]
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;
    [Tooltip("Optional. Otherwise uses the PlayerMovement in the scene.")]
    [SerializeField] private Transform player;

    [Header("Opening")]
    [Tooltip("Offsets use each panel parent's local axes. Reverse X if the panels move inward.")]
    [SerializeField] private Vector3 leftOpenOffset = new Vector3(-1.5f, 0f, 0f);
    [SerializeField] private Vector3 rightOpenOffset = new Vector3(1.5f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float openDistance = 4f;
    [SerializeField, Min(0.1f)] private float closeDistance = 5f;
    [SerializeField, Min(0.01f)] private float slideDuration = 0.6f;
    [SerializeField, Min(0f)] private float closeDelay = 1f;
    [Tooltip("Optional fixed detection point. Defaults to the closed panels' renderer bounds center.")]
    [SerializeField] private Transform detectionCenter;

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private Vector3 localCenter;
    private float progress;
    private float lastNearbyTime;
    private bool open;

    private void Start()
    {
        if (leftPanel == null || rightPanel == null || leftPanel == rightPanel
            || leftPanel == transform || rightPanel == transform
            || leftPanel.IsChildOf(rightPanel) || rightPanel.IsChildOf(leftPanel))
        {
            Debug.LogError("Assign two separate door panel roots to AutomaticSlidingDoor.", this);
            enabled = false;
            return;
        }

        leftClosed = leftPanel.localPosition;
        rightClosed = rightPanel.localPosition;
        localCenter = transform.InverseTransformPoint(
            (PanelCenter(leftPanel) + PanelCenter(rightPanel)) * 0.5f);

        if (player == null)
        {
            PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null)
                player = movement.transform;
        }

        if (player == null)
            Debug.LogWarning("AutomaticSlidingDoor needs a Player reference to open.", this);
    }

    private static Vector3 PanelCenter(Transform panel)
    {
        Renderer[] renderers = panel.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return panel.position;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds.center;
    }

    private void Update()
    {
        if (player == null || leftPanel == null || rightPanel == null)
            return;

        Vector3 center = detectionCenter != null
            ? detectionCenter.position : transform.TransformPoint(localCenter);
        float distance = Vector3.Distance(player.position, center);
        float threshold = open ? Mathf.Max(openDistance, closeDistance) : openDistance;
        if (distance <= threshold)
        {
            open = true;
            lastNearbyTime = Time.time;
        }
        else if (Time.time - lastNearbyTime >= closeDelay)
        {
            open = false;
        }

        progress = Mathf.MoveTowards(progress, open ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.01f, slideDuration));
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        leftPanel.localPosition = leftClosed + leftOpenOffset * eased;
        rightPanel.localPosition = rightClosed + rightOpenOffset * eased;
    }
}
