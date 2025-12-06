using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class NoteUI : MonoBehaviour
{
    private MidiGameManager manager;
    private RectTransform rectTransform;
    private Image image;
    private int laneIndex;
    private float speed;
    
    // Durum kontrolü
    private bool isProcessed = false;     // İşlem gördü mü? (Vuruldu veya Miss oldu)
    private bool isHitAnimating = false;  // Şu an vurulma animasyonu oynuyor mu?

    [Header("Görsel Ayarlar")]
    public Color normalColor = Color.white;
    public Color approachingColor = Color.yellow;
    public float colorChangeDistance = 150f;

    [Header("Vurulma Animasyonu (Hit FX)")]
    public float hitAnimDuration = 0.2f;  // Animasyon kaç saniye sürsün
    public float hitScaleTarget = 1.5f;   // Vurulunca kaç kat büyüsün
    public Color hitColor = Color.cyan;   // Vurulma anındaki renk

    private float hitAnimTimer = 0f;
    private Vector3 initialScale;

    public int LaneIndex => laneIndex;
    public RectTransform RectTransform => rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        initialScale = rectTransform.localScale; // Orijinal boyutu kaydet
        
        if (image != null) image.color = normalColor;
    }

    public void Initialize(MidiGameManager mgr, int lane, float moveSpeed)
    {
        manager = mgr;
        laneIndex = lane;
        speed = moveSpeed;
        
        // Durumları sıfırla
        isProcessed = false;
        isHitAnimating = false;
        hitAnimTimer = 0f;

        // Görünümü sıfırla (Havuzdan kirli gelebilir)
        rectTransform.localScale = initialScale; // Boyutu düzelt
        if (image != null)
        {
            image.color = normalColor; // Rengi düzelt
            
            // Alpha'yı fulle (Eğer animasyonda kıstıysak geri açalım)
            Color c = image.color;
            c.a = 1f;
            image.color = c;
        }
    }

    void Update()
    {
        // Eğer manager yoksa veya Miss olduysa ve işlendiyse dur
        if (manager == null) return;

        // --- 1. DURUM: VURULMA ANİMASYONU ---
        if (isHitAnimating)
        {
            PlayHitAnimation();
            return; // Aşağıdaki hareket kodlarını çalıştırma
        }

        // Eğer miss olduysa ve havuza gidiyorsa daha fazla işlem yapma
        if (isProcessed) return;

        // --- 2. DURUM: NORMAL HAREKET ---
        rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        UpdateColor();

        // Miss Kontrolü (HitBar'a göre)
        float signedDistance = manager.GetNoteSignedDistance(rectTransform);
        if (signedDistance < -manager.MissDistance)
        {
            OnMiss();
        }
    }

    // Vurulma efekti (Büyüme + Şeffaflaşma)
    void PlayHitAnimation()
    {
        hitAnimTimer += Time.deltaTime;
        float progress = hitAnimTimer / hitAnimDuration; // 0 ile 1 arası ilerleme

        if (progress >= 1f)
        {
            // Animasyon bitti, artık havuza dönebiliriz
            isHitAnimating = false;
            manager.ReturnNoteToPool(this);
        }
        else
        {
            // A. Scale Efekti (Büyüt)
            float currentScale = Mathf.Lerp(1f, hitScaleTarget, progress);
            rectTransform.localScale = initialScale * currentScale;

            // B. Alpha Efekti (Şeffaflaştır)
            if (image != null)
            {
                Color c = hitColor; // Parlak renk
                c.a = Mathf.Lerp(1f, 0f, progress); // Görünürden görünmeze
                image.color = c;
            }
        }
    }

    void UpdateColor()
    {
        if (image == null) return;

        float absDist = Mathf.Abs(manager.GetNoteSignedDistance(rectTransform));

        if (absDist <= colorChangeDistance)
        {
            float t = absDist / colorChangeDistance;
            image.color = Color.Lerp(approachingColor, normalColor, t);
        }
        else
        {
            image.color = normalColor;
        }
    }

    // Manager tarafından çağrılır
    public void OnHit()
    {
        if (isProcessed) return;
        isProcessed = true;
        
        // Hemen yok etme! Animasyonu başlat.
        isHitAnimating = true;
        hitAnimTimer = 0f;
    }

    private void OnMiss()
    {
        if (isProcessed) return;
        isProcessed = true;

        if (manager != null)
        {
            manager.RegisterMiss(laneIndex);
            // Miss olunca animasyona gerek yok, direkt gönder
            manager.ReturnNoteToPool(this);
        }
        // Manager yoksa güvenlik için destroy (nadiren olur)
        else
        {
            Destroy(gameObject);
        }
    }
}