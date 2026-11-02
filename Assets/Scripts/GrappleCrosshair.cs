using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the on-screen crosshair and provides dynamic visual feedback
/// when a valid grapple target is in range.
/// </summary>
public class GrappleCrosshair : MonoBehaviour
{
    [Header("Crosshair Graphic")]
    [Tooltip("UI Image component displaying the crosshair")]
    public Image crosshairImage;
    [Tooltip("Sprite to use for crosshair (default: Crosshair_SquareDot_Target)")]
    public Sprite crosshairSprite;
    [Tooltip("Pixel size of the crosshair on screen (e.g. 32x32, 48x48)")]
    public Vector2 crosshairSize = new Vector2(36f, 36f);

    [Header("Dynamic Feedback Colors")]
    [Tooltip("Color when aiming at nothing or out of range")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.75f);
    [Tooltip("Color when aiming at a valid grapple target within reach (Easy Aim!)")]
    public Color targetReadyColor = new Color(0.1f, 1f, 0.55f, 1f); // Neon Green / Cyan
    [Tooltip("Color while actively grappling")]
    public Color grapplingColor = new Color(1f, 0.85f, 0.2f, 1f);   // Electric Gold

    [Header("Scale Animation")]
    [Tooltip("Scale multiplier when target is locked in range")]
    public float targetScaleMultiplier = 1.15f;
    [Tooltip("Animation speed for color and scale transitions")]
    public float transitionSpeed = 12f;

    [Header("References")]
    [Tooltip("Reference to the GrapplingGun script (auto-detected if empty)")]
    public GrapplingGun grapplingGun;

    private RectTransform rectTransform;
    private Vector3 initialScale;

    private void Awake()
    {
        EnsureCrosshairUI();

        if (grapplingGun == null)
        {
            grapplingGun = FindAnyObjectByType<GrapplingGun>();
        }
    }

    private void Start()
    {
        EnsureCrosshairUI();
    }

    private void Update()
    {
        if (grapplingGun == null)
        {
            grapplingGun = FindAnyObjectByType<GrapplingGun>();
            if (grapplingGun == null) return;
        }

        if (crosshairImage == null) return;

        Color targetColor = normalColor;
        float targetScale = 1f;

        if (grapplingGun.IsGrappling())
        {
            targetColor = grapplingColor;
            targetScale = targetScaleMultiplier * 1.1f;
        }
        else if (grapplingGun.CanGrapple())
        {
            targetColor = targetReadyColor;
            targetScale = targetScaleMultiplier;
        }

        // Smooth transition
        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * transitionSpeed);
        if (rectTransform != null)
        {
            Vector3 desiredScale = initialScale * targetScale;
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, desiredScale, Time.deltaTime * transitionSpeed);
        }
    }

    /// <summary>
    /// Automatically ensures the Canvas, CanvasScaler, and Image exist and are configured.
    /// </summary>
    public void EnsureCrosshairUI()
    {
        if (crosshairSprite == null)
        {
            // Load sliced target sprite
            var assets = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in assets)
            {
                if (s.name == "Crosshair_SquareDot_Target")
                {
                    crosshairSprite = s;
                    break;
                }
            }
        }

        if (crosshairImage == null)
        {
            // Find or create Canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = GameObject.Find("HUD_Canvas");
                if (canvasObj == null)
                {
                    canvasObj = new GameObject("HUD_Canvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;

                    var scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;

                    canvasObj.AddComponent<GraphicRaycaster>();
                }
                else
                {
                    canvas = canvasObj.GetComponent<Canvas>();
                }
            }

            // Find or create Crosshair Image
            Transform crosshairTransform = canvas.transform.Find("GrappleCrosshair");
            if (crosshairTransform == null)
            {
                GameObject imgObj = new GameObject("GrappleCrosshair");
                imgObj.transform.SetParent(canvas.transform, false);
                crosshairImage = imgObj.AddComponent<Image>();
            }
            else
            {
                crosshairImage = crosshairTransform.GetComponent<Image>();
            }
        }

        if (crosshairImage != null)
        {
            rectTransform = crosshairImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = crosshairSize;

            if (crosshairSprite != null)
            {
                crosshairImage.sprite = crosshairSprite;
            }

            crosshairImage.raycastTarget = false;
            crosshairImage.color = normalColor;
            initialScale = Vector3.one;
        }
    }
}
