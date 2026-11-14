using System;
using System.Collections.Generic;
using UnityEngine;
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
    private ResourceGaugePreview resourceGaugePreview;
    [SerializeField] private Slider expSlider;
    [SerializeField] private InputActionAsset inputActions;
    [Header("약공격 연결")]
    [SerializeField] private Button lightAttackButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private GameObject lightAttackProjectile;
    private GameObject lightAttackCooldown;
    private GameObject heavyAttackCooldown;
    [Header("패시브 투사체 (Key는 SkillData.PassivePrefabKey와 일치)")]
    [SerializeField] private PlayerProjectileBinding[] passiveProjectiles = new PlayerProjectileBinding[0];

    [Header("투사체 적중 섬광")]
    [SerializeField, Min(0f)] private float projectileHitFlashScale = 0.35f;
    [SerializeField] private Sprite[] projectileHitFlashFrames;
    [SerializeField] private Material projectileHitFlashMaterial;

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
        resourceGaugePreview = GetComponent<ResourceGaugePreview>();
        resourceGaugePreview?.Bind(data, healthSlider, mpSlider, expSlider);
        defense = new PlayerDefense(view, parryStartup, parryActive, parryRecovery,
            guardTransitionTime, maxGuardDuration, normalGuardCooldown, breakGuardCooldown);
        defense.ParrySucceeded += HandleParrySucceeded;
        actions = new PlayerActionGate(data);
        movement = new PlayerMovement(transform, data, () => defense.IsMovementSlowed ? parrySpeedRatio : 1f);
        combat = new PlayerCombat(transform, data, bulletPrefab, enemyTag);
        combat.BasicAttackHitFlash = ShowProjectileHitFlash;
        if (passiveProjectiles != null)
            foreach (PlayerProjectileBinding binding in passiveProjectiles)
                if (binding != null) combat.BindProjectile(binding.Key, binding.Prefab);
        lightSkill = new PlayerLightSkill(this, data, view);
        InitializeHeavyCharge();
        InitializeGuardInput();
        if (lightAttackButton != null)
            Util.GetOrAddComponent<PlayerLightAttackButton>(lightAttackButton.gameObject).Bind(this);
        lightAttackCooldown = lightAttackButton != null ? lightAttackButton.transform.Find("Cooldown")?.gameObject : null;
        heavyAttackCooldown = heavyAttackButton != null ? heavyAttackButton.transform.Find("Cooldown")?.gameObject : null;
        RefreshAttackMpIndicators();
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

}
