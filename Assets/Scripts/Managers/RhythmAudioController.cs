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

    // 0 => ceza yok, 1 => full ceza (tam mute)
    private float errorLevel = 0f;
    private float targetVolume = 1f;

    void Start()
    {
        // Tüm kanalları aynı anda başlat (senkron için)
        if (organSource != null)
        {
            organSource.volume = maxVolume;
            organSource.Play();
        }

        foreach (var source in backingTracks)
        {
            if (source != null)
            {
                source.Play();
            }
        }

        targetVolume = maxVolume;
    }

    void Update()
    {
        // Ceza zamanla azalıyor (oyuncu toparladıkça ses geri gelir)
        errorLevel = Mathf.Max(0f, errorLevel - errorDecay * Time.deltaTime);

        // 0 => full volume, 1 => minVolume
        float penalty = Mathf.Clamp01(errorLevel);
        float desiredVolume = Mathf.Lerp(maxVolume, minVolume, penalty);
        targetVolume = desiredVolume;

        if (organSource != null)
        {
            organSource.volume = Mathf.Lerp(
                organSource.volume,
                targetVolume,
                Time.deltaTime * fadeSpeed
            );
        }
    }

    /// <summary>
    /// Hata (yanlış tuş, miss, erken basış) olduğunda çağır.
    /// </summary>
    public void RegisterError()
    {
        errorLevel = Mathf.Clamp01(errorLevel + errorIncrease);

        if (sfxSource != null && errorClip != null)
        {
            sfxSource.PlayOneShot(errorClip, 0.7f);
        }
    }

    /// <summary>
    /// Başarılı vuruşlarda (Perfect/Good/Ok) çağır.
    /// Ceza seviyesini biraz azaltır.
    /// </summary>
    public void RegisterGoodHit()
    {
        errorLevel = Mathf.Max(0f, errorLevel - errorIncrease * 0.5f);
    }

    /// <summary>
    /// Oyun resetlendiğinde çağır (tam temizle).
    /// </summary>
    public void ResetAudio()
    {
        errorLevel = 0f;
        targetVolume = maxVolume;

        if (organSource != null)
            organSource.volume = maxVolume;
    }

    // Eski API ile uyum için (kullanmasan da olur ama zarar vermez)
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
