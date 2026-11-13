using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Composition root and compatibility facade for the player.
/// Input and Unity lifecycle stay here; gameplay rules and presentation are delegated.
/// </summary>
public sealed partial class Player : MonoBehaviour, IContactDamageReceiver
{
    internal const float DefaultParryVfxScale = 1.75f;

    [Header("플레이어 데이터 및 기본 설정")]
    public PlayerData data;
    public GameObject bulletPrefab;
    public Slider healthSlider;
    [SerializeField] private Slider mpSlider;
    [SerializeField] private Slider expSlider;
    [SerializeField] private InputActionAsset inputActions;
    [Header("약공격 연결")]
    [SerializeField] private Button lightAttackButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private GameObject lightAttackProjectile;
    [Header("패시브 투사체 (Key는 SkillData.PassivePrefabKey와 일치)")]
    [SerializeField] private PlayerProjectileBinding[] passiveProjectiles = new PlayerProjectileBinding[0];

    [Header("태그 설정")]
    public string enemyTag = "Enemy";

    [Header("렌더러 설정")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Animator playerAnimator;

    [Header("패링 이펙트 설정")]
    [InspectorName("패링 이펙트 크기"), SerializeField, Min(0.01f)]
    [Tooltip("패링 성공 섬광의 크기입니다. 기본값은 1.75입니다.")]
    private float parryVfxScale = DefaultParryVfxScale;

    [Header("패링 타이밍 설정 (초 단위)")]
    [SerializeField] private float parryStartup = 0.033f;
    [SerializeField] private float parryActive = 0.133f;
    [SerializeField] private float parryRecovery = 0.233f;
    [SerializeField, Min(0f)] private float parryInvulnerableDuration = 0.5f;

    [Header("일반 가드 설정 (초 단위)")]
    [SerializeField] private float guardTransitionTime = 0.183f;
    [SerializeField] private float maxGuardDuration = 2f;
    [SerializeField] private float normalGuardCooldown = 0.5f;
    [SerializeField] private float breakGuardCooldown = 1.5f;

    [Header("이동 속도 조절")]
    [SerializeField, Range(0f, 1f)] private float parrySpeedRatio = 0.8f;

    [Header("대쉬 설정")]
    [SerializeField, Min(1f)] private float dashSpeedMultiplier = 3f;
    // 이동속도×배율×시간. 0.15→0.25로 거리를 약 1.67배 올립니다.
    [SerializeField, Min(0.01f)] private float dashDuration = 0.25f;
    [SerializeField, Min(0f)] private float dashCooldown = 3f;

    private bool hasDied;
    public event System.Action Died;
    public event System.Action<float, GameObject> Damaged;
    public static event System.Action<Player, float> DamageApplied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDamageObservers() => DamageApplied = null;

    private PlayerView view;
    private PlayerMovement movement;
    private PlayerCombat combat;
    private PlayerDefense defense;
    private PlayerActionGate actions;
    private PlayerLightSkill lightSkill;
    private PlayerController2D joystickController;
    private InputAction moveInput;
    private InputAction dashInput;
    private InputAction parryInput;
    private InputAction attackInput;
    private GameObject lightFxPrototype;
    private SunBarrageCrest sunCrest;
    private PlayerShieldBar shieldBar;
    private float nextTacticalReadyTime;
    private float tacticalCooldownDuration;
    private const int FallbackTacticalId = 30009;
    /// <summary>기획 초기 기준: 전략 스킬은 스테이지당 5회.</summary>
    public const int TacticalStageMaxUses = 5;
    private readonly Dictionary<int, int> tacticalUsesRemaining = new Dictionary<int, int>();
    private readonly int[] ownedPassiveIds = new int[PlayerLoadout.AttackCapacity];
    private readonly float[] ownedPassiveTimers = new float[PlayerLoadout.AttackCapacity];
    private bool lastExternalMoving;
    private bool startingDash;
    private int lifecycleVersion;

    public bool IsDashing => movement != null && movement.IsDashing;
    internal float ParryVfxScale => Mathf.Max(0.01f, parryVfxScale);
    public float DashCooldownRemaining => movement != null ? movement.DashCooldownRemaining : 0f;
    public float DashCooldownProgress => movement != null ? movement.DashCooldownProgress : 1f;
    public event Action TacticalUsesChanged;
    public float TacticalCooldownRemaining => Mathf.Max(0f, nextTacticalReadyTime - Time.time);
    public float TacticalCooldownProgress => tacticalCooldownDuration > 0f
        ? Mathf.Clamp01(1f - TacticalCooldownRemaining / tacticalCooldownDuration) : 1f;

    /// <summary>모바일 Sprint 버튼도 기존 입력과 같은 조준 방향·MP·대시 규칙을 사용합니다.</summary>
    public bool TryUseDash() => TryDash(CurrentAim());
    public int ActiveLightAttackId => lightSkill != null ? lightSkill.ActionId : 0;
    public bool IsInputLocked => actions != null && actions.IsInputLocked;
    public bool CanAcceptGameplayInput => isActiveAndEnabled && !hasDied && data != null
        && !data.IsDead && !IsInputLocked && !startingDash && !startingDefense && !parryFeedbackRunning
        && (heavyCharge == null || !heavyCharge.IsStarting) && Time.timeScale > 0f;

    private void OnEnable()
    {
        moveInput = CreateInputClone("Player/Move");
        dashInput = CreateInputClone("Player/Dash");
        parryInput = CreateInputClone("Player/Parry");

        if (moveInput == null || dashInput == null || parryInput == null)
        {
            Debug.LogError("Player requires Move, Dash, and Parry actions in its Input Action Asset.", this);
            DisposeInputActions();
            enabled = false;
            return;
        }

        moveInput.Enable();
        dashInput.Enable();
        parryInput.Enable();
        attackInput = CloneOptional("Player/Attack");
        attackInput?.Enable();
        EnableHeavyInput();
        view?.RefreshResources();
    }

    private void Awake()
    {
        joystickController = GetComponent<PlayerController2D>();
        if (data == null) data = GetComponent<PlayerData>();
        if (playerSpriteRenderer == null) playerSpriteRenderer = GetComponent<SpriteRenderer>();
        GroundShadow.Attach(playerSpriteRenderer);
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (playerAnimator == null) playerAnimator = gameObject.AddComponent<Animator>();
        if (playerAnimator.runtimeAnimatorController == null)
            playerAnimator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("PlayerAnimator/Player");

        if (data == null)
        {
            Debug.LogError("Player requires a PlayerData component.", this);
            enabled = false;
            return;
        }

        view = new PlayerView(transform, playerSpriteRenderer, healthSlider, data, playerAnimator);
        view.BindResourceSliders(mpSlider, expSlider);
        GetComponent<ResourceGaugePreview>()?.Bind(data, healthSlider, mpSlider, expSlider);
        defense = new PlayerDefense(view, parryStartup, parryActive, parryRecovery,
            guardTransitionTime, maxGuardDuration, normalGuardCooldown, breakGuardCooldown);
        defense.ParrySucceeded += HandleParrySucceeded;
        actions = new PlayerActionGate(data);
        movement = new PlayerMovement(transform, data, () => defense.IsMovementSlowed ? parrySpeedRatio : 1f);
        combat = new PlayerCombat(transform, data, bulletPrefab, enemyTag);
        if (passiveProjectiles != null)
            foreach (PlayerProjectileBinding binding in passiveProjectiles)
                if (binding != null) combat.BindProjectile(binding.Key, binding.Prefab);
        lightSkill = new PlayerLightSkill(this, data, view);
        InitializeHeavyCharge();
        InitializeGuardInput();
        if (lightAttackButton != null)
            Util.GetOrAddComponent<PlayerLightAttackButton>(lightAttackButton.gameObject).Bind(this);
        BindTacticalButton();
        shieldBar = gameObject.AddComponent<PlayerShieldBar>();
        shieldBar.Initialize(data, playerSpriteRenderer);
    }

    private void Start()
    {
        data.InitHealth();
        view.RefreshHealth();
        // 모바일 UI가 Awake 이후에 켜지는 경우를 대비해 한 번 더 연결합니다.
        BindTacticalButton();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) { CancelReleasedHeavyKey(); ReleaseTouchGuard(); return; }
        if (hasDied || data.IsDead) return;
        data.TickShield(Time.deltaTime);
        data.TickHealOverTime(Time.deltaTime);
        data.TickHealthRecovery(Time.deltaTime);
        data.TickInvulnerable(Time.deltaTime);
        data.TickMPRecovery(Time.deltaTime);
        lightSkill?.CheckAnimation();
        if (IsInputLocked)
        {
            view.SetMovementAnimation(Vector2.zero, false, false, dashDuration);
            // 약공격 입력 잠금과 자동 패시브 발사 금지는 별개입니다.
            combat.Tick(Time.deltaTime);
            TickOwnedPassives(Time.deltaTime);
            return;
        }

        bool useJoystick = joystickController != null && joystickController.enabled;
        Vector2 moveDirection = moveInput.ReadValue<Vector2>();
        if (useJoystick)
        {
            Vector2 dashDirection = joystickController.MovementDirection.sqrMagnitude > 0.0001f
                ? joystickController.MovementDirection
                : view.FacingDirection;
            if (dashInput.WasPressedThisFrame()) TryDash(dashDirection);
        }
        else
        {
            if (dashInput.WasPressedThisFrame()) TryDash(moveDirection);

            if (!IsHeavyCharging)
            {
                movement.Tick(moveDirection);
                view.SetMovementAnimation(moveDirection, moveDirection.sqrMagnitude > 0.0001f && !movement.IsDashing,
                    movement.IsDashing, dashDuration);
            }
        }
        if (parryInput.WasPressedThisFrame()) TryParry();
        TickHeavyCharge();
        if (IsHeavyCharging || IsInputLocked) return;
        if (attackInput != null && attackInput.WasPressedThisFrame() && !PointerOverUi())
            TryCastLightSkill(CurrentAim());
        if (!defense.IsInDefenseMotion)
        {
            combat.Tick(Time.deltaTime);
            TickOwnedPassives(Time.deltaTime);
        }
    }

    public bool TryUseLightSkill()
    {
        if (!CanAcceptGameplayInput || IsHeavyCharging) return false;
        return TryCastLightSkill(CurrentAim());
    }

    /// <summary>장착한 전략 스킬을 시전합니다. 미장착이면 표의 물의 방패(30009)로 대체합니다.</summary>
    public bool TryUseTacticalSkill()
    {
        // 약공격 잠금과 별개로 전략은 기존 보호막처럼 사망·정지 중에만 거절합니다.
        if (data == null || hasDied || data.IsDead || Time.timeScale <= 0f) return false;
        if (Time.time < nextTacticalReadyTime) return false;
        int id = data.Loadout != null ? data.Loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) : 0;
        if (id == 0) id = FallbackTacticalId;
        if (!CombatSkillTable.TryGet(id, out CombatSkillRecord record)) return false;
        int cost = TacticalUseCost(record);
        if (cost > 0 && GetTacticalUsesRemaining(id) < cost) return false;
        if (!TacticalSkillEffect.TryApply(data, record)) return false;
        if (cost > 0) ConsumeTacticalUses(id, cost);
        tacticalCooldownDuration = Mathf.Max(0f, record.Cooldown);
        nextTacticalReadyTime = Time.time + tacticalCooldownDuration;
        return true;
    }

    /// <summary>Skill 버튼에 표시할 현재 전략의 남은 스테이지 횟수입니다.</summary>
    public int GetEquippedTacticalUsesRemaining()
    {
        int id = data != null && data.Loadout != null
            ? data.Loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) : 0;
        if (id == 0) id = FallbackTacticalId;
        return GetTacticalUsesRemaining(id);
    }

    public int GetTacticalUsesRemaining(int skillId)
    {
        if (skillId <= 0) return 0;
        if (!tacticalUsesRemaining.TryGetValue(skillId, out int remaining))
        {
            remaining = TacticalStageMaxUses;
            tacticalUsesRemaining[skillId] = remaining;
        }
        return remaining;
    }

    public void ResetTacticalStageUses()
    {
        tacticalUsesRemaining.Clear();
        TacticalUsesChanged?.Invoke();
    }

    private void ConsumeTacticalUses(int skillId, int cost)
    {
        int remaining = Mathf.Max(0, GetTacticalUsesRemaining(skillId) - Mathf.Max(1, cost));
        tacticalUsesRemaining[skillId] = remaining;
        TacticalUsesChanged?.Invoke();
    }

    private static int TacticalUseCost(CombatSkillRecord record)
    {
        if (record == null) return 0;
        if (!string.Equals(record.CostType, "Count", StringComparison.OrdinalIgnoreCase)) return 0;
        return Mathf.Max(1, Mathf.RoundToInt(record.CostAmount));
    }

    /// <summary>이전 보호막 버튼 경로 호환용입니다.</summary>
    public void TryUseShieldSkill() => TryUseTacticalSkill();

    private void BindTacticalButton()
    {
        if (shieldButton == null)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.scene == gameObject.scene && buttons[i].name == "Skill")
                {
                    shieldButton = buttons[i];
                    break;
                }
            }
        }
        if (shieldButton == null) return;
        // 약공격과 같이 포인터 다운에서 바로 시전합니다. Inspector 영구 이벤트와 무관합니다.
        shieldButton.onClick.RemoveListener(TryUseShieldSkill);
        Util.GetOrAddComponent<PlayerTacticalSkillButton>(shieldButton.gameObject).Bind(this);
        Util.GetOrAddComponent<SprintCooldownUI>(shieldButton.gameObject).BindTactical(this);
    }

    public void NotifyLightAttackAnimationProgress(int actionId, float elapsed) => lightSkill?.OnAnimationProgress(actionId, elapsed);
    public void NotifyLightAttackAnimationFinished(int actionId) => lightSkill?.OnAnimationFinished(actionId);

    /// <summary>장착된 약공격만 시전합니다. 미장착 또는 실패 시 MP를 쓰지 않습니다.</summary>
    public bool TryCastLightSkill(Vector2 aim)
    {
        int id = data != null && data.Loadout != null
            ? data.Loadout.GetEquippedSkillId(PlayerSkillButton.Light) : 0;
        if (id == 0) return false;
        if (!LightSkillCatalog.TryGet(id, out LightSkillCast skill)) return false;
        if (!view.CanPlayLightAttack("LightAttack")) return false;
        if (!TryBeginLightAttack(skill.MpCost, out int actionId)) return false;
        if (!view.CanPlayLightAttack("LightAttack"))
        {
            CompleteLightAttack(actionId);
            data.RestoreMP(skill.MpCost);
            return false;
        }
        if (aim.sqrMagnitude < 0.0001f)
            aim = view != null && view.FacingDirection.sqrMagnitude > 0.0001f ? view.FacingDirection : Vector2.right;
        float stackDamage = data.ScaleAttackDamage(skill.Damage(data.AttackDamage), skill.Element)
            * (data.Loadout != null ? data.Loadout.GetAttackSkillDamageMultiplier(skill.Id) : 1f);
        BeginManualSkillAnimation(skill, aim.normalized, stackDamage, actionId);
        return true;
    }

    private void BeginManualSkillAnimation(LightSkillCast skill, Vector2 aim, float damage, int actionId)
    {
        lightSkill.BeginManualCast(actionId, () => StartCoroutine(CastShots(skill, aim, damage, true)));
        view.PlayLightAttack("LightAttack", aim);
    }

    private IEnumerator CastShots(LightSkillCast skill, Vector2 aim, float damage, bool isManual)
    {
        skill = PrepareSkill(skill);
        float projectileSize = 1f + data.StatCards.Value(PlayerStat.Projectile_Size);
        int shots = Mathf.Max(1, skill.Projectiles);
        float spacing = skill.ScatterVolley ? 0f : skill.ShotSpacing;
        // 태양 탄막은 태양 문양을 먼저 그린 뒤, 긴 가시 끝에서 적을 쫓는 미사일을 한 발씩 쏩니다.
        SunBarrageCrest crest = SunCrestFor(skill);
        int firstSpike = 0;
        if (crest != null)
        {
            crest.Play(crest.DrawSeconds + spacing * (shots - 1) + SunBarrageCrest.LingerSeconds);
            firstSpike = crest.NearestSpike(aim);
            // 수동 발사는 모션0.25초의 타격 시점에 이미 도달했습니다.
            if (!isManual) yield return new WaitForSeconds(crest.DrawSeconds);
        }
        for (int i = 0; i < shots; i++)
        {
            if (!isActiveAndEnabled || hasDied || data == null || data.IsDead) break;
            if (!isManual && Time.timeScale <= 0f) yield break;
            if (crest != null) SpawnSunMissile(skill, crest, firstSpike + i, aim, damage, projectileSize);
            else
            {
                float spread = shots == 1 || skill.StraightRapidFire ? 0f : (i - (shots - 1) * 0.5f) * 8f;
                Vector2 direction = (Vector2)(Quaternion.Euler(0f, 0f, spread) * (Vector3)aim);
                SpawnLightSkill(skill, direction, damage, projectileSize);
            }
            if (i < shots - 1 && spacing > 0f) yield return new WaitForSeconds(spacing);
        }
    }

    private LightSkillCast PrepareSkill(LightSkillCast skill) => skill.Presented().WithStatUpgrades(
        (int)data.StatCards.Value(PlayerStat.Projectile_Count), 1f + data.StatCards.Value(PlayerStat.Skill_Range));

#if UNITY_EDITOR
    [ContextMenu("테스트/스킬 범위·투사체 크기 진단 (Play 중)")]
    private void DiagnoseSkillScaling()
    {
        if (!Application.isPlaying || data == null)
        {
            Debug.Log("[SkillScale] Play 중 플레이어에서 실행하세요.", this);
            return;
        }
        float rangeBonus = data.StatCards.Value(PlayerStat.Skill_Range);
        float sizeBonus = data.StatCards.Value(PlayerStat.Projectile_Size);
        Debug.Log($"[SkillScale] 기술 범위 Lv{data.StatCards.Level(PlayerStat.Skill_Range)} +{rangeBonus:P0}, "
            + $"투사체 크기 Lv{data.StatCards.Level(PlayerStat.Projectile_Size)} +{sizeBonus:P0}", this);
        for (int i = 0; i < data.Loadout.UnlockedAttackSlots; i++)
        {
            var owned = data.Loadout.GetAttackSkill(i);
            if (owned == null || !CombatSkillTable.TryGet(owned.DataId, out var record)
                || !LightSkillCast.TryFrom(record, out var original)) continue;
            var prepared = PrepareSkill(original);
            var clip = LightSkillVisuals.Baked(LightSkillVisuals.BakedClipName(prepared.Id));
            string expectedScale = clip == null ? "대체 그림 사용"
                : (prepared.Shape == LightSkillShape.Projectile || clip.ReachUnits <= 0f
                    ? 1f + sizeBonus : prepared.Range / clip.ReachUnits).ToString("0.###");
            Debug.Log($"[SkillScale] {prepared.Id} {prepared.Name}: {prepared.Shape}, "
                + $"범위 {original.Range:0.###} → {prepared.Range:0.###}, 구운 그림 예상 Scale {expectedScale}", this);
        }
        foreach (var fx in FindObjectsByType<LightSkillFx>())
        {
            if (fx.gameObject.scene != gameObject.scene) continue;
            var renderer = fx.GetComponent<SpriteRenderer>();
            var collider = fx.GetComponent<CircleCollider2D>();
            float worldRadius = collider != null && collider.enabled
                ? collider.radius * Mathf.Max(Mathf.Abs(fx.transform.lossyScale.x), Mathf.Abs(fx.transform.lossyScale.y)) : 0f;
            Debug.Log($"[SkillScale] 활성 {fx.name}: localScale={fx.transform.localScale}, "
                + $"worldScale={fx.transform.lossyScale}, 그림={renderer?.sprite?.name}, "
                + $"그림 bounds={renderer?.bounds.size}, 투사체 월드 반경={worldRadius:0.###}", fx);
        }
    }
#endif

    /// <summary>보유한 약·강 스킬을 표의 패시브 주기마다 같은 모양으로 발사합니다. MP와 입력 잠금은 쓰지 않습니다.</summary>
    private void TickOwnedPassives(float deltaTime)
    {
        if (IsHeavyCharging || data == null || data.Loadout == null || combat == null) return;
        for (int i = 0; i < ownedPassiveIds.Length; i++)
        {
            Data.SkillData owned = data.Loadout.GetAttackSkill(i);
            int id = owned != null ? owned.DataId : 0;
            if (ownedPassiveIds[i] != id) { ownedPassiveIds[i] = id; ownedPassiveTimers[i] = 0f; }
            if (!CombatSkillTable.TryGet(id, out CombatSkillRecord record)
                || !record.TryOwnedPassive(out float cooldown, out float scale)
                || !LightSkillCast.TryFrom(record, out LightSkillCast skill)) continue;
            ownedPassiveTimers[i] = Mathf.Max(0f, ownedPassiveTimers[i] - deltaTime / data.PassiveCooldownMultiplier);
            if (ownedPassiveTimers[i] > 0f) continue;
            if (!combat.TryGetClosestEnemyDirection(PrepareSkill(skill).Range, out Vector2 direction)) continue;
            ownedPassiveTimers[i] = cooldown;
            float stackMul = data.Loadout.GetAttackSkillDamageMultiplier(id);
            float damage = data.ScaleAttackDamage(skill.Damage(data.AttackDamage), skill.Element);
            StartCoroutine(CastShots(skill, direction, damage * scale * stackMul, false));
        }
    }

    private void SpawnLightSkill(LightSkillCast skill, Vector2 direction, float damage, float projectileSize)
    {
        EnsureLightFxPrototype();
        Vector3 position = transform.position;
        if (skill.Shape == LightSkillShape.Circle || skill.Shape == LightSkillShape.Cone || skill.Shape == LightSkillShape.Arc)
            position += (Vector3)(direction * skill.Range * 0.35f);
        void Arm(GameObject fx)
        {
            if (fx.TryGetComponent<LightSkillFx>(out LightSkillFx skillFx))
                skillFx.Play(skill, direction, damage, enemyTag, projectileSize);
        }

        PopLightFx(position, Arm);
    }

    /// <summary>구운 태양 탄막 그림이 있을 때만 태양 문양을 돌려줍니다. 없으면 null이고 예전 탄환 연사로 쏩니다.</summary>
    private SunBarrageCrest SunCrestFor(LightSkillCast skill)
    {
        if (!skill.SunBarrage || skill.Shape != LightSkillShape.Projectile) return null;
        if (LightSkillVisuals.BlazeFrames(LightSkillVisuals.SunMissilePrefix).Length == 0) return null;
        if (sunCrest == null) sunCrest = SunBarrageCrest.Attach(transform);
        return sunCrest.Ready ? sunCrest : null;
    }

    /// <summary>태양 문양의 긴 가시 끝에서 유도 미사일 한 발을 쏩니다. 적이 없으면 aim 쪽으로 날아갑니다.</summary>
    private void SpawnSunMissile(LightSkillCast skill, SunBarrageCrest crest, int spike, Vector2 aim, float damage, float projectileSize)
    {
        EnsureLightFxPrototype();
        Vector2 castOrigin = transform.position;
        Vector2 outward = crest.SpikeDirection(spike);
        crest.Flare(spike);
        void Arm(GameObject fx)
        {
            if (fx.TryGetComponent<LightSkillFx>(out LightSkillFx skillFx))
                skillFx.PlayHoming(skill, outward, aim, castOrigin, damage, enemyTag, projectileSize);
        }

        PopLightFx(crest.SpikeTip(spike), Arm);
    }

    private void PopLightFx(Vector3 position, Action<GameObject> arm)
    {
        if (Managers.Initialized && Managers.Pool != null)
            Managers.Pool.Pop(lightFxPrototype, position, Quaternion.identity, initialize: arm);
        else
        {
            GameObject fx = Instantiate(lightFxPrototype, position, Quaternion.identity);
            fx.SetActive(true);
            arm(fx);
        }
    }

    private void EnsureLightFxPrototype()
    {
        if (lightFxPrototype != null) return;
        lightFxPrototype = new GameObject("LightSkillFx");
        lightFxPrototype.SetActive(false);
        lightFxPrototype.AddComponent<SpriteRenderer>();
        CircleCollider2D collider = lightFxPrototype.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        Rigidbody2D body = lightFxPrototype.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.useFullKinematicContacts = true;
        lightFxPrototype.AddComponent<PooledObject>();
        lightFxPrototype.AddComponent<LightSkillFx>();
    }

    private Vector2 CurrentAim()
    {
        if (joystickController != null && joystickController.enabled
            && joystickController.MovementDirection.sqrMagnitude > 0.0001f)
            return joystickController.MovementDirection;
        Vector2 move = moveInput != null ? moveInput.ReadValue<Vector2>() : Vector2.zero;
        if (move.sqrMagnitude > 0.0001f) return move;
        return view != null ? view.FacingDirection : Vector2.right;
    }

    private static bool PointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private InputAction CloneOptional(string actionPath)
    {
        if (inputActions == null) return null;
        InputAction source = inputActions.FindAction(actionPath, false);
        return source != null ? source.Clone() : null;
    }
    public bool TryBeginLightAttack(float mpCost, out int actionId)
    {
        actionId = 0;
        if (!CanAcceptGameplayInput || IsHeavyCharging || movement.IsDashing || !defense.CanStart) return false;
        if (!actions.TryBeginLightAttack(mpCost, out actionId)) return false;
        joystickController?.StopMovement();
        view.SetMovementAnimation(Vector2.zero, false, false, dashDuration);
        return true;
    }

    /// <summary>애니메이션 실행에 보관한 ID를 반환합니다. 이전 동작의 늦은 종료는 무시합니다.</summary>
    public bool CompleteLightAttack(int actionId) => actions != null && actions.CompleteLightAttack(actionId);

    public bool TryDash(Vector2 direction)
    {
        if (!CanAcceptGameplayInput || !movement.CanBeginDash(direction)) return false;
        float cost = data.dashMPCost;
        int version = lifecycleVersion;
        bool paid = false;
        bool started = false;
        // MPChanged는 동기 알림입니다. 알림 중 다른 행동이 시작되지 않도록 예약합니다.
        startingDash = true;
        try
        {
            paid = data.TrySpendMP(cost);
            if (!paid || version != lifecycleVersion || !isActiveAndEnabled || hasDied
                || data.IsDead || IsInputLocked || Time.timeScale <= 0f) return false;
            if (!movement.TryBeginDash(direction, dashDuration, dashSpeedMultiplier, dashCooldown)) return false;
            started = true;
            ResetHeavyCharge();
            guardInput?.Release();
            defense.CancelForDash();
            joystickController?.StopMovement();
            StartCoroutine(movement.DashRoutine());
            return true;
        }
        finally
        {
            // 아직 시작하지 못한 회피만 되돌립니다. 강공격의 취소 환불 정책과 별개입니다.
            try { if (paid && !started) data.RestoreMP(cost); }
            finally { startingDash = false; }
        }
    }

    public bool TryParry()
    {
        if (!CanAcceptGameplayInput || movement.IsDashing || !defense.CanStart) return false;
        ResetHeavyCharge();
        StartCoroutine(defense.GuardParryProcess(IsGuardHeld));
        return true;
    }

    private void HandleParrySucceeded()
    {
        DeviceHaptics.Pulse();
        int version = lifecycleVersion;
        data.RestoreMP(data.parryMPRecovery);
        if (parryInvulnerableDuration > 0f)
            data.GrantInvulnerable(parryInvulnerableDuration);
        if (version == lifecycleVersion && isActiveAndEnabled && !hasDied && !data.IsDead)
        {
            BeginParryFeedback(defense.LastParrySource);
            PlayParryCameraZoom();
        }
    }

    /// <summary>Compatibility entry point used by boss attacks and existing scene logic.</summary>
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (data == null || hasDied || defense == null || movement == null)
            return;

        if (data.IsInvulnerable)
            return;

        if (movement.IsDashing || defense.IsBlockingDamage)
        {
            if (defense.IsBlockingDamage)
            {
                defense.NotifyIncomingAttackBlockedFrom(attacker);
                Debug.Log("[패링/가드 성공] 공격을 방어했습니다.");
            }
            return;
        }

        if (!data.ApplyDamage(damage)) return;
        if (IsHeavyCharging && !heavyCharge.SuperArmor) ResetHeavyCharge();
        DamageApplied?.Invoke(this, damage);

        Debug.Log($"[피격] 받은 데미지: {damage} / 남은 체력: {data.currentHealth}");
        if (data.IsDead)
        {
            hasDied = true;
            Died?.Invoke();
            Debug.Log("[Player 사망] 플레이어가 사망했습니다.");
            gameObject.SetActive(false);
        }
        else Damaged?.Invoke(damage, attacker);
    }

    public void TakeContactDamage(float damage, GameObject source)
    {
        TakeDamage(damage, source);
    }

    /// <summary>Updates player visuals from an external movement source such as the mobile joystick.</summary>
    public void SetExternalMovementAnimation(Vector2 moveDirection, bool isDashing = false)
    {
        if (!CanAcceptGameplayInput) moveDirection = Vector2.zero;
        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        view?.SetMovementAnimation(moveDirection, isMoving && !isDashing, isDashing, dashDuration);
        if (isMoving != lastExternalMoving)
        {
            lastExternalMoving = isMoving;
        }
    }

    private void OnDisable()
    {
        unchecked { lifecycleVersion++; }
        CancelParryFeedback();
        CancelParryCameraZoom();
        StopAllCoroutines();
        // 발사 코루틴이 끊기면 태양 문양만 남지 않게 함께 지웁니다.
        if (sunCrest != null) sunCrest.Cancel();
        guardInput?.Release();
        Array.Clear(ownedPassiveIds, 0, ownedPassiveIds.Length);
        Array.Clear(ownedPassiveTimers, 0, ownedPassiveTimers.Length);
        tacticalUsesRemaining.Clear();
        lightSkill?.Cancel();
        ResetHeavyCharge();
        actions?.Reset();
        joystickController?.StopMovement();
        movement?.Reset();
        defense?.Reset();
        view?.Reset();
        DisposeInputActions();
    }

    private void OnDestroy()
    {
        if (shieldButton != null) shieldButton.onClick.RemoveListener(TryUseShieldSkill); // TryUseTacticalSkill 경로
        if (lightFxPrototype != null) Destroy(lightFxPrototype);
        view?.Dispose();
    }

    private InputAction CreateInputClone(string actionPath)
    {
        if (inputActions == null)
        {
            Debug.LogError("Player requires an Input Action Asset reference.", this);
            return null;
        }

        InputAction source = inputActions.FindAction(actionPath, false);
        if (source == null)
        {
            Debug.LogError($"Input Action '{actionPath}' was not found.", this);
            return null;
        }

        return source.Clone();
    }

    private void DisposeInputActions()
    {
        moveInput?.Dispose();
        dashInput?.Dispose();
        parryInput?.Dispose();
        attackInput?.Dispose();
        heavyInput?.Dispose();
        moveInput = null;
        dashInput = null;
        parryInput = null;
        attackInput = null;
        heavyInput = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
    }
}
