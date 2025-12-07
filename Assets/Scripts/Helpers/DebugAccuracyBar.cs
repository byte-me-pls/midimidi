using UnityEngine;
using System.Collections.Generic;

public class CodeDrawnAccuracyBar : MonoBehaviour
{
    [Header("Referans")]
    public MidiGameManager gameManager;

    [Header("Game Logic")]
    public int lowAccuracyIndex = 0; // Ceza Sayısı
    public float penaltyInterval = 3.0f; // Kaç saniyede bir ceza artsın?
    
    [Header("Ardı Ardına Basış Mantığı")]
    [Tooltip("Kaç doğru basış barı fullesin")]
    public int successStreakToFull = 5;
    
    [Tooltip("Kaç yanlış basış barı %30'un altına düşürsün")]
    public int errorStreakToCritical = 3;
    
    [Tooltip("Her doğru basışta bar ne kadar artmalı (0-1 arası)")]
    public float successIncrement = 0.2f; // 5 basışta full olması için 1/5 = 0.2
    
    [Tooltip("Her yanlış basışta bar ne kadar azalmalı (0-1 arası)")]
    public float errorDecrement = 0.233f; // 3 basışta %30'a düşmesi için (1-0.3)/3 = 0.233
    
    private bool isBelowThreshold = false; 
    private float penaltyTimer = 0f;

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
    
    // Önceki hata sayısını tutarak yeni hataları tespit edeceğiz
    private int previousPerfectCount = 0;
    private int previousGoodCount = 0;
    private int previousOkCount = 0;
    private int previousMissCount = 0;
    private int previousTooEarlyCount = 0;
    
    // İstatistikler
    private int consecutiveSuccesses = 0;
    private int consecutiveErrors = 0;

    void Start()
    {
        drawTexture = new Texture2D(1, 1);
        drawTexture.SetPixel(0, 0, Color.white);
        drawTexture.Apply();
        
        // Başlangıçta bar full
        currentAccuracy = 1f;
        targetAccuracy = 1f;
    }

    void Update()
    {
        if (gameManager != null)
        {
            var stats = gameManager.GetHitStats();
            
            // Yeni başarılı vuruş oldu mu?
            int currentPerfect = stats[HitResult.Perfect];
            int currentGood = stats[HitResult.Good];
            int currentOk = stats[HitResult.Ok];
            
            int newSuccesses = (currentPerfect - previousPerfectCount) + 
                              (currentGood - previousGoodCount) + 
                              (currentOk - previousOkCount);
            
            // Yeni hata oldu mu?
            int currentMiss = stats[HitResult.Miss];
            int currentTooEarly = stats[HitResult.TooEarly];
            
            int newErrors = (currentMiss - previousMissCount) + 
                           (currentTooEarly - previousTooEarlyCount);
            
            // BAŞARILI BASIŞLAR
            if (newSuccesses > 0)
            {
                consecutiveSuccesses += newSuccesses;
                consecutiveErrors = 0; // Hata streak'i kır
                
                // Bar'ı artır
                targetAccuracy = Mathf.Min(1f, targetAccuracy + (successIncrement * newSuccesses));
                
                Debug.Log($"✅ Doğru Basış! +{newSuccesses} | Streak: {consecutiveSuccesses} | Bar: {(targetAccuracy * 100):F1}%");
                
                // 5 ardı ardına doğru basışta full bar
                if (consecutiveSuccesses >= successStreakToFull)
                {
                    targetAccuracy = 1f;
                    Debug.Log($"🌟 {successStreakToFull} ARDIARDINA DOĞRU! BAR FULL!");
                }
            }
            
            // HATALI BASIŞLAR
            if (newErrors > 0)
            {
                consecutiveErrors += newErrors;
                consecutiveSuccesses = 0; // Başarı streak'i kır
                
                // Bar'ı azalt
                targetAccuracy = Mathf.Max(0f, targetAccuracy - (errorDecrement * newErrors));
                
                Debug.Log($"❌ Yanlış Basış! +{newErrors} | Streak: {consecutiveErrors} | Bar: {(targetAccuracy * 100):F1}%");
                
                // 3 ardı ardına hata → kritik seviye
                if (consecutiveErrors >= errorStreakToCritical)
                {
                    targetAccuracy = Mathf.Min(targetAccuracy, 0.29f); // %30'un altına düşür
                    Debug.Log($"💀 {errorStreakToCritical} ARDIARDINA HATA! KRİTİK SEVİYE!");
                }
            }
            
            // Önceki sayıları güncelle
            previousPerfectCount = currentPerfect;
            previousGoodCount = currentGood;
            previousOkCount = currentOk;
            previousMissCount = currentMiss;
            previousTooEarlyCount = currentTooEarly;
        }

        // Kritik Seviye Kontrolü (Zamanlayıcı ile)
        if (targetAccuracy < 0.3f)
        {
            if (!isBelowThreshold)
            {
                lowAccuracyIndex++;
                isBelowThreshold = true;
                penaltyTimer = 0f;
                Debug.Log($"🔴 KRİTİK SEVİYE! İlk Ceza. Index: {lowAccuracyIndex}");
            }
            else
            {
                penaltyTimer += Time.deltaTime;

                if (penaltyTimer >= penaltyInterval)
                {
                    lowAccuracyIndex++;
                    penaltyTimer = 0f;
                    Debug.Log($"🔴 KRİTİK SÜRE DOLDU! Ekstra Ceza. Index: {lowAccuracyIndex}");
                }
            }
        }
        else
        {
            isBelowThreshold = false;
            penaltyTimer = 0f;
        }

        // Smooth Geçiş
        currentAccuracy = Mathf.Lerp(currentAccuracy, targetAccuracy, Time.deltaTime * 5f);
    }

    void OnGUI()
    {
        if (drawTexture == null) return;

        float posX = (Screen.width - width) / 2f;
        float posY = Screen.height - bottomOffset; 

        // Arka plan
        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(posX, posY, width, height), drawTexture);

        // Bar rengi
        Color barColor;
        if (currentAccuracy > 0.5f)
            barColor = Color.Lerp(midColor, fullColor, (currentAccuracy - 0.5f) * 2f);
        else
            barColor = Color.Lerp(lowColor, midColor, currentAccuracy * 2f);

        GUI.color = barColor;
        float currentWidth = width * currentAccuracy;
        GUI.DrawTexture(new Rect(posX, posY, currentWidth, height), drawTexture);

        // Debug bilgisi
        GUI.color = Color.white;
        string debugText = $"Bar: {(currentAccuracy * 100f):F1}% | ✅ Streak: {consecutiveSuccesses} | ❌ Streak: {consecutiveErrors} | Penalty: {lowAccuracyIndex}";
        GUI.Label(new Rect(posX, posY - 25f, width, 20f), debugText);
    }
}