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

    [Header("Judgement Ayarları")]
    public float judgementDisplayTime = 0.5f;
    private float judgementTimer = 0f;
    private CanvasGroup judgementCanvasGroup;

    [Header("Combo Ayarları")]
    public int minComboToShow = 3;

    [Header("Renkler")]
    public Color perfectColor = new Color(1f, 0.84f, 0f);
    public Color goodColor = new Color(0f, 1f, 0.5f);
    public Color okColor = new Color(0f, 0.7f, 1f);
    public Color missColor = new Color(1f, 0.2f, 0.2f);

    void Start()
    {
        if (judgementText != null)
        {
            judgementCanvasGroup = judgementText.GetComponent<CanvasGroup>();
            if (judgementCanvasGroup == null)
            {
                judgementCanvasGroup = judgementText.gameObject.AddComponent<CanvasGroup>();
            }
            judgementText.gameObject.SetActive(false);
        }

        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (gameManager == null) return;

        UpdateScoreText(gameManager.TotalScore);
        UpdateComboText(gameManager.CurrentCombo);

        if (Time.frameCount % 10 == 0)
        {
            UpdateAccuracy();
        }

        UpdateJudgementTimer();
    }

    void UpdateScoreText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score:N0}";
        }
    }

    void UpdateComboText(int combo)
    {
        if (comboText == null) return;

        if (combo >= minComboToShow)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"{combo} COMBO!";
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    void UpdateAccuracy()
    {
        if (accuracyText == null) return;

        var stats = gameManager.GetHitStats();
        int total = stats[HitResult.Perfect] + stats[HitResult.Good] + 
                   stats[HitResult.Ok] + stats[HitResult.Miss];

        if (total > 0)
        {
            int successful = stats[HitResult.Perfect] + stats[HitResult.Good] + stats[HitResult.Ok];
            float accuracy = ((float)successful / total) * 100f;
            accuracyText.text = $"Accuracy: {accuracy:F1}%";

            if (accuracy >= 95f)
                accuracyText.color = perfectColor;
            else if (accuracy >= 85f)
                accuracyText.color = goodColor;
            else if (accuracy >= 70f)
                accuracyText.color = okColor;
            else
                accuracyText.color = missColor;
        }
        else
        {
            accuracyText.text = "Accuracy: 100%";
            accuracyText.color = perfectColor;
        }
    }

    void UpdateJudgementTimer()
    {
        if (judgementTimer > 0)
        {
            judgementTimer -= Time.deltaTime;
            
            if (judgementText != null && judgementCanvasGroup != null)
            {
                float fadeProgress = judgementTimer / judgementDisplayTime;
                judgementCanvasGroup.alpha = fadeProgress;
            }
            
            if (judgementTimer <= 0 && judgementText != null)
            {
                judgementText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowJudgement(HitResult result)
    {
        if (judgementText == null) return;

        judgementText.gameObject.SetActive(true);
        judgementTimer = judgementDisplayTime;

        if (judgementCanvasGroup != null)
        {
            judgementCanvasGroup.alpha = 1f;
        }

        switch (result)
        {
            case HitResult.Perfect:
                judgementText.text = "PERFECT!";
                judgementText.color = perfectColor;
                break;
            case HitResult.Good:
                judgementText.text = "GOOD";
                judgementText.color = goodColor;
                break;
            case HitResult.Ok:
                judgementText.text = "OK";
                judgementText.color = okColor;
                break;
            case HitResult.Miss:
                judgementText.text = "MISS";
                judgementText.color = missColor;
                break;
        }
    }
}

