using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손/무기 부위 콜라이더. 활성 구간에만 켜지며 접촉 피해를 1회만 준다.
/// 바닥 범위 Overlap이 아니라 부위 트리거가 판정의 본체다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MotionBossWeaponHitbox : MonoBehaviour
{
    private readonly HashSet<int> hitInstanceIds = new HashSet<int>();
    private float damage;
    private GameObject source;
    private Collider2D hitCollider;
    private bool armed;

    public bool IsArmed => armed;

    private void Awake()
    {
        hitCollider = GetComponent<Collider2D>();
        if (hitCollider == null)
            hitCollider = gameObject.AddComponent<CircleCollider2D>();
        hitCollider.isTrigger = true;
        hitCollider.enabled = false;
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
        if (value) hitInstanceIds.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed || other == null) return;
        int id = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject.GetInstanceID()
            : other.gameObject.GetInstanceID();
        if (!hitInstanceIds.Add(id)) return;

        var receiver = other.GetComponentInParent<IContactDamageReceiver>();
        if (receiver != null)
            receiver.TakeContactDamage(damage, source);
    }
}
