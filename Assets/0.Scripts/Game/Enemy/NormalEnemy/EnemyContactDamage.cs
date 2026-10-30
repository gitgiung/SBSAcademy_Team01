using UnityEngine;

/// <summary>Applies throttled contact damage while the player remains in the enemy's trigger.</summary>
[DisallowMultipleComponent]
public sealed class EnemyContactDamage : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0.01f)] private float damageInterval = 0.5f;

    private float nextDamageTime;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextDamageTime) return;
        var receiver = other.GetComponentInParent(typeof(IContactDamageReceiver)) as IContactDamageReceiver;
        if (receiver == null) return;

        nextDamageTime = Time.time + Mathf.Max(0.01f, damageInterval);
        receiver.TakeContactDamage(damage, gameObject);
    }
}
