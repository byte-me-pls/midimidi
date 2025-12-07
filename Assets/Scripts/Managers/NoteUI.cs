using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class NoteUI : MonoBehaviour
{
    private MidiGameManager manager;
    private RectTransform rectTransform;
    private Image image;
    private RawImage rawImage;
    private int midiNoteNumber;
    private float speed;
    
    // Durum kontrolü
    private bool isProcessed = false;
    private bool isHitAnimating = false;
    private bool isBlackNote = false;
    private bool useRawImage = false;

    [Header("Görsel Ayarlar - Beyaz Notalar")]
    public Color normalColor = Color.white;
    public Color approachingColor = Color.yellow;
    
    [Header("Görsel Ayarlar - Siyah Notalar")]
    public Color blackNoteColor = Color.black;
    public Color blackApproachingColor = new Color(0.4f, 0.4f, 0.4f);
    
    [Header("Genel Ayarlar")]
    public float colorChangeDistance = 150f;

    [Header("Vurulma Animasyonu")]
    public float hitAnimDuration = 0.2f;
    public float hitScaleTarget = 1.5f;
    public Color hitColor = Color.cyan;

    private float hitAnimTimer = 0f;
    private Vector3 initialScale;

    public int LaneIndex => midiNoteNumber;
    public RectTransform RectTransform => rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Image veya RawImage'i bul
        image = GetComponent<Image>();
        rawImage = GetComponent<RawImage>();
        
        useRawImage = (rawImage != null);
        
        initialScale = rectTransform.localScale;
        
        SetColor(normalColor);
    }

    // Renk ayarlama helper metodu (Image veya RawImage)
    void SetColor(Color color)
    {
        if (useRawImage && rawImage != null)
        {
            rawImage.color = color;
        }
        else if (image != null)
        {
            image.color = color;
        }
    }

    // Renk alma helper metodu
    Color GetColor()
    {
        if (useRawImage && rawImage != null)
        {
            return rawImage.color;
        }
        else if (image != null)
        {
            return image.color;
        }
        return Color.white;
    }

    public void Initialize(MidiGameManager mgr, int midiNote, float moveSpeed)
    {
        manager = mgr;
        midiNoteNumber = midiNote;
        speed = moveSpeed;
        
        // Direkt MIDI numarasına göre siyah nota kontrolü
        // Siyah notalar: 53, 56, 58, 61, 63
        isBlackNote = (midiNote == 54 || midiNote == 56 || midiNote == 58 || 
                       midiNote == 61 || midiNote == 63);
        
        // Durumları sıfırla
        isProcessed = false;
        isHitAnimating = false;
        hitAnimTimer = 0f;

        // Görünümü ayarla
        rectTransform.localScale = initialScale;
        
        Color targetColor = isBlackNote ? blackNoteColor : normalColor;
        targetColor.a = 1f;
        SetColor(targetColor);
        
        // Debug
        Debug.Log($"🎵 Nota spawn: MIDI {midiNote} | Renk: {(isBlackNote ? "SİYAH ⚫" : "BEYAZ ⚪")} | Tip: {(useRawImage ? "RawImage" : "Image")}");
    }

    void Update()
    {
        if (manager == null) return;

        // Vurulma animasyonu
        if (isHitAnimating)
        {
            PlayHitAnimation();
            return;
        }

        if (isProcessed) return;

        // Normal hareket
        rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;
        UpdateColor();

        // Miss kontrolü
        float signedDistance = manager.GetNoteSignedDistance(rectTransform);
        if (signedDistance < -manager.MissDistance)
        {
            OnMiss();
        }
    }

    void PlayHitAnimation()
    {
        hitAnimTimer += Time.deltaTime;
        float progress = hitAnimTimer / hitAnimDuration;

        if (progress >= 1f)
        {
            isHitAnimating = false;
            manager.ReturnNoteToPool(this);
        }
        else
        {
            float currentScale = Mathf.Lerp(1f, hitScaleTarget, progress);
            rectTransform.localScale = initialScale * currentScale;

            Color c = hitColor;
            c.a = Mathf.Lerp(1f, 0f, progress);
            SetColor(c);
        }
    }

    void UpdateColor()
    {
        float absDist = Mathf.Abs(manager.GetNoteSignedDistance(rectTransform));

        Color baseColor = isBlackNote ? blackNoteColor : normalColor;
        Color targetColor = isBlackNote ? blackApproachingColor : approachingColor;

        if (absDist <= colorChangeDistance)
        {
            float t = absDist / colorChangeDistance;
            SetColor(Color.Lerp(targetColor, baseColor, t));
        }
        else
        {
            SetColor(baseColor);
        }
    }

    public void OnHit()
    {
        if (isProcessed) return;
        isProcessed = true;
        isHitAnimating = true;
        hitAnimTimer = 0f;
    }

    private void OnMiss()
    {
        if (isProcessed) return;
        isProcessed = true;

        if (manager != null)
        {
            manager.RegisterMiss(midiNoteNumber);
            manager.ReturnNoteToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}