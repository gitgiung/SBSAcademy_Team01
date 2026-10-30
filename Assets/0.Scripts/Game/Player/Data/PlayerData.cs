using UnityEngine;

/// <summary>Player gameplay state and tunable base stats (the MVC model).</summary>
public class PlayerData : MonoBehaviour
{
    [Header("플레이어 기본 설정")]
    public float speed = 5f;
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("공격 설정")]
    public float attackRange = 5f;
    public float attackCooldown = 1f;
    public float attackDamage = 10f;
    public float bulletSpeed = 10f;

    public bool IsDead => currentHealth <= 0f;
    public event System.Action<float, float> HealthChanged;
    public event System.Action PlayerDeath;

    private void Awake()
    {
        InitHealth();
    }

    public void InitHealth()
    {
        if (maxHealth <= 0f) maxHealth = 100f;
        currentHealth = maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Applies validated damage and publishes the updated health state.</summary>
    public bool ApplyDamage(float damage)
    {
        if (damage <= 0f || IsDead) return false;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (IsDead)
            PlayerDeath?.Invoke();
        return true;
    }
}
