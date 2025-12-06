using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreDisplayUI : MonoBehaviour
{
    [Header("Referanslar")]
    public MidiGameManager gameManager;

    [Header("UI Text (TextMeshPro)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI judgementText;
    public TextMeshProUGUI accuracyText;

    // --- JUDGEMENT DEĞİŞKENLERİ ---
    [Header("Judgement Animasyonu")]
    public float judgementDisplayTime = 0.5f;
    public float floatSpeed = 100f; // Yukarı kayma hızı
    private float judgementTimer = 0f;
    private CanvasGroup judgementCanvasGroup;
    private RectTransform judgementRect;
    private Vector2 originalJudgementPos;

    // --- COMBO DEĞİŞKENLERİ ---
    [Header("Combo Animasyonu")]
    public float comboDisplayTime = 1.0f;
    public int minComboToShow = 3;
    
    [Header("Combo Titreme (Shake) Ayarları")]
    public float baseShakeAmount = 5f;    // En az ne kadar titresin
    public float maxShakeAmount = 25f;    // En fazla ne kadar titresin (Sınır)
    public float shakeMultiplier = 0.2f;  // Combo başına ne kadar artsın (Combo * 0.2)
    
    private float comboTimer = 0f;
    private CanvasGroup comboCanvasGroup;
    private RectTransform comboRect;
    private Vector2 originalComboPos;
    private int lastComboValue = -1;
    
    // Titreme ve Yüzme hesaplaması için anlık Y konumu
    private float currentComboFloatY = 0f;
    private float currentShakeIntensity = 0f;

    [Header("Renkler")]
    public Color perfectColor = new Color(1f, 0.84f, 0f);
    public Color goodColor = new Color(0f, 1f, 0.5f);
    public Color okColor = new Color(0f, 0.7f, 1f);
    public Color missColor = new Color(1f, 0.2f, 0.2f);
    public Color comboColor = new Color(1f, 0.6f, 0.2f);

    void Start()
    {
        // --- JUDGEMENT KURULUMU ---
        if (judgementText != null)
        {
            judgementCanvasGroup = EnsureCanvasGroup(judgementText.gameObject);
            judgementRect = judgementText.GetComponent<RectTransform>();
            if (judgementRect != null) originalJudgementPos = judgementRect.anchoredPosition;
            judgementText.gameObject.SetActive(false);
        }

        // --- COMBO KURULUMU ---
        if (comboText != null)
        {
            comboCanvasGroup = EnsureCanvasGroup(comboText.gameObject);
            comboRect = comboText.GetComponent<RectTransform>();
            if (comboRect != null) originalComboPos = comboRect.anchoredPosition;
            
            comboText.color = comboColor;
            comboText.gameObject.SetActive(false);
        }
    }

    CanvasGroup EnsureCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    void Update()
    {
        if (gameManager == null) return;

        UpdateScoreText(gameManager.TotalScore);
        CheckComboUpdate();

        if (Time.frameCount % 10 == 0) UpdateAccuracy();

        HandleJudgementAnimation();
        HandleComboAnimation();
    }

    void CheckComboUpdate()
    {
        int currentCombo = gameManager.CurrentCombo;

        if (currentCombo != lastComboValue)
        {
            lastComboValue = currentCombo;

            if (currentCombo >= minComboToShow)
            {
                TriggerComboAnimation(currentCombo);
            }
            else
            {
                if (comboText != null) comboText.gameObject.SetActive(false);
            }
        }
    }

    void TriggerComboAnimation(int combo)
    {
        if (comboText == null) return;

        comboText.text = $"{combo}\nCOMBO";
        comboText.gameObject.SetActive(true);
        
        // Timer Reset
        comboTimer = comboDisplayTime;
        
        // Görünürlük Reset
        if (comboCanvasGroup != null) comboCanvasGroup.alpha = 1f;
        
        // Hareket Değişkenlerini Resetle
        currentComboFloatY = 0f; // Yüzme sıfırlanır
        
        // --- TİTREME ŞİDDETİ HESABI ---
        // Temel Titreme + (Combo Sayısı * Çarpan)
        // Örn: 100 Combo * 0.2 = 20 birim ek titreme
        float calculatedShake = baseShakeAmount + (combo * shakeMultiplier);
        
        // Titremeyi Max değere sabitle (Çok abartmasın)
        currentShakeIntensity = Mathf.Min(calculatedShake, maxShakeAmount);

        // Scale Pop Efekti (Combo arttıkça biraz daha büyüsün)
        float popScale = 1.2f + Mathf.Min(combo * 0.005f, 0.5f); // Max 1.7x büyüklük
        comboRect.localScale = Vector3.one * popScale;
    }

    void HandleComboAnimation()
    {
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;

            if (comboRect != null)
            {
                // 1. Yukarı Yüzme (Float Up) Değerini Artır
                currentComboFloatY += floatSpeed * 0.5f * Time.deltaTime;

                // 2. Titreme (Shake) Hesapla
                // Random.insideUnitCircle: 1 birim yarıçaplı daire içinde rastgele nokta verir
                Vector2 shakeOffset = Random.insideUnitCircle * currentShakeIntensity;

                // 3. Pozisyonu Uygula: Orijinal Yer + Yüzme Miktarı + Titreme
                comboRect.anchoredPosition = originalComboPos + new Vector2(0, currentComboFloatY) + shakeOffset;
                
                // 4. Büyüklüğü normale döndür (Pop etkisi sönümlensin)
                comboRect.localScale = Vector3.Lerp(comboRect.localScale, Vector3.one, Time.deltaTime * 10f);
            }

            // Fade Out
            if (comboCanvasGroup != null)
            {
                if (comboTimer < comboDisplayTime * 0.5f)
                    comboCanvasGroup.alpha = comboTimer / (comboDisplayTime * 0.5f);
                else
                    comboCanvasGroup.alpha = 1f;
            }

            // Süre bitti
            if (comboTimer <= 0 && comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }

    void HandleJudgementAnimation()
    {
        if (judgementTimer > 0)
        {
            judgementTimer -= Time.deltaTime;

            // Judgement sadece yukarı kayar, titremez (daha temiz görünsün diye)
            if (judgementRect != null)
                judgementRect.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;

            if (judgementCanvasGroup != null)
                judgementCanvasGroup.alpha = judgementTimer / judgementDisplayTime;

            if (judgementTimer <= 0 && judgementText != null)
                judgementText.gameObject.SetActive(false);
        }
    }

    public void ShowJudgement(HitResult result)
    {
        if (judgementText == null) return;

        judgementText.gameObject.SetActive(true);
        judgementTimer = judgementDisplayTime;

        if (judgementCanvasGroup != null) judgementCanvasGroup.alpha = 1f;
        if (judgementRect != null) judgementRect.anchoredPosition = originalJudgementPos;

        switch (result)
        {
            case HitResult.Perfect: judgementText.text = "PERFECT"; judgementText.color = perfectColor; break;
            case HitResult.Good: judgementText.text = "GOOD"; judgementText.color = goodColor; break;
            case HitResult.Ok: judgementText.text = "OK"; judgementText.color = okColor; break;
            case HitResult.Miss: judgementText.text = "MISS"; judgementText.color = missColor; break;
        }
    }

    void UpdateScoreText(int score)
    {
        if (scoreText != null) scoreText.text = $"Score: {score:N0}";
    }

    void UpdateAccuracy()
    {
        if (accuracyText == null) return;
        var stats = gameManager.GetHitStats();
        int total = stats[HitResult.Perfect] + stats[HitResult.Good] + stats[HitResult.Ok] + stats[HitResult.Miss];

        if (total > 0)
        {
            int successful = stats[HitResult.Perfect] + stats[HitResult.Good] + stats[HitResult.Ok];
            float accuracy = ((float)successful / total) * 100f;
            accuracyText.text = $"{accuracy:F1}%";
        }
        else accuracyText.text = "100%";
    }
}