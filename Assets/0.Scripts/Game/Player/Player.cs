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
    private bool lastExternalMoving;
    private bool startingDash;
    private int lifecycleVersion;

    public bool IsDashing => movement != null && movement.IsDashing;
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
        if (lightAttackButton != null)
            Util.GetOrAddComponent<PlayerLightAttackButton>(lightAttackButton.gameObject).Bind(this);
    }

    private void Start()
    {
        data.InitHealth();
        view.RefreshHealth();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || hasDied || data.IsDead) return;
        data.TickMPRecovery(Time.deltaTime);
        lightSkill?.CheckAnimation();
        if (IsInputLocked)
        {
            view.SetMovementAnimation(Vector2.zero, false, false, dashDuration);
            // 약공격 입력 잠금과 자동 패시브 발사 금지는 별개입니다.
            combat.Tick(Time.deltaTime);
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

            movement.Tick(moveDirection);
            view.SetMovementAnimation(moveDirection, moveDirection.sqrMagnitude > 0.0001f && !movement.IsDashing,
                movement.IsDashing, dashDuration);
        }
        if (parryInput.WasPressedThisFrame()) TryParry();
        if (attackInput != null && attackInput.WasPressedThisFrame() && !PointerOverUi())
            TryCastLightSkill(CurrentAim());
        if (!defense.IsInDefenseMotion) combat.Tick(Time.deltaTime);
    }

    public bool TryUseLightSkill()
    {
        if (!CanAcceptGameplayInput || IsHeavyCharging) return false;
        return TryCastLightSkill(CurrentAim());
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
        StartCoroutine(CastLightSkill(skill, aim.normalized, actionId));
        return true;
    }

    private IEnumerator CastLightSkill(LightSkillCast skill, Vector2 aim, int actionId)
    {
        int shots = Mathf.Max(1, skill.Projectiles);
        float spacing = skill.ShotSpacing;
        for (int i = 0; i < shots; i++)
        {
            if (!isActiveAndEnabled || hasDied) break;
            float spread = shots == 1 ? 0f : (i - (shots - 1) * 0.5f) * 8f;
            Vector2 direction = (Vector2)(Quaternion.Euler(0f, 0f, spread) * (Vector3)aim);
            SpawnLightSkill(skill, direction);
            if (i < shots - 1 && spacing > 0f) yield return new WaitForSeconds(spacing);
        }
        float hold = 0.28f + skill.RepeatSpacing * Mathf.Max(0, skill.Hits - 1);
        if (hold > 0f) yield return new WaitForSeconds(hold);
        CompleteLightAttack(actionId);
    }

    private void SpawnLightSkill(LightSkillCast skill, Vector2 direction)
    {
        EnsureLightFxPrototype();
        Vector3 position = transform.position;
        if (skill.Shape == LightSkillShape.Circle || skill.Shape == LightSkillShape.Cone || skill.Shape == LightSkillShape.Arc)
            position += (Vector3)(direction * skill.Range * 0.35f);
        float damage = skill.Damage(data.attackDamage);
        void Arm(GameObject fx)
        {
            if (fx.TryGetComponent<LightSkillFx>(out LightSkillFx skillFx))
                skillFx.Play(skill, direction, damage, enemyTag);
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
        if (!CanAcceptGameplayInput || movement.IsDashing || !defense.CanStart) return false;
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
        lightSkill?.Cancel();
        actions?.Reset();
        joystickController?.StopMovement();
        movement?.Reset();
        defense?.Reset();
        view?.Reset();
        DisposeInputActions();
    }

    private void OnDestroy()
    {
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
        moveInput = null;
        dashInput = null;
        parryInput = null;
        attackInput = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
    }
}
