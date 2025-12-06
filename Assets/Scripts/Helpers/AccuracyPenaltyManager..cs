using UnityEngine;

public class AccuracyPenaltyManager : MonoBehaviour
{
    [Header("Referanslar")]
    public CodeDrawnAccuracyBar accuracyBar; 

    private int lastRecordedIndex = 0;

    void Start()
    {
        if (accuracyBar == null)
            accuracyBar = FindObjectOfType<CodeDrawnAccuracyBar>();

        if (accuracyBar != null)
            lastRecordedIndex = accuracyBar.lowAccuracyIndex;
    }

    void Update()
    {
        if (accuracyBar == null) return;

        if (accuracyBar.lowAccuracyIndex > lastRecordedIndex)
        {
            lastRecordedIndex = accuracyBar.lowAccuracyIndex;
            TriggerPenaltyEffect(lastRecordedIndex);
        }
    }

    void TriggerPenaltyEffect(int penaltyIndex)
    {
        Debug.Log($"Ceza Tetiklendi! Seviye: {penaltyIndex}");

        if (ObjectPulseEffect.Instance == null) return;

        // --- GÜNCELLENEN KISIM ---
        switch (penaltyIndex)
        {
            case 1:
                ObjectPulseEffect.Instance.TriggerPulse(2); // 1. Seviye Büyüme
                break;

            case 2:
                ObjectPulseEffect.Instance.TriggerPulse(4); // 2. Seviye Büyüme
                break;

            case 3:
                ObjectPulseEffect.Instance.Explode(); // Patlama
                break;
                
            default:
                // 3'ten sonrası için bir şey yapma (zaten yok oldu)
                break;
        }
    }
}