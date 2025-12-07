using UnityEngine;

public class RhythmAudioController : MonoBehaviour
{
    [Header("Müzik Kanalları (Stems)")]
    public AudioSource organSource;     // Senin çaldığın kanal (Org/Piyano)
    
    [Header("Arkaplan Müzikleri (3 Adet)")]
    public AudioSource elektroSource;   // Elektro
    public AudioSource bassSource;      // Bass
    public AudioSource drumsSource;     // Drums

    [Header("Hata Efektleri")]
    public AudioSource sfxSource;       // Hata sesi için kaynak
    public AudioClip errorClip;         // "Dıt" sesi

    [Header("Organ Temel Ayarları")]
    [Tooltip("Org kanalının referans (temel) volumü.")]
    public float baseOrganVolume = 1f;

    [Tooltip("Organ volumünün hedefe yaklaşma hızı.")]
    public float volumeLerpSpeed = 10f;

    [Header("Performans Eğrisi (Curve Mantığı)")]
    [Tooltip("Performans skorunu (accuracy 0–1) volume'a çeviren eğri.\nX = accuracy, Y = mult. (0–1).")]
    public AnimationCurve accuracyToVolumeCurve = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);

    [Header("Accuracy Dinamiği")]
    [Tooltip("İyi vuruşta accuracy ne kadar artsın.")]
    public float accuracyIncreaseOnGood = 0.08f;

    [Tooltip("Hata olduğunda accuracy ne kadar azalsın.")]
    public float accuracyDecreaseOnError = 0.18f;

    [Tooltip("Zamanla kendi kendine toparlanma hızı (saniyede). 0 yaparsan sadece düzgün basışlar düzeltir.")]
    public float passiveRecoveryPerSecond = 0.02f;

    [Tooltip("Accuracy başlangıç seviyesi (genelde 1).")]
    public float initialAccuracy = 1f;

    // 0–1 arası performans değeri
    private float accuracy;

    // Müzik başladı mı?
    private bool musicStarted = false;

    void Start()
    {
        // Tüm AudioSource'ları hazırla ama BAŞLATMA
        if (organSource != null)
        {
            organSource.Stop();
            organSource.volume = 0f; // Fade ile curve'e oturacak
        }

        if (elektroSource != null)
            elektroSource.Stop();
        
        if (bassSource != null)
            bassSource.Stop();
        
        if (drumsSource != null)
            drumsSource.Stop();

        accuracy = Mathf.Clamp01(initialAccuracy); // genelde 1
        musicStarted = false;
    }

    void Update()
    {
        if (!musicStarted || organSource == null)
            return;

        // İsteğe bağlı pasif toparlanma (istersen Inspector'dan 0 yap)
        if (passiveRecoveryPerSecond > 0f && accuracy < 1f)
        {
            accuracy += passiveRecoveryPerSecond * Time.deltaTime;
            accuracy = Mathf.Clamp01(accuracy);
        }

        // Accuracy'yi eğri üzerinden volume multiplier'a çevir
        float curveValue = accuracyToVolumeCurve != null 
            ? accuracyToVolumeCurve.Evaluate(accuracy) 
            : accuracy; // Eğri yoksa düz lineer kullan

        float targetVolume = baseOrganVolume * curveValue;
        targetVolume = Mathf.Clamp01(targetVolume);

        // Volume'ü yumuşakça target'a çek
        organSource.volume = Mathf.Lerp(
            organSource.volume,
            targetVolume,
            Time.deltaTime * volumeLerpSpeed
        );
    }

    /// <summary>
    /// Müzik başlatma: Dışarıdan (MidiGameManager veya başka controller) çağrılmalı.
    /// Sen zaten ilk nota hitbar'a gelince StartMusic() çağırıyorsun / çağıracaksın.
    /// </summary>
    public void StartMusic()
    {
        if (musicStarted) return;
        musicStarted = true;

        if (organSource != null)
        {
            organSource.volume = 0f; // Curve ile yukarı çıkacak
            organSource.Play();
        }

        if (elektroSource != null)
            elektroSource.Play();
        
        if (bassSource != null)
            bassSource.Play();
        
        if (drumsSource != null)
            drumsSource.Play();

        Debug.Log("🎵 Müzik başlatıldı!");
    }

    /// <summary>
    /// Hata (yanlış tuş, miss, erken basış) olduğunda çağır.
    /// Pitch / tempo bozmadan sadece accuracy düşürür.
    /// </summary>
    public void RegisterError()
    {
        if (!musicStarted)
        {
            // Müzik başlamadan hatalar = sadece SFX
            PlayErrorSound();
            return;
        }

        // Accuracy'yi aşağı çek → eğri üzerinden volume bozuluyor
        accuracy = Mathf.Clamp01(accuracy - accuracyDecreaseOnError);

        // Hata sesi
        PlayErrorSound();
    }

    /// <summary>
    /// Başarılı vuruşlarda (Perfect/Good/Ok) çağır.
    /// Accuracy'i artırır; eğriye göre ses eski çizgiye doğru düzelir.
    /// </summary>
    public void RegisterGoodHit()
    {
        if (!musicStarted) return;

        accuracy = Mathf.Clamp01(accuracy + accuracyIncreaseOnGood);
    }

    /// <summary>
    /// Oyun resetlendiğinde çağır (tam temizle).
    /// </summary>
    public void ResetAudio()
    {
        musicStarted = false;
        accuracy = Mathf.Clamp01(initialAccuracy);

        if (organSource != null)
        {
            organSource.Stop();
            organSource.volume = 0f;
        }
        
        if (elektroSource != null)
            elektroSource.Stop();
        
        if (bassSource != null)
            bassSource.Stop();
        
        if (drumsSource != null)
            drumsSource.Stop();
    }

    // Eski API'lerle uyum için basit stub'lar:

    public void MuteOrgan()
    {
        if (organSource != null)
            organSource.volume = 0f;
    }

    public void UnmuteOrgan()
    {
        if (organSource != null)
            organSource.volume = baseOrganVolume;
    }

    public void PlayErrorSound()
    {
        if (sfxSource != null && errorClip != null)
            sfxSource.PlayOneShot(errorClip, 0.7f);
    }

    /// <summary>
    /// Patlama olduğunda elektro sesini kapat
    /// </summary>
    public void MuteElektroOnExplosion()
    {
        if (elektroSource != null)
        {
            elektroSource.volume = 0f;
            Debug.Log("💥 [Audio] Elektro sesi patlamada kapatıldı!");
        }
    }

    /// <summary>
    /// Elektro sesini tekrar aç (opsiyonel)
    /// </summary>
    public void UnmuteElektro()
    {
        if (elektroSource != null)
        {
            elektroSource.volume = 1f;
            Debug.Log("🔊 [Audio] Elektro sesi açıldı!");
        }
    }
}
