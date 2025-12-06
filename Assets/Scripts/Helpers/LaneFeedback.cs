using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LaneFeedback : MonoBehaviour
{
    [Header("Efekt Ayarları")]
    public Color pressColor = Color.red;     // Basılınca olacak renk
    public float scaleMultiplier = 1.2f;     // Ne kadar büyüyeceği (örn: 1.2 katı)
    public float fadeSpeed = 10f;            // Normale dönme hızı

    private Vector3 baseScale;
    private Color baseColor;
    private Image targetImage;
    private RectTransform rectTransform;
    
    // Anlık efekt değeri (1 = tam basılı, 0 = normal)
    private float currentIntensity = 0f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>();

        // Başlangıç değerlerini kaydet
        baseScale = rectTransform.localScale;
        
        if (targetImage != null)
        {
            baseColor = targetImage.color;
        }
    }

    public void OnLaneHit(float velocity = 1.0f)
    {
        // Vuruş geldiğinde yoğunluğu artır (Velocity'ye göre şiddeti değişebilir)
        currentIntensity = 1f * velocity;
    }

    void Update()
    {
        // Efekt varsa uygula
        if (currentIntensity > 0.001f)
        {
            // 1. Scale Efekti (Senin kodundaki mantık)
            float s = 1f + (currentIntensity * (scaleMultiplier - 1f));
            rectTransform.localScale = baseScale * s;

            // 2. Renk Efekti (Kırmızıya dönme)
            if (targetImage != null)
            {
                targetImage.color = Color.Lerp(baseColor, pressColor, currentIntensity);
            }

            // Zamanla normale dön (Lerp benzeri sönümleme)
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * fadeSpeed);
        }
        else
        {
            // Tamamen normale sabitle (titremeyi önlemek için)
            rectTransform.localScale = baseScale;
            if (targetImage != null) targetImage.color = baseColor;
            currentIntensity = 0f;
        }
    }
}