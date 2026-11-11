using UnityEngine;

/// <summary>SoulArena 보스/적 임시 체력. Main EnemyHealth와 분리.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaDummyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 200f;
    private float health;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsDead => health <= 0f;

    private void Awake() => health = maxHealth;

    public void Configure(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        health = Mathf.Max(0f, health - damage);
        var fx = GetComponent<SoulHitFeedback>();
        if (fx == null) fx = gameObject.AddComponent<SoulHitFeedback>();
        fx.Play();
        ApplyHitKnockback();
        SoulCameraShake.Punch(0.28f);
        Debug.Log($"[SoulPipeline] {name} 피격 {damage:0.#} → HP {health:0.#}/{maxHealth:0.#}", this);
        if (IsDead)
        {
            Debug.Log($"[SoulPipeline] {name} 처치", this);
            SoulArenaKillFeed.Push($"처치: {name}");
            var bossAi = GetComponent<SoulBossApproachSlash>();
            if (bossAi != null) bossAi.enabled = false;
            var hit = GetComponentInChildren<SoulWeaponHitbox3D>(true);
            hit?.SetArmed(false);
        }
    }

    private void ApplyHitKnockback()
    {
        var avatar = FindAnyObjectByType<SoulPipelineAvatar>();
        if (avatar == null) return;
        Vector3 dir = transform.position - avatar.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();
        var cc = GetComponent<CharacterController>();
        if (cc != null && cc.enabled)
            cc.Move(dir * 0.55f);
        else
            transform.position += dir * 0.55f;
    }
}
