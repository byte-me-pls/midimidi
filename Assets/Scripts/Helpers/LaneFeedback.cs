using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LaneFeedback : MonoBehaviour
{
    [Header("Efekt Ayarları")]
    public Color pressColor = Color.red;
    public float scaleMultiplier = 1.2f;
    public float fadeSpeed = 10f;

    private Vector3 baseScale;
    private Color baseColor;
    private Image targetImage;
    private RectTransform rectTransform;

    private float currentIntensity = 0f;
    private bool isHolding = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>();

        baseScale = rectTransform.localScale;
        if (targetImage != null)
            baseColor = targetImage.color;
    }

    public void OnLaneHit(float velocity = 1.0f)
    {
        currentIntensity = Mathf.Clamp01(velocity);
        isHolding = true;
    }

    public void OnLaneRelease()
    {
        isHolding = false;
    }

    void Update()
    {
        if (isHolding)
            currentIntensity = Mathf.Lerp(currentIntensity, 1f, Time.deltaTime * 20f);
        else
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * fadeSpeed);

        float s = 1f + (currentIntensity * (scaleMultiplier - 1f));
        rectTransform.localScale = baseScale * s;

        if (targetImage != null)
            targetImage.color = Color.Lerp(baseColor, pressColor, currentIntensity);
    }
}