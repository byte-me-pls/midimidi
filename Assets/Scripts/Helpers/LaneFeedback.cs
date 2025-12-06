using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(AudioSource))]
public class LaneFeedback : MonoBehaviour
{
    [Header("Efekt Ayarları")]
    public Color pressColor = Color.red;
    public float scaleMultiplier = 1.2f;
    public float fadeSpeed = 10f;

    [Header("Ses Ayarları")]
    public AudioClip tapSound;
    [Range(0f, 1f)] public float volume = 1f;

    private Vector3 baseScale;
    private Color baseColor;
    private Image targetImage;
    private RectTransform rectTransform;
    private AudioSource audioSource;
    
    private float currentIntensity = 0f;
    private bool isHolding = false; // Tuşa basılı tutuluyor mu?

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();

        baseScale = rectTransform.localScale;
        
        if (targetImage != null)
        {
            baseColor = targetImage.color;
        }

        audioSource.playOnAwake = false;
        audioSource.clip = tapSound; // Clip'i kaynağa ata
    }

    // TUŞA BASINCA (NOTE ON)
    public void OnLaneHit(float velocity = 1.0f)
    {
        currentIntensity = 1f * velocity;
        isHolding = true;

        if (audioSource != null && tapSound != null)
        {
            audioSource.volume = volume * velocity;
            audioSource.Play(); // Sesi başlat
        }
    }

    // TUŞU BIRAKINCA (NOTE OFF) - YENİ FONKSİYON
    public void OnLaneRelease()
    {
        isHolding = false;

        if (audioSource != null)
        {
            audioSource.Stop(); // Sesi anında kes
        }
    }

    void Update()
    {
        // Tuşa basılı tutuyorsa görsel efekti canlı tut
        if (isHolding)
        {
            currentIntensity = Mathf.Lerp(currentIntensity, 1f, Time.deltaTime * 10f);
        }
        else
        {
            // Bıraktıysa yavaşça sönsün
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * fadeSpeed);
        }

        // Görseli uygula
        float s = 1f + (currentIntensity * (scaleMultiplier - 1f));
        rectTransform.localScale = baseScale * s;

        if (targetImage != null)
        {
            targetImage.color = Color.Lerp(baseColor, pressColor, currentIntensity);
        }
    }
}