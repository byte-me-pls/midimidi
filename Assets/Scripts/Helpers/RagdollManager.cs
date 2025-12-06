using UnityEngine;

public class RagdollManager : MonoBehaviour
{
    [Header("Referanslar")]
    public Animator animator;

    [Header("Root Bileşenleri")]
    public Rigidbody rootRigidbody;
    public Collider rootCollider;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    [Header("Durum")]
    public bool isRagdollActive = false;

    [Header("Ragdoll Ayarları")]
    public float ragdollMass = 80f; // Toplam kütle
    public float ragdollDrag = 2f;  // Hava direnci (yavaşlatır)
    public float ragdollAngularDrag = 2f;  // Dönme direnci
    public float jointSpring = 0f;
    public float jointDamper = 0f;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rootRigidbody == null) rootRigidbody = GetComponent<Rigidbody>();
        if (rootCollider == null) rootCollider = GetComponent<Collider>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        DeactivateRagdoll();
    }

    public void ActivateRagdoll()
    {
        isRagdollActive = true;

        // Animasyonu kapat
        if (animator != null)
            animator.enabled = false;

        // Root collider'ı kapat ki ragdoll'la çakışmasın
        if (rootCollider != null)
            rootCollider.enabled = false;

        // Root rigidbody'yi kinematic yap (hareket etmesin)
        if (rootRigidbody != null)
        {
            rootRigidbody.isKinematic = true;
            rootRigidbody.useGravity = false;
        }

        // Kütleyi dağıt
        int boneCount = 0;
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null && rb.gameObject != this.gameObject)
                boneCount++;
        }

        float massPerBone = boneCount > 0 ? ragdollMass / boneCount : 1f;

        // Kemiklerde fiziği aç
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            if (rb.gameObject == this.gameObject) continue; // root'u atla

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = massPerBone;
            rb.linearDamping = ragdollDrag;  // Hava direnci ekle
            rb.angularDamping = ragdollAngularDrag;  // Dönme direnci ekle
            
            // Interpolation ekle (daha smooth hareket)
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        foreach (var col in ragdollColliders)
        {
            if (col == null) continue;
            if (col.gameObject == this.gameObject) continue;

            col.isTrigger = false;
            col.enabled = true;
        }

        // Character Joint ayarları (eğer varsa)
        CharacterJoint[] joints = GetComponentsInChildren<CharacterJoint>();
        foreach (var joint in joints)
        {
            if (joint != null)
            {
                joint.enableProjection = true;
                
                SoftJointLimit limit = joint.lowTwistLimit;
                limit.bounciness = 0f;
                joint.lowTwistLimit = limit;
                
                limit = joint.highTwistLimit;
                limit.bounciness = 0f;
                joint.highTwistLimit = limit;
                
                limit = joint.swing1Limit;
                limit.bounciness = 0f;
                joint.swing1Limit = limit;
                
                limit = joint.swing2Limit;
                limit.bounciness = 0f;
                joint.swing2Limit = limit;
            }
        }
    }

    public void DeactivateRagdoll()
    {
        isRagdollActive = false;

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            if (rb.gameObject == this.gameObject) continue;

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var col in ragdollColliders)
        {
            if (col == null) continue;
            if (col.gameObject == this.gameObject) continue;

            col.isTrigger = true;
        }

        if (rootCollider != null)
        {
            rootCollider.enabled = true;
            rootCollider.isTrigger = false;
        }

        if (rootRigidbody != null)
        {
            rootRigidbody.isKinematic = false;
            rootRigidbody.useGravity = true;
        }

        if (animator != null)
            animator.enabled = true;
    }

    /// <summary>
    /// Patlama kuvvetini tüm ragdoll kemiklerine uygular.
    /// </summary>
    public void ApplyExplosionForceToRagdoll(
        float force,
        Vector3 position,
        float radius,
        float upwardModifier = 2f)
    {
        // İlk önce ragdoll moduna geç
        if (!isRagdollActive)
            ActivateRagdoll();

        // Patlamayı biraz aşağıdan kabul edelim ki yukarı doğru uçursun
        Vector3 explosionPos = position - Vector3.up * 0.5f;

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            if (rb.gameObject == this.gameObject) continue; // root

            // Mesafe hesapla
            float distance = Vector3.Distance(rb.position, position);
            if (distance > radius) continue;

            // Mesafeye göre güç azalt
            float normalizedDistance = 1f - (distance / radius);
            float adjustedForce = force * normalizedDistance;

            rb.AddExplosionForce(
                adjustedForce,
                explosionPos,
                radius,
                upwardModifier,
                ForceMode.Impulse
            );
        }
    }

    /// <summary>
    /// Belirli bir yöne doğru güç uygular
    /// </summary>
    public void ApplyForceToRagdoll(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        if (!isRagdollActive)
            ActivateRagdoll();

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            if (rb.gameObject == this.gameObject) continue;

            rb.AddForce(force, mode);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!isRagdollActive) return;

        // Aktif ragdoll kemiklerini göster
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        Gizmos.color = Color.green;
        foreach (var rb in rbs)
        {
            if (rb != null && rb.gameObject != gameObject && !rb.isKinematic)
            {
                Gizmos.DrawWireSphere(rb.position, 0.05f);
            }
        }
    }
}