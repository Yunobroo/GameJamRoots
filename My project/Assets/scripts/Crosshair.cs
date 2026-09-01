using UnityEngine;
using UnityEngine.UI;

public class SwingCrosshair : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RootSwing rootSwing;
    [SerializeField] private Image crosshairImage;

    [Header("Crosshair")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color targetColor = Color.green;

    [SerializeField] private float normalSize = 18f;
    [SerializeField] private float targetSize = 26f;

    [SerializeField] private float smoothSpeed = 10f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (rootSwing == null)
            return;

        bool hasTarget =
            rootSwing.HasSwingTarget;

        Color desiredColor =
            hasTarget
            ? targetColor
            : normalColor;

        float desiredSize =
            hasTarget
            ? targetSize
            : normalSize;

        crosshairImage.color =
            Color.Lerp(
                crosshairImage.color,
                desiredColor,
                smoothSpeed * Time.deltaTime
            );

        Vector2 desiredScale =
            new Vector2(
                desiredSize,
                desiredSize
            );

        rectTransform.sizeDelta =
            Vector2.Lerp(
                rectTransform.sizeDelta,
                desiredScale,
                smoothSpeed * Time.deltaTime
            );
    }
}