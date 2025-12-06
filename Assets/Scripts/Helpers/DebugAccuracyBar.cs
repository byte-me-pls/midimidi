using UnityEngine;
using System.Collections.Generic;

public class CodeDrawnAccuracyBar : MonoBehaviour
{
    [Header("Referans")]
    public MidiGameManager gameManager;

    [Header("Game Logic")]
    public int lowAccuracyIndex = 0; // Ceza Sayısı
    public float penaltyInterval = 3.0f; // Kaç saniyede bir ceza artsın?
    
    private bool isBelowThreshold = false; 
    private float penaltyTimer = 0f; // Süreyi tutacak sayaç

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
        // 1. Accuracy Hesapla
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

        // --- GÜNCELLENMİŞ MANTIK (ZAMANLAYICI DAHİL) ---
        
        // Eğer Accuracy %30'un altındaysa
        if (targetAccuracy < 0.3f)
        {
            // A. İLK GİRİŞ ANI
            if (!isBelowThreshold)
            {
                lowAccuracyIndex++; // İlk cezayı kes
                isBelowThreshold = true;
                penaltyTimer = 0f; // Sayacı sıfırla
                Debug.Log($"KRİTİK SEVİYE! İlk Ceza. Index: {lowAccuracyIndex}");
            }
            // B. ZATEN AŞAĞIDA VE BEKLİYORSA
            else
            {
                // Sayacı çalıştır
                penaltyTimer += Time.deltaTime;

                // 3 saniye doldu mu?
                if (penaltyTimer >= penaltyInterval)
                {
                    lowAccuracyIndex++; // Ekstra ceza kes
                    penaltyTimer = 0f; // Sayacı sıfırla (tekrar 3 sn sayması için)
                    Debug.Log($"KRİTİK SÜRE DOLDU! Ekstra Ceza. Index: {lowAccuracyIndex}");
                }
            }
        }
        else
        {
            // Accuracy düzelirse her şeyi sıfırla
            isBelowThreshold = false;
            penaltyTimer = 0f;
        }
        // -----------------------------------------------

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