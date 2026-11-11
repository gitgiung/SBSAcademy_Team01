using System.Collections.Generic;
using UnityEngine;

/// <summary>손/검 부위 3D 트리거. 활성 구간에만 피해. 바닥 Overlap 대체.</summary>
[DisallowMultipleComponent]
public sealed class SoulWeaponHitbox3D : MonoBehaviour
{
    private readonly HashSet<EntityId> hitIds = new HashSet<EntityId>();
    private Collider hitCollider;
    private float damage = 18f;
    private GameObject source;
    private bool armed;

    public bool IsArmed => armed;

    private void Awake()
    {
        hitCollider = GetComponent<Collider>();
        if (hitCollider == null)
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.22f;
            hitCollider = sphere;
        }

        hitCollider.isTrigger = true;
        hitCollider.enabled = false;

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (GetComponent<SoulWeaponArmedPulse>() == null)
            gameObject.AddComponent<SoulWeaponArmedPulse>();
    }

    public void Configure(float contactDamage, GameObject damageSource)
    {
        damage = contactDamage;
        source = damageSource != null ? damageSource : gameObject;
    }

    public void SetArmed(bool value)
    {
        if (armed == value) return;
        armed = value;
        if (hitCollider != null) hitCollider.enabled = value;
        if (value) hitIds.Clear();
    }

    private void OnTriggerEnter(Collider other) => TryHitCollider(other);

    /// <summary>트리거 누락 시 CharacterController 등을 Overlap으로 보조 타격.</summary>
    public void TryHitCollider(Collider other)
    {
        if (!armed || other == null) return;
        EntityId id = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject.GetEntityId()
            : other.gameObject.GetEntityId();
        if (!hitIds.Add(id)) return;

        var receiver = other.GetComponentInParent<IContactDamageReceiver>();
        if (receiver != null)
            receiver.TakeContactDamage(damage, source);
    }
}
