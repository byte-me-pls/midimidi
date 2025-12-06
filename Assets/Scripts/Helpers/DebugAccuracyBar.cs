using UnityEngine;
using System.Collections.Generic;

public class CodeDrawnAccuracyBar : MonoBehaviour
{
    [Header("Referans")]
    public MidiGameManager gameManager;

    [Header("Game Logic")]
    public int lowAccuracyIndex = 0; // <-- ARTACAK OLAN INDEX
    private bool isBelowThreshold = false; // Tekrar tekrar artmasını önleyen bayrak

    [Header("Konum Ayarları")]
    public float bottomOffset = 100f; 
    public float width = 800f;
    public float height = 30f;
    
    [Header("Renkler")]
    public Color fullColor = Color.green;   
    public Color midColor = Color.yellow;   
    public Color lowColor = Color.red;      
    public Color backgroundColor = new Color(0, 0, 0, 0.5f); 

    private float currentAccuracy = 1f;
    private float targetAccuracy = 1f;
    
    private Texture2D drawTexture;

    void Start()
    {
        drawTexture = new Texture2D(1, 1);
        drawTexture.SetPixel(0, 0, Color.white);
        drawTexture.Apply();
    }

    void Update()
    {
        if (gameManager != null)
        {
            var stats = gameManager.GetHitStats();
            int total = stats[HitResult.Perfect] + stats[HitResult.Good] + 
                       stats[HitResult.Ok] + stats[HitResult.Miss];
            
            if (total > 0)
            {
                int successful = stats[HitResult.Perfect] + stats[HitResult.Good] + stats[HitResult.Ok];
                targetAccuracy = (float)successful / total;
            }
        }
        else
        {
            targetAccuracy = 1f;
        }

        // --- YENİ EKLENEN MANTIK ---
        // Eğer Doğruluk %30'un (0.3) altındaysa
        if (targetAccuracy < 0.3f)
        {
            // Ve daha önce bu düşüşü kaydetmediysek
            if (!isBelowThreshold)
            {
                lowAccuracyIndex++; // Indexi arttır
                isBelowThreshold = true; // Bayrağı kaldır (tekrar artmasın diye)
                Debug.Log($"DİKKAT! Accuracy Kritik Seviyede! Index: {lowAccuracyIndex}");
            }
        }
        else
        {
            // Eğer accuracy tekrar %30'un üzerine çıkarsa bayrağı indir
            // Böylece ilerde tekrar düşerse yeniden sayabiliriz.
            isBelowThreshold = false;
        }
        // ---------------------------

        currentAccuracy = Mathf.Lerp(currentAccuracy, targetAccuracy, Time.deltaTime * 5f);
    }

    void OnGUI()
    {
        if (drawTexture == null) return;

        float posX = (Screen.width - width) / 2f;
        float posY = Screen.height - bottomOffset; 

        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(posX, posY, width, height), drawTexture);

        Color barColor;
        if (currentAccuracy > 0.5f)
            barColor = Color.Lerp(midColor, fullColor, (currentAccuracy - 0.5f) * 2f);
        else
            barColor = Color.Lerp(lowColor, midColor, currentAccuracy * 2f);

        GUI.color = barColor;
        float currentWidth = width * currentAccuracy;
        GUI.DrawTexture(new Rect(posX, posY, currentWidth, height), drawTexture);

        GUI.color = Color.white;
    }
}