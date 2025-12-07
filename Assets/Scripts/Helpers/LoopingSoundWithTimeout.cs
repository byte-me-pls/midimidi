using UnityEngine;

public class LoopingSoundWithTimeout : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip soundClip;
    [SerializeField] private float soundDuration = 30f;

    private void Start()
    {
        // AudioSource yoksa ekle
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // AudioSource ayarları
        audioSource.clip = soundClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        // Hemen sesi başlat
        if (soundClip != null)
        {
            audioSource.Play();
            // 30 saniye sonra durdur
            Invoke(nameof(StopSound), soundDuration);
        }
        else
        {
            Debug.LogWarning("AudioClip atanmamış!");
        }
    }

    private void StopSound()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}