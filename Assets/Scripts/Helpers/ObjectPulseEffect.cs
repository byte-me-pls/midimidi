using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectPulseEffect : MonoBehaviour
{
    public static ObjectPulseEffect Instance;

    [Header("Efekt Ayarları")]
    public float scaleMultiplier = 1.2f;
    public float fadeSpeed = 10f;

    [Header("Patlama Ayarları")]
    public GameObject explosionVFXPrefab;
    public AudioClip explosionSound;

    [Header("Patlama Fiziği")]
    public float explosionRadius = 5f;
    public float explosionForce = 2500f;
    public float explosionUpwardModifier = 2f;
    public LayerMask charactersLayer;

    [Header("Ragdoll Temizlik")]
    [Tooltip("Patlama sonrası ragdoll kaç saniye sonra yok olsun")]
    public float ragdollDestroyDelay = 5f;

    [Header("Yavaş Çekim (Opsiyonel)")]
    public bool useSlowMotion = true;
    public float slowMotionScale = 0.3f;
    public float slowMotionDuration = 0.5f;

    [Header("Kamera Shake (Opsiyonel)")]
    public bool useCameraShake = true;
    public float shakeIntensity = 0.5f;
    public float shakeDuration = 0.3f;

    private Vector3 baseScale;
    private float currentIntensity = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        baseScale = transform.localScale;
    }

    public void TriggerPulse(float intensityMultiplier = 1.0f)
    {
        currentIntensity = intensityMultiplier;
    }

    public void Explode()
    {
        Debug.Log($"[Explosion] Patlama başladı. Pozisyon: {transform.position}");

        // 1) VFX ve Ses
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f);

        // 2) Kamera shake
        if (useCameraShake)
            StartCoroutine(CameraShakeEffect());

        // 3) Patlama alanındaki collider'ları bul
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            charactersLayer
        );

        Debug.Log($"[Explosion] {colliders.Length} collider yakalandı.");

        // Aynı RagdollManager'a 50 kez gitmemek için
        HashSet<RagdollManager> affectedRagdolls = new HashSet<RagdollManager>();

        foreach (Collider nearbyObject in colliders)
        {
            // Karakterin root'unda veya herhangi bir child'ında RagdollManager ara
            RagdollManager ragdoll = nearbyObject.GetComponentInParent<RagdollManager>();

            if (ragdoll != null && !affectedRagdolls.Contains(ragdoll))
            {
                affectedRagdolls.Add(ragdoll);
                Debug.Log($"[Explosion] Ragdoll vuruldu: {ragdoll.name}");

                // Patlama kuvvetini uygula
                ragdoll.ApplyExplosionForceToRagdoll(
                    explosionForce,
                    transform.position,
                    explosionRadius,
                    explosionUpwardModifier
                );

                // 5 saniye sonra ragdoll'u yok et
                StartCoroutine(DestroyRagdollAfterDelay(ragdoll.gameObject, ragdollDestroyDelay));
            }
        }

        if (affectedRagdolls.Count == 0)
        {
            Debug.LogWarning("[Explosion] Hiçbir ragdoll bulunamadı! Layer mask ve karakter pozisyonlarını kontrol edin.");
        }

        // 4) Yakındaki diğer fizik objelerine de kuvvet uygula
        Rigidbody[] nearbyRigidbodies = FindObjectsOfType<Rigidbody>();
        foreach (var rb in nearbyRigidbodies)
        {
            if (rb == null) continue;
            
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

        // 5) Kendini yok et veya kapat
        Destroy(gameObject, 0.1f);
    }

    /// <summary>
    /// Ragdoll objesini belirtilen süre sonra yok eder
    /// </summary>
    IEnumerator DestroyRagdollAfterDelay(GameObject ragdollObject, float delay)
    {
        if (ragdollObject == null) yield break;

        Debug.Log($"[Ragdoll] {ragdollObject.name} {delay} saniye sonra yok edilecek.");
        
        yield return new WaitForSeconds(delay);

        if (ragdollObject != null)
        {
            Debug.Log($"[Ragdoll] {ragdollObject.name} yok ediliyor.");
            Destroy(ragdollObject);
        }
    }

    IEnumerator CameraShakeEffect()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        Vector3 originalPos = mainCam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;

            mainCam.transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        mainCam.transform.localPosition = originalPos;
    }

    void Update()
    {
        // Eski scale efekti
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
        // Patlama yarıçapını göster
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Etkilenecek karakterleri göster
        Gizmos.color = Color.yellow;
        Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius, charactersLayer);
        foreach (var col in cols)
        {
            Gizmos.DrawLine(transform.position, col.transform.position);
        }
    }
}