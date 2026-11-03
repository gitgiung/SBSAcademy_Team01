using UnityEngine;

/// <summary>Player gameplay state and tunable base stats (the MVC model).</summary>
public class PlayerData : MonoBehaviour
{
    [SerializeField] private PlayerLoadout loadout = new PlayerLoadout();
    public PlayerLoadout Loadout => loadout ?? (loadout = new PlayerLoadout());
    [Header("플레이어 기본 설정")]
    public float speed = 5f;
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("공격 설정")]
    public float attackRange = 5f;
    public float attackCooldown = 1f;
    public float attackDamage = 10f;
    public float bulletSpeed = 10f;

    [Header("MP 설정 (수치 미정, 테스트 시 지정)")]
    [Min(0f)] public float maxMP;
    [SerializeField] private float currentMP;
    [Min(0f)] public float mpRecoveryPerSecond;
    [Min(0f)] public float dashMPCost;
    [Min(0f)] public float parryMPRecovery;

    public float CurrentMP => currentMP;
    public event System.Action<float, float> MPChanged;
    public event System.Action<float> MPSpendRejected;

    [Header("EXP 표시 상태 (요구량/레벨업 규칙 미정)")]
    [Min(0f)] public float maxEXP;
    [SerializeField] private float currentEXP;
    public float CurrentEXP => currentEXP;
    public event System.Action<float, float> EXPChanged;

    [Header("EXP 획득 범위 설정")]
    [Min(0f)] public float experiencePickupDistance;

    public bool IsDead => currentHealth <= 0f;
    public event System.Action<float, float> HealthChanged;
    public event System.Action PlayerDeath;

    private void Awake()
    {
        InitHealth();
        InitMP();
        InitEXP();
    }

    [ContextMenu("EXP/현재 목표로 초기화")]
    public void InitEXP()
    {
        if (!IsValidAmount(maxEXP)) maxEXP = 0f;
        SetEXP(0f, maxEXP);
    }

    /// <summary>성장 담당이 계산한 현재 EXP/목표를 받습니다. 레벨업과 초과분 처리는 하지 않습니다.</summary>
    public bool SetEXP(float current, float maximum)
    {
        if (!IsValidAmount(current) || !IsValidAmount(maximum)) return false;
        currentEXP = current;
        maxEXP = maximum;
        EXPChanged?.Invoke(currentEXP, maxEXP);
        return true;
    }

    /// <summary>줍기 등에서 EXP를 더합니다. 레벨업 요구량을 넘는 EXP도 보존합니다.</summary>
    public bool AddEXP(float amount)
    {
        if (IsDead || !IsValidAmount(amount) || amount <= 0f ||
            !IsValidAmount(currentEXP) || !IsValidAmount(maxEXP))
            return false;

        float updatedEXP = currentEXP + amount;
        return IsValidAmount(updatedEXP) && SetEXP(updatedEXP, maxEXP);
    }

    [ContextMenu("MP/현재 설정으로 초기화")]
    public void InitMP()
    {
        if (!IsValidAmount(maxMP)) maxMP = 0f;
        currentMP = maxMP;
        MPChanged?.Invoke(currentMP, maxMP);
    }

    public bool TrySpendMP(float amount)
    {
        if (IsDead || !IsValidAmount(amount)) return false;
        if (amount > currentMP)
        {
            MPSpendRejected?.Invoke(amount);
            return false;
        }
        if (amount == 0f) return true;
        currentMP -= amount;
        MPChanged?.Invoke(currentMP, maxMP);
        return true;
    }

    public float RestoreMP(float amount)
    {
        if (IsDead || !IsValidAmount(amount) || !IsValidAmount(maxMP)) return 0f;
        float restored = Mathf.Min(amount, Mathf.Max(0f, maxMP - currentMP));
        if (restored <= 0f) return 0f;
        currentMP += restored;
        MPChanged?.Invoke(currentMP, maxMP);
        return restored;
    }

    public void TickMPRecovery(float deltaTime)
    {
        if (!IsValidAmount(deltaTime) || deltaTime <= 0f || !IsValidAmount(mpRecoveryPerSecond)) return;
        RestoreMP(mpRecoveryPerSecond * deltaTime);
    }

    private static bool IsValidAmount(float value) =>
        value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

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
