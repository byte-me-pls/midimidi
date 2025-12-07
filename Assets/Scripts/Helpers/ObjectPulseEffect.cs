using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPulseEffect : MonoBehaviour
{
    public static ObjectPulseEffect Instance;

    [Header("Efekt Ayarları")]
    public float scaleMultiplier = 1.2f;
    public float fadeSpeed = 10f;

    [Header("Patlama Görseli")]
    public GameObject explosionVFXPrefab;
    
    [Header("Ses Ayarları (Rhythm Yerine)")]
    [Tooltip("Genel patlama sesi")]
    public AudioClip explosionSound;
    [Tooltip("Ragdoll vurulunca çıkacak özel ses (Opsiyonel)")]
    public AudioClip ragdollHitSound;
    [Tooltip("Sesi çalacak Kaynak (MainCamera veya boş bir obje verin, çünkü bu obje yok oluyor!)")]
    public AudioSource targetAudioSource; 

    [Header("Patlama Fiziği")]
    public float explosionRadius = 5f;
    public float explosionForce = 2500f;
    public float explosionUpwardModifier = 2f;
    public LayerMask charactersLayer;

    [Header("Ragdoll Temizlik")]
    public float ragdollDestroyDelay = 5f;

    [Header("Kamera Shake")]
    public bool useCameraShake = true;
    public float shakeIntensity = 0.8f;
    public float shakeDuration = 0.3f;

    private Vector3 baseScale;
    private float currentIntensity = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        baseScale = transform.localScale;
        
        // Eğer AudioSource atanmadıysa otomatik MainCamera'dakini bulmaya çalış
        if (targetAudioSource == null && Camera.main != null)
        {
            targetAudioSource = Camera.main.GetComponent<AudioSource>();
        }
    }

    public void TriggerPulse(float intensityMultiplier = 1.0f)
    {
        currentIntensity = intensityMultiplier;
    }

    public void Explode()
    {
        Debug.Log($"[Explosion] Patlama başladı. Pozisyon: {transform.position}");

        // 1) VFX
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        // 2) SES (RhythmController yerine direkt ses)
        // Genel patlama sesi (Objeden bağımsız çalar)
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);
        }

        // 3) Kamera shake (Düzeltilmiş versiyon)
        if (useCameraShake)
        {
            StopCoroutine("CameraShakeEffect");
            StartCoroutine(CameraShakeEffect());
        }

        // 4) Ragdoll Etkileşimi
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, charactersLayer);
        HashSet<RagdollManager> affectedRagdolls = new HashSet<RagdollManager>();

        foreach (Collider nearbyObject in colliders)
        {
            RagdollManager ragdoll = nearbyObject.GetComponentInParent<RagdollManager>();

            if (ragdoll != null && !affectedRagdolls.Contains(ragdoll))
            {
                affectedRagdolls.Add(ragdoll);
                
                ragdoll.ApplyExplosionForceToRagdoll(
                    explosionForce, 
                    transform.position, 
                    explosionRadius, 
                    explosionUpwardModifier
                );

                StartCoroutine(DestroyRagdollAfterDelay(ragdoll.gameObject, ragdollDestroyDelay));
            }
        }

        // --- DEĞİŞİKLİK BURADA ---
        // Eğer ragdoll'lara vurduysak Rhythm yerine direkt ses çalıyoruz
        if (affectedRagdolls.Count > 0)
        {
            Debug.Log("[Explosion] Ragdoll'lar etkilendi, vuruş sesi çalınıyor.");
            
            if (targetAudioSource != null && ragdollHitSound != null)
            {
                targetAudioSource.PlayOneShot(ragdollHitSound);
            }
        }
        else
        {
            Debug.LogWarning("[Explosion] Hiçbir ragdoll bulunamadı!");
        }

        // 5) Çevre Fiziği
        Rigidbody[] nearbyRigidbodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        foreach (var rb in nearbyRigidbodies)
        {
            if (rb == null) continue;
            if (rb.GetComponentInParent<RagdollManager>() != null) continue;

            float distance = Vector3.Distance(rb.position, transform.position);
            if (distance <= explosionRadius)
            {
                rb.AddExplosionForce(
                    explosionForce, 
                    transform.position, 
                    explosionRadius, 
                    explosionUpwardModifier, 
                    ForceMode.Impulse
                );
            }
        }

        // Objenin kendini yok etmesi (Seslerin kesilmemesi için PlayClipAtPoint veya harici source kullandık)
        Destroy(gameObject, 0.2f);
    }

    // --- KAMERA TİTREME (Düzeltilmiş - Additive Shake) ---
    IEnumerator CameraShakeEffect()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            // Rastgele ofset üret
            Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
            
            // Kameraya uygula
            mainCam.transform.position += shakeOffset;
            
            yield return null; 

            // Bir sonraki karede geri al (böylece kamera takibi bozulmaz)
            if (mainCam != null)
                mainCam.transform.position -= shakeOffset;

            elapsed += Time.deltaTime;
        }
    }

    IEnumerator DestroyRagdollAfterDelay(GameObject ragdollObject, float delay)
    {
        if (ragdollObject == null) yield break;
        yield return new WaitForSeconds(delay);
        if (ragdollObject != null) Destroy(ragdollObject);
    }

    void Update()
    {
        currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * fadeSpeed);
        float s = 1f + (currentIntensity * (scaleMultiplier - 1f));
        transform.localScale = baseScale * s;

        if (currentIntensity < 0.001f)
        {
            transform.localScale = baseScale;
            currentIntensity = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}