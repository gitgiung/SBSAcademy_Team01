using UnityEngine;

/// <summary>Player gameplay state and tunable base stats (the MVC model).</summary>
public class PlayerData : MonoBehaviour
{
    [SerializeField] private PlayerLoadout loadout = new PlayerLoadout();
    public PlayerLoadout Loadout => loadout ?? (loadout = new PlayerLoadout());
    public PlayerStatProgress StatCards { get; } = new PlayerStatProgress();
    private float maxHealthBeforeStatCards;
    public float MoveSpeed => speed * (1f + StatCards.Value(PlayerStat.Move_Speed));
    public float MPRecovery => StatCards.Value(PlayerStat.MP_Regen, mpRecoveryPerSecond);
    public float PickupDistance => experiencePickupDistance * (1f + StatCards.Value(PlayerStat.Pickup_Radius));
    public float AttackDamage => attackDamage + StatCards.Increase(PlayerStat.DMG);
    // 기존 공개 API 호환용입니다. DMG 카드는 이제 공격력을 더하며 최종 피해를 증폭하지 않습니다.
    public float DamageMultiplier => 1f;
    public float PassiveCooldownMultiplier => 1f - StatCards.Value(PlayerStat.Cooldown);

    public float ScaleAttackDamage(float damage, string element = null) => damage
        * (string.IsNullOrEmpty(element) || element == "None" || element == "ELEM_NONE"
            ? 1f : 1f + StatCards.Value(PlayerStat.Elm_All_ATK));

    public bool TryUpgradeStat(PlayerStatCard card, int level)
    {
        float previousBaseHealth = maxHealth;
        bool firstHealthCard = card != null && card.Stat == PlayerStat.HP && StatCards.Level(PlayerStat.HP) == 0;
        if (IsDead || !StatCards.TryUpgrade(card, level)) return false;
        if (firstHealthCard) maxHealthBeforeStatCards = previousBaseHealth;
        if (card.Stat == PlayerStat.HP)
        {
            float previousMaxHealth = maxHealth;
            maxHealth = StatCards.Value(PlayerStat.HP);
            float healthIncrease = Mathf.Max(0f, maxHealth - previousMaxHealth);
            currentHealth = Mathf.Min(currentHealth + healthIncrease, maxHealth);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
        return true;
    }

    /// <summary>해당 스탯 카드의 모든 단계를 제거하고 획득 전 상태로 되돌립니다.</summary>
    public bool RemoveStatCard(PlayerStat stat)
    {
        if (IsDead || !StatCards.Remove(stat)) return false;
        if (stat == PlayerStat.HP)
        {
            maxHealth = maxHealthBeforeStatCards;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }
        return true;
    }
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

    [Header("성장 설정 (1차 플레이테스트용)")]
    [SerializeField] private PlayerExperienceSettings experienceSettings = new PlayerExperienceSettings();
    private PlayerExperienceProgress experienceProgress;
    private PlayerExperienceProgress ExperienceProgress => experienceProgress ??
        (experienceProgress = new PlayerExperienceProgress(experienceSettings ?? (experienceSettings = new PlayerExperienceSettings())));
    public int CurrentLevel => ExperienceProgress.Level;
    public int PendingLevelUps => ExperienceProgress.PendingLevelUps;
    public event System.Action<int> LevelChanged;
    private SurvivalExperienceBonus experienceBonus;
    private double experienceGameplayTime;
    private SurvivalExperienceBonus ExperienceBonus => experienceBonus ??
        (experienceBonus = new SurvivalExperienceBonus(new SurvivalBalanceSettings()));
    public bool CatchupActive => CurrentLevel < 100 && ExperienceBonus.Active;
    public double ExperienceFractionalCarry => ExperienceBonus.FractionalCarry;
#if UNITY_5_3_OR_NEWER
    private EnemySpawnDirector survivalDirector;
    private bool survivalStarted;
    private void Start() { survivalStarted = true; BindSurvivalDirector(); }
    private void OnEnable() { if (survivalStarted) BindSurvivalDirector(); }
    private void OnDisable()
    {
        if (survivalDirector != null) survivalDirector.TimeAdvanced -= UpdateExperienceTime;
    }
    private void BindSurvivalDirector()
    {
        foreach (EnemySpawnDirector candidate in FindObjectsByType<EnemySpawnDirector>())
            if (candidate.gameObject.scene == gameObject.scene && candidate.Settings.IsValid)
            {
                if (survivalDirector != candidate)
                {
                    survivalDirector = candidate;
                    experienceBonus = new SurvivalExperienceBonus(candidate.Settings);
                }
                survivalDirector.TimeAdvanced -= UpdateExperienceTime;
                survivalDirector.TimeAdvanced += UpdateExperienceTime;
                UpdateExperienceTime(survivalDirector.ElapsedTime);
                break;
            }
    }
#endif

    public void UpdateExperienceTime(float elapsed)
    {
        if (!IsValidAmount(elapsed)) return;
        experienceGameplayTime = elapsed;
        ExperienceBonus.Update(experienceGameplayTime, CurrentLevel);
#if UNITY_5_3_OR_NEWER
        if (survivalDirector != null) survivalDirector.RecordPlayerState(CurrentLevel, CatchupActive);
#endif
    }

    /// <summary>보석을 실제 회수한 시점에만 보정을 적용하고 소수 잔여를 보존합니다.</summary>
    public bool TryCollectExperience(float rawExperience, out float awarded)
    {
        awarded = 0;
        if (IsDead || !IsValidAmount(rawExperience) || rawExperience <= 0) return false;
        ExperienceBonus.Update(experienceGameplayTime, CurrentLevel);
        // 실패하는 지급은 carry도 소비하지 않습니다.
        double boostedExperience = rawExperience * (double)StatCards.Value(PlayerStat.XP_Gain, 1f);
        double exact = boostedExperience * ExperienceBonus.Multiplier + ExperienceBonus.FractionalCarry;
        if (exact > float.MaxValue || ExperienceProgress.Experience + System.Math.Floor(exact) > float.MaxValue) return false;
        awarded = (float)ExperienceBonus.Award(boostedExperience);
        if (awarded > 0 && !AddEXP(awarded)) return false;
        ExperienceBonus.Update(experienceGameplayTime, CurrentLevel);
#if UNITY_5_3_OR_NEWER
        if (survivalDirector != null) survivalDirector.RecordPlayerState(CurrentLevel, CatchupActive);
#endif
        return true;
    }


    [Header("EXP 표시 상태 (요구량은 현재 레벨에서 자동 계산)")]
    [Min(0f)] public float maxEXP;
    [SerializeField] private float currentEXP;
    public float CurrentEXP => currentEXP;
    public event System.Action<float, float> EXPChanged;

    [Header("EXP 획득 범위 설정")]
    [Min(0f)] public float experiencePickupDistance;

    public bool IsDead => currentHealth <= 0f;
    public event System.Action<float, float> HealthChanged;
    public event System.Action PlayerDeath;
    public float CurrentShield { get; private set; }
    public float MaxShield { get; private set; }
    public bool HasShield => CurrentShield > 0f && shieldDuration > 0f;
    public event System.Action<float, float> ShieldChanged;
    private float shieldDuration;
    private float healOverTimePerTick;
    private float healOverTimeDelay;
    private float healOverTimeTimer;
    private int healOverTimeHitsLeft;
    private float invulnerableRemaining;
    public bool IsInvulnerable => invulnerableRemaining > 0f;

    private void Awake()
    {
        InitHealth();
        InitMP();
        InitEXP();
    }

    [ContextMenu("EXP/레벨 1로 초기화")]
    public void InitEXP()
    {
        ExperienceProgress.Reset();
        ExperienceBonus.Reset();
        PublishExperience();
        LevelChanged?.Invoke(CurrentLevel);
    }

    /// <summary>현재 레벨의 XP를 지정합니다. maximum은 호환 인자이며 실제 요구량은 성장 설정으로 계산합니다.</summary>
    public bool SetEXP(float current, float maximum)
    {
        if (!IsValidAmount(current) || !IsValidAmount(maximum)) return false;
        int previousLevel = CurrentLevel;
        ExperienceProgress.SetExperience(current);
        PublishExperience(previousLevel);
        return true;
    }

    /// <summary>요구 XP를 차감해 성장하고 초과분을 이월합니다. 중간 레벨의 선택도 모두 대기합니다.</summary>
    public bool AddEXP(float amount)
    {
        if (IsDead || !IsValidAmount(amount) || amount <= 0f ||
            ExperienceProgress.Experience + amount > float.MaxValue)
            return false;
        int previousLevel = CurrentLevel;
        if (!ExperienceProgress.Add(amount)) return false;
        PublishExperience(previousLevel);
        return true;
    }

    private void PublishExperience(int previousLevel = 0)
    {
        currentEXP = (float)ExperienceProgress.Experience;
        maxEXP = ExperienceProgress.RequiredXP;
        // 모든 상태와 선택 대기를 확정한 뒤 관찰자에게 알립니다.
        if (previousLevel > 0)
            for (int level = previousLevel + 1; level <= CurrentLevel; level++) LevelChanged?.Invoke(level);
        EXPChanged?.Invoke(currentEXP, maxEXP);
    }

    public bool TryCompleteLevelUp() => !IsDead && ExperienceProgress.CompleteLevelUp();
    public void CancelPendingLevelUps() => ExperienceProgress.CancelPendingLevelUps();

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
        if (!IsValidAmount(deltaTime) || deltaTime <= 0f) return;
        RestoreMP(MPRecovery * deltaTime);
    }

    public void TickHealthRecovery(float deltaTime)
    {
        float healthRegen = StatCards.Value(PlayerStat.HP_Regen);
        if (healthRegen > 0f) RestoreHealth(maxHealth * healthRegen * deltaTime);
    }

    private static bool IsValidAmount(float value) =>
        value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    public void InitHealth()
    {
        if (maxHealth <= 0f) maxHealth = 100f;
        currentHealth = maxHealth;
        ClearHealOverTime();
        ClearInvulnerable();
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>유효한 양만 회복하며 최대 체력을 넘기지 않습니다.</summary>
    public float RestoreHealth(float amount)
    {
        if (IsDead || !IsValidAmount(amount) || !IsValidAmount(maxHealth)) return 0f;
        float restored = Mathf.Min(amount, Mathf.Max(0f, maxHealth - currentHealth));
        if (restored <= 0f) return 0f;
        currentHealth += restored;
        HealthChanged?.Invoke(currentHealth, maxHealth);
        return restored;
    }

    public void BeginHealOverTime(float amountPerTick, float hitDelay, int hitCount)
    {
        if (IsDead || !IsValidAmount(amountPerTick) || amountPerTick <= 0f) return;
        if (!IsValidAmount(hitDelay) || hitDelay <= 0f || hitCount <= 0) return;
        healOverTimePerTick = amountPerTick;
        healOverTimeDelay = hitDelay;
        healOverTimeTimer = 0f;
        healOverTimeHitsLeft = hitCount;
    }

    public void TickHealOverTime(float deltaTime)
    {
        if (healOverTimeHitsLeft <= 0 || IsDead || !IsValidAmount(deltaTime) || deltaTime <= 0f) return;
        healOverTimeTimer += deltaTime;
        while (healOverTimeHitsLeft > 0 && healOverTimeTimer >= healOverTimeDelay)
        {
            healOverTimeTimer -= healOverTimeDelay;
            healOverTimeHitsLeft--;
            RestoreHealth(healOverTimePerTick);
            if (IsDead) { ClearHealOverTime(); return; }
        }
        if (healOverTimeHitsLeft <= 0) ClearHealOverTime();
    }

    public void ClearHealOverTime()
    {
        healOverTimePerTick = 0f;
        healOverTimeDelay = 0f;
        healOverTimeTimer = 0f;
        healOverTimeHitsLeft = 0;
    }

    public void GrantInvulnerable(float duration)
    {
        if (IsDead || !IsValidAmount(duration) || duration <= 0f) return;
        invulnerableRemaining = Mathf.Max(invulnerableRemaining, duration);
    }

    public void TickInvulnerable(float deltaTime)
    {
        if (invulnerableRemaining <= 0f || !IsValidAmount(deltaTime) || deltaTime <= 0f) return;
        invulnerableRemaining -= deltaTime;
        if (invulnerableRemaining <= 0f) ClearInvulnerable();
    }

    public void ClearInvulnerable() => invulnerableRemaining = 0f;

    public void GrantShield(float amount, float duration)
    {
        if (IsDead || !IsValidAmount(amount) || amount <= 0f || !IsValidAmount(duration) || duration <= 0f) return;
        MaxShield = amount;
        CurrentShield = amount;
        shieldDuration = duration;
        ShieldChanged?.Invoke(CurrentShield, MaxShield);
    }

    public void TickShield(float deltaTime)
    {
        if (!HasShield || !IsValidAmount(deltaTime) || deltaTime <= 0f) return;
        shieldDuration -= deltaTime;
        if (shieldDuration <= 0f) ClearShield();
    }

    public void ClearShield()
    {
        if (CurrentShield <= 0f && MaxShield <= 0f && shieldDuration <= 0f) return;
        CurrentShield = 0f;
        MaxShield = 0f;
        shieldDuration = 0f;
        ShieldChanged?.Invoke(0f, 0f);
    }

    private float AbsorbShield(float damage)
    {
        if (!HasShield || damage <= 0f) return damage;
        float absorbed = Mathf.Min(CurrentShield, damage);
        CurrentShield -= absorbed;
        if (CurrentShield <= 0.0001f) ClearShield();
        else ShieldChanged?.Invoke(CurrentShield, MaxShield);
        return damage - absorbed;
    }

    /// <summary>Applies validated damage and publishes the updated health state.</summary>
    public bool ApplyDamage(float damage)
    {
        if (damage <= 0f || IsDead) return false;
        // 무적이면 피해·피격 반응 모두 없는 것으로 처리합니다.
        if (IsInvulnerable) return false;
        float defense = StatCards.Value(PlayerStat.DEF);
        damage *= 1f - defense / (defense + 33.34f);
        damage = AbsorbShield(damage);
        if (damage <= 0f) return true;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (IsDead)
        {
            ClearShield();
            ClearHealOverTime();
            ClearInvulnerable();
            CancelPendingLevelUps();
            PlayerDeath?.Invoke();
        }
        return true;
    }
}
