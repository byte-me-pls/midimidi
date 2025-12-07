using UnityEngine;

public class AccuracyPenaltyManager : MonoBehaviour
{
    [Header("Referanslar")]
    public CodeDrawnAccuracyBar accuracyBar;
    public GameObject ambulance;

    private int lastRecordedIndex = 0;
    private GameObject fok;
    void Start()
    {
        fok = GameObject.FindWithTag("fok");
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

        switch (penaltyIndex)
        {
            case 1:
                if (ObjectPulseEffect.Instance != null)
                {
                     ObjectPulseEffect.Instance.TriggerPulse(2);
                }
                   
                else
                    Debug.LogWarning("PulseEffect yok (seviye 1).");
                break;

            case 2:
                if (ObjectPulseEffect.Instance != null)
                    ObjectPulseEffect.Instance.TriggerPulse(4);
                else
                    Debug.LogWarning("PulseEffect yok (seviye 2).");
                break;

            case 3:
                if (ObjectPulseEffect.Instance != null)
                { 
                    ObjectPulseEffect.Instance.Explode();
                }
                
                else
                    Debug.LogWarning("PulseEffect yok (seviye 3, patlama atlandı).");
                break;

            case 4:
                if (ambulance != null)
                    ambulance.SetActive(true);
                Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                JumpEffect.Instance.TriggerJump(fok, "BASS SOLOOO");
                break;

            default:
                // İstersen logla
                break;
        }
    }
}