using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Composition root and compatibility facade for the player.
/// Input and Unity lifecycle stay here; gameplay rules and presentation are delegated.
/// </summary>
public sealed class Player : MonoBehaviour, IContactDamageReceiver
{
    [Header("플레이어 데이터 및 기본 설정")]
    public PlayerData data;
    public GameObject bulletPrefab;
    public Slider healthSlider;
    [SerializeField] private InputActionAsset inputActions;

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
    [SerializeField] private BossParry boss;

    private bool hasDied;
    public event System.Action Died;

    private PlayerView view;
    private PlayerMovement movement;
    private PlayerCombat combat;
    private PlayerDefense defense;
    private PlayerController2D joystickController;
    private InputAction moveInput;
    private InputAction dashInput;
    private InputAction parryInput;
    private bool lastExternalMoving;

    public bool IsDashing => movement != null && movement.IsDashing;

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
    }

    private void Awake()
    {
        joystickController = GetComponent<PlayerController2D>();
        if (data == null) data = GetComponent<PlayerData>();
        if (playerSpriteRenderer == null) playerSpriteRenderer = GetComponent<SpriteRenderer>();
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
        defense = new PlayerDefense(boss, view, parryStartup, parryActive, parryRecovery,
            guardTransitionTime, maxGuardDuration, normalGuardCooldown, breakGuardCooldown);
        movement = new PlayerMovement(transform, data, () => defense.IsMovementSlowed ? parrySpeedRatio : 1f);
        combat = new PlayerCombat(transform, data, bulletPrefab, enemyTag);
    }

    private void Start()
    {
        data.InitHealth();
        view.RefreshHealth();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        bool useJoystick = joystickController != null && joystickController.enabled;
        Vector2 moveDirection = moveInput.ReadValue<Vector2>();
        if (useJoystick)
        {
            Vector2 dashDirection = joystickController.MovementDirection.sqrMagnitude > 0.0001f
                ? joystickController.MovementDirection
                : view.FacingDirection;
            if (dashInput.WasPressedThisFrame() && movement.TryBeginDash(dashDirection,
                    dashDuration, dashSpeedMultiplier, dashCooldown))
            {
                defense.CancelForDash();
                StartCoroutine(movement.DashRoutine());
            }
        }
        else
        {
            if (dashInput.WasPressedThisFrame() && movement.TryBeginDash(moveDirection,
                    dashDuration, dashSpeedMultiplier, dashCooldown))
            {
                defense.CancelForDash();
                StartCoroutine(movement.DashRoutine());
            }

            movement.Tick(moveDirection);
            view.SetMovementAnimation(moveDirection, moveDirection.sqrMagnitude > 0.0001f && !movement.IsDashing,
                movement.IsDashing, dashDuration);
        }
        combat.Tick(Time.deltaTime);

        if (!movement.IsDashing && parryInput.WasPressedThisFrame() && defense.CanStart)
            StartCoroutine(defense.GuardParryProcess(() => parryInput != null && parryInput.IsPressed()));
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
                defense.NotifyIncomingAttackBlocked();
                Debug.Log("[패링/가드 성공] 공격을 방어했습니다.");
            }
            return;
        }

        if (!data.ApplyDamage(damage)) return;

        Debug.Log($"[피격] 받은 데미지: {damage} / 남은 체력: {data.currentHealth}");
        if (data.IsDead)
        {
            hasDied = true;
            Died?.Invoke();
            Debug.Log("[Player 사망] 플레이어가 사망했습니다.");
            gameObject.SetActive(false);
        }
    }

    public void TakeContactDamage(float damage, GameObject source)
    {
        TakeDamage(damage, source);
    }

    /// <summary>Updates player visuals from an external movement source such as the mobile joystick.</summary>
    public void SetExternalMovementAnimation(Vector2 moveDirection, bool isDashing = false)
    {
        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        view?.SetMovementAnimation(moveDirection, isMoving && !isDashing, isDashing, dashDuration);
        if (isMoving != lastExternalMoving)
        {
            lastExternalMoving = isMoving;
        }
    }

    private void OnDisable()
    {
        movement?.Reset();
        defense?.Reset();
        view?.Reset();
        DisposeInputActions();
    }

    private void OnDestroy()
    {
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
        moveInput = null;
        dashInput = null;
        parryInput = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
    }
}
