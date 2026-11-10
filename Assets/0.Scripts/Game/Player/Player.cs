using System;
using System.Collections;
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

    [Header("패링 타이밍 설정 (초 단위)")]
    [SerializeField] private float parryStartup = 0.033f;
    [SerializeField] private float parryActive = 0.133f;
    [SerializeField] private float parryRecovery = 0.233f;

    [Header("일반 가드 설정 (초 단위)")]
    [SerializeField] private float guardTransitionTime = 0.183f;
    [SerializeField] private float maxGuardDuration = 2f;
    [SerializeField] private float normalGuardCooldown = 0.5f;
    [SerializeField] private float breakGuardCooldown = 1.5f;

    [Header("이동 속도 조절")]
    [SerializeField, Range(0f, 1f)] private float parrySpeedRatio = 0.8f;

    [Header("대쉬 설정")]
    [SerializeField, Min(1f)] private float dashSpeedMultiplier = 3f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.15f;
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
    private PlayerShieldBar shieldBar;
    private float nextTacticalReadyTime;
    private const int FallbackTacticalId = 30009;
    private readonly int[] ownedPassiveIds = new int[PlayerLoadout.AttackCapacity];
    private readonly float[] ownedPassiveTimers = new float[PlayerLoadout.AttackCapacity];
    private bool lastExternalMoving;
    private bool startingDash;
    private int lifecycleVersion;

    public bool IsDashing => movement != null && movement.IsDashing;
    public float DashCooldownRemaining => movement != null ? movement.DashCooldownRemaining : 0f;
    public float DashCooldownProgress => movement != null ? movement.DashCooldownProgress : 1f;

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
        if (lightAttackButton != null)
            Util.GetOrAddComponent<PlayerLightAttackButton>(lightAttackButton.gameObject).Bind(this);
        shieldBar = gameObject.AddComponent<PlayerShieldBar>();
        shieldBar.Initialize(data, playerSpriteRenderer);
    }

    private void Start()
    {
        data.InitHealth();
        view.RefreshHealth();
        BindTacticalButton();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) { CancelReleasedHeavyKey(); return; }
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
        if (data == null || hasDied || data.IsDead || Time.timeScale <= 0f || IsInputLocked) return false;
        if (Time.time < nextTacticalReadyTime) return false;
        int id = data.Loadout != null ? data.Loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) : 0;
        if (id == 0) id = FallbackTacticalId;
        if (!CombatSkillTable.TryGet(id, out CombatSkillRecord record)) return false;
        if (!TacticalSkillEffect.TryApply(data, record)) return false;
        nextTacticalReadyTime = Time.time + Mathf.Max(0f, record.Cooldown);
        return true;
    }

    /// <summary>이전 보호막 버튼 경로 호환용입니다.</summary>
    public void TryUseShieldSkill() => TryUseTacticalSkill();

    private void BindTacticalButton()
    {
        if (shieldButton == null)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.scene == gameObject.scene && buttons[i].name == "Skill")
                {
                    shieldButton = buttons[i];
                    break;
                }
            }
        }
        if (shieldButton == null || shieldButton.onClick.GetPersistentEventCount() > 0) return;
        shieldButton.onClick.RemoveListener(TryUseShieldSkill);
        shieldButton.onClick.AddListener(TryUseShieldSkill);
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
        if (!TryBeginLightAttack(skill.MpCost, out int actionId)) return false;
        if (aim.sqrMagnitude < 0.0001f)
            aim = view != null && view.FacingDirection.sqrMagnitude > 0.0001f ? view.FacingDirection : Vector2.right;
        float stackDamage = data.ScaleAttackDamage(skill.Damage(data.attackDamage), skill.Element)
            * (data.Loadout != null ? data.Loadout.GetAttackSkillDamageMultiplier(skill.Id) : 1f);
        StartCoroutine(CastShots(skill, aim.normalized, stackDamage, true, actionId));
        return true;
    }

    private IEnumerator CastShots(LightSkillCast skill, Vector2 aim, float damage, bool lockUntilEnd, int actionId)
    {
        skill = PrepareSkill(skill);
        float projectileSize = 1f + data.StatCards.Value(PlayerStat.Projectile_Size);
        int shots = Mathf.Max(1, skill.Projectiles);
        float spacing = skill.ScatterVolley ? 0f : skill.ShotSpacing;
        for (int i = 0; i < shots; i++)
        {
            if (!isActiveAndEnabled || hasDied || data == null || data.IsDead) break;
            if (!lockUntilEnd && Time.timeScale <= 0f) yield break;
            float spread = shots == 1 || skill.StraightRapidFire ? 0f : (i - (shots - 1) * 0.5f) * 8f;
            Vector2 direction = (Vector2)(Quaternion.Euler(0f, 0f, spread) * (Vector3)aim);
            SpawnLightSkill(skill, direction, damage, projectileSize);
            if (i < shots - 1 && spacing > 0f) yield return new WaitForSeconds(spacing);
        }
        if (!lockUntilEnd) yield break;
        float hold = skill.ScatterVolley ? 0.15f : 0.28f + skill.RepeatSpacing * Mathf.Max(0, skill.Hits - 1);
        if (hold > 0f) yield return new WaitForSeconds(hold);
        CompleteLightAttack(actionId);
    }

    private LightSkillCast PrepareSkill(LightSkillCast skill) => skill.Presented().WithStatUpgrades(
        (int)data.StatCards.Value(PlayerStat.Projectile_Count), 1f + data.StatCards.Value(PlayerStat.Skill_Range));

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
            float damage = data.ScaleAttackDamage(skill.Damage(data.attackDamage), skill.Element);
            StartCoroutine(CastShots(skill, direction, damage * scale * stackMul, false, 0));
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

        if (Managers.Initialized && Managers.Pool != null)
            Managers.Pool.Pop(lightFxPrototype, position, Quaternion.identity, initialize: Arm);
        else
        {
            GameObject fx = Instantiate(lightFxPrototype, position, Quaternion.identity);
            fx.SetActive(true);
            Arm(fx);
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
        StartCoroutine(defense.GuardParryProcess(() => parryInput != null && parryInput.IsPressed()));
        return true;
    }

    private void HandleParrySucceeded()
    {
        int version = lifecycleVersion;
        data.RestoreMP(data.parryMPRecovery);
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
        Array.Clear(ownedPassiveIds, 0, ownedPassiveIds.Length);
        Array.Clear(ownedPassiveTimers, 0, ownedPassiveTimers.Length);
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
