using UnityEngine;

public class RagdollManager : MonoBehaviour
{
    [Header("Referanslar")]
    public Animator animator;

    [Header("Ragdoll Ayarları")]
    public float ragdollMass = 80f;
    public float ragdollDrag = 0.5f;
    public float ragdollAngularDrag = 0.5f;

    [Header("Collision Ayarları")]
    public float minCollisionMass = 0.1f;
    public PhysicsMaterial ragdollPhysicsMaterial;

    [Header("Durum")]
    public bool isRagdollActive = false;

    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private MonoBehaviour[] allBehaviours;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders   = GetComponentsInChildren<Collider>();
        allBehaviours      = GetComponentsInChildren<MonoBehaviour>(true);

        DeactivateRagdoll();
    }

    public void ActivateRagdoll()
    {
        if (isRagdollActive) return;
        isRagdollActive = true;

        // Animator, RigBuilder, IK vs. ne varsa sustur (seni göğüsten zımbalayanlar bunlar)
        DisableOtherBehaviours();

        if (animator != null)
            animator.enabled = false;

        // Kütle dağılımı
        int boneCount = 0;
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb != null) boneCount++;
        }

        float massPerBone = boneCount > 0 ? ragdollMass / boneCount : 1f;
        massPerBone = Mathf.Max(massPerBone, minCollisionMass);

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;

            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.mass        = massPerBone;

            rb.linearDamping  = ragdollDrag;
            rb.angularDamping = ragdollAngularDrag;

            rb.interpolation          = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints            = RigidbodyConstraints.None;
            rb.maxAngularVelocity     = 50f;
        }

        foreach (var col in ragdollColliders)
        {
            if (col == null) continue;

            col.enabled   = true;
            col.isTrigger = false;

            if (ragdollPhysicsMaterial != null && col is CapsuleCollider)
                col.material = ragdollPhysicsMaterial;
        }

        ConfigureJoints();
    }

    private void DisableOtherBehaviours()
    {
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            if (mb == this) continue;          // RagdollManager açık kalsın
            if (!mb.enabled) continue;        // Zaten kapalıysa dokunma

            mb.enabled = false;
        }
    }

    public void DeactivateRagdoll()
    {
        isRagdollActive = false;

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;

            rb.isKinematic     = true;
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var col in ragdollColliders)
        {
            if (col == null) continue;
            col.isTrigger = true;
        }

        if (animator != null)
            animator.enabled = true;
    }

    private void ConfigureJoints()
    {
        CharacterJoint[] joints = GetComponentsInChildren<CharacterJoint>();

        foreach (var joint in joints)
        {
            if (joint == null) continue;

            joint.enableProjection  = true;
            joint.projectionDistance = 0.1f;
            joint.projectionAngle    = 180f;

            joint.breakForce  = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;

            SoftJointLimitSpring spring = new SoftJointLimitSpring
            {
                spring = 0f,
                damper = 0f
            };

            joint.twistLimitSpring = spring;
            joint.swingLimitSpring = spring;

            SoftJointLimit limit;

            limit = joint.lowTwistLimit;
            limit.bounciness      = 0f;
            limit.contactDistance  = 0f;
            joint.lowTwistLimit   = limit;

            limit = joint.highTwistLimit;
            limit.bounciness      = 0f;
            limit.contactDistance  = 0f;
            joint.highTwistLimit  = limit;

            limit = joint.swing1Limit;
            limit.bounciness      = 0f;
            limit.contactDistance  = 0f;
            joint.swing1Limit     = limit;

            limit = joint.swing2Limit;
            limit.bounciness      = 0f;
            limit.contactDistance  = 0f;
            joint.swing2Limit     = limit;

            joint.enablePreprocessing = true;
        }
    }

    public void ApplyExplosionForceToRagdoll(
        float force,
        Vector3 position,
        float radius,
        float upwardModifier = 2f)
    {
        if (!isRagdollActive)
            ActivateRagdoll();

        // Patlama merkezini biraz aşağıdan uygula
        Vector3 explosionPos = position - Vector3.up * 0.3f;

        // En uzaktaki kemik bile minimum güç alsın
        const float minForceFactor = 0.25f; // %25 minimum

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;

            float distance = Vector3.Distance(rb.position, position);
            if (distance > radius) continue;

            float normalizedDistance = 1f - (distance / radius);
            normalizedDistance = Mathf.Clamp01(normalizedDistance);

            float forceFactor  = Mathf.Lerp(minForceFactor, 1f, normalizedDistance);
            float adjustedForce = force * forceFactor;

            rb.AddExplosionForce(
                adjustedForce,
                explosionPos,
                radius,
                upwardModifier,
                ForceMode.Impulse
            );
        }

        // Gövdeye ekstra ufak tekme (daha hissedilir reaksiyon için)
        Rigidbody hips = FindHipsRigidbody();
        if (hips != null)
        {
            hips.AddExplosionForce(
                force * 0.3f,
                explosionPos,
                radius,
                upwardModifier,
                ForceMode.Impulse
            );
        }
    }

    private Rigidbody FindHipsRigidbody()
    {
        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            if (rb.name.Contains("Hips") || rb.name.Contains("Pelvis"))
                return rb;
        }
        return null;
    }

    public void ApplyForceToRagdoll(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        if (!isRagdollActive)
            ActivateRagdoll();

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == null) continue;
            rb.AddForce(force, mode);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!isRagdollActive) return;

        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        Gizmos.color = Color.green;

        foreach (var rb in rbs)
        {
            if (rb != null && !rb.isKinematic)
                Gizmos.DrawWireSphere(rb.position, 0.05f);
        }
    }
}
