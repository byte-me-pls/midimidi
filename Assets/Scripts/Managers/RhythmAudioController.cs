using UnityEngine;

public class RhythmAudioController : MonoBehaviour
{
    [Header("Müzik Kanalları (Stems)")]
    public AudioSource organSource;     // Senin çaldığın kanal (Org/Piyano)
    public AudioSource[] backingTracks; // Diğer kanallar (Davul, Bass, Gitar vs.)

    [Header("Hata Efektleri")]
    public AudioSource sfxSource;       // Hata sesi için kaynak
    public AudioClip errorClip;         // "Dıt" sesi

    [Header("Fade / Ceza Ayarları")]
    public float fadeSpeed = 10f;       // Sesin açılma/kısılma hızı
    [Tooltip("Her hatada ceza seviyesinin ne kadar artacağı (0–1 arası mantık).")]
    public float errorIncrease = 0.35f;
    [Tooltip("Ceza seviyesinin saniyede ne kadar azalacağı.")]
    public float errorDecay = 0.5f;

    [Tooltip("Organ kanalının minimum sesi (ceza maksimumdayken).")]
    public float minVolume = 0f;
    [Tooltip("Organ kanalının maksimum sesi (ceza yokken).")]
    public float maxVolume = 1f;

    [Header("Müzik Bozulma Ayarları")]
    [Tooltip("Normal pitch değeri (genelde 1.0)")]
    public float normalPitch = 1f;
    [Tooltip("Hata anında pitch ne kadar bozulsun (örn: 0.85 = %15 yavaşlama)")]
    public float errorPitch = 0.85f;
    [Tooltip("Pitch düzeltme hızı")]
    public float pitchRecoverySpeed = 3f;

    // 0 => ceza yok, 1 => full ceza (tam mute)
    private float errorLevel = 0f;
    private float targetVolume = 1f;
    private float currentPitch = 1f;
    
    // Müziğin başlayıp başlamadığını takip et
    private bool musicStarted = false;

    void Start()
    {
        // Tüm AudioSource'ları hazırla ama BAŞLATMA
        if (organSource != null)
        {
            organSource.volume = maxVolume;
            organSource.pitch = normalPitch;
            organSource.Stop();
        }

        foreach (var source in backingTracks)
        {
            if (source != null)
            {
                source.pitch = normalPitch;
                source.Stop();
            }
        }

        targetVolume = maxVolume;
        currentPitch = normalPitch;
        musicStarted = false;
    }

    void Update()
    {
        // Ceza zamanla azalıyor (oyuncu toparladıkça ses geri gelir)
        errorLevel = Mathf.Max(0f, errorLevel - errorDecay * Time.deltaTime);

        // Volume fade
        float penalty = Mathf.Clamp01(errorLevel);
        float desiredVolume = Mathf.Lerp(maxVolume, minVolume, penalty);
        targetVolume = desiredVolume;

        if (organSource != null && musicStarted)
        {
            // Volume fade
            organSource.volume = Mathf.Lerp(
                organSource.volume,
                targetVolume,
                Time.deltaTime * fadeSpeed
            );

            // Pitch düzeltme (yavaşça normale dön)
            currentPitch = Mathf.Lerp(
                currentPitch,
                normalPitch,
                Time.deltaTime * pitchRecoverySpeed
            );
            organSource.pitch = currentPitch;
        }
    }

    /// <summary>
    /// İlk etkileşimde müziği başlat (senkron şekilde)
    /// </summary>
    private void StartMusicIfNeeded()
    {
        if (musicStarted) return;
        
        musicStarted = true;
        
        // Tüm kanalları aynı anda başlat
        if (organSource != null)
        {
            organSource.Play();
        }

        foreach (var source in backingTracks)
        {
            if (source != null)
            {
                source.Play();
            }
        }

        Debug.Log("🎵 Müzik başlatıldı! (İlk etkileşim)");
    }

    /// <summary>
    /// Hata (yanlış tuş, miss, erken basış) olduğunda çağır.
    /// SADECE ORGAN kanalında bozulma yaratır!
    /// </summary>
    public void RegisterError()
    {
        // İlk etkileşim - müziği başlat
        StartMusicIfNeeded();
        
        errorLevel = Mathf.Clamp01(errorLevel + errorIncrease);

        // Pitch'i anında boz (sadece organ!)
        if (organSource != null)
        {
            currentPitch = errorPitch;
            organSource.pitch = errorPitch;
        }

        // Hata sesi çal
        if (sfxSource != null && errorClip != null)
        {
            sfxSource.PlayOneShot(errorClip, 0.7f);
        }
    }

    /// <summary>
    /// Başarılı vuruşlarda (Perfect/Good/Ok) çağır.
    /// Ceza seviyesini azaltır ve pitch'i hızla düzeltir.
    /// </summary>
    public void RegisterGoodHit()
    {
        // İlk etkileşim - müziği başlat
        StartMusicIfNeeded();
        
        // Cezayı azalt
        errorLevel = Mathf.Max(0f, errorLevel - errorIncrease * 0.5f);

        // Pitch'i hızla düzelt (başarılı vuruşta anında normale yaklaştır)
        if (organSource != null)
        {
            currentPitch = Mathf.Lerp(currentPitch, normalPitch, 0.6f);
            organSource.pitch = currentPitch;
        }
    }

    /// <summary>
    /// Oyun resetlendiğinde çağır (tam temizle).
    /// </summary>
    public void ResetAudio()
    {
        errorLevel = 0f;
        targetVolume = maxVolume;
        currentPitch = normalPitch;
        
        // Müziği durdur ve resetle
        musicStarted = false;
        
        if (organSource != null)
        {
            organSource.Stop();
            organSource.volume = maxVolume;
            organSource.pitch = normalPitch;
        }
        
        foreach (var source in backingTracks)
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }

    // Eski API ile uyum için
    public void MuteOrgan()
    {
        errorLevel = 1f;
    }

    public void UnmuteOrgan()
    {
        errorLevel = 0f;
    }

    public void PlayErrorSound()
    {
        if (sfxSource != null && errorClip != null)
        {
            sfxSource.PlayOneShot(errorClip, 0.7f);
        }
    }
}