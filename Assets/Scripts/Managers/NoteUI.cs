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
    private bool isProcessed = false; // "isDestroyed" yerine "isProcessed" kullandım

    [Header("Görsel Ayarlar")]
    public Color normalColor = Color.white;
    public Color approachingColor = Color.yellow;
    public float colorChangeDistance = 150f;

    public int LaneIndex => laneIndex;
    public RectTransform RectTransform => rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        if (image != null) image.color = normalColor;
    }

    public void Initialize(MidiGameManager mgr, int lane, float moveSpeed)
    {
        manager = mgr;
        laneIndex = lane;
        speed = moveSpeed;
        isProcessed = false; // Resetlendiğinde tekrar işlenebilir hale getir
        
        if (image != null) image.color = normalColor;
    }

    void Update()
    {
        // Eğer obje havuza gönderildiyse veya manager yoksa çalışma
        if (isProcessed || manager == null) return;

        rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;

        UpdateColor();

        float signedDistance = manager.GetNoteSignedDistance(rectTransform);

        if (signedDistance < -manager.MissDistance)
        {
            OnMiss();
        }
    }

    void UpdateColor()
    {
        if (image == null || manager == null) return;

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

    public void OnHit()
    {
        if (isProcessed) return;
        isProcessed = true;
        
        // Destroy(gameObject) YERİNE:
        if (manager != null) manager.ReturnNoteToPool(this);
    }

    private void OnMiss()
    {
        if (isProcessed) return;
        isProcessed = true;

        if (manager != null)
        {
            manager.RegisterMiss(laneIndex);
            // Destroy(gameObject) YERİNE:
            manager.ReturnNoteToPool(this);
        }
    }
}