using UnityEngine;

/// <summary>본선 파이프라인용 3D 플레이어(임시 휴머노이드). MainScene Player와 분리.</summary>
[DisallowMultipleComponent]
public sealed class SoulPipelineAvatar : MonoBehaviour, IContactDamageReceiver
{
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashSeconds = 0.18f;
    [SerializeField] private float dashCooldown = 0.85f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float parrySeconds = 0.13f;
    [SerializeField] private float invulnSeconds = 0.35f;
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkState = "Walk";

    private float health;
    private float parryRemaining;
    private float invulnRemaining;
    private float dashRemaining;
    private float dashCooldownRemaining;
    private float attackLock;
    private Vector3 dashDir = Vector3.forward;
    private Animator animator;
    private CharacterController character;
    private bool wasMoving;

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsDead => health <= 0f;
    public bool IsMoving { get; private set; }
    public float DashSeconds => dashSeconds;
    public event System.Action LightSwung;

    private void Awake()
    {
        health = maxHealth;
        animator = GetComponentInChildren<Animator>();
        character = GetComponent<CharacterController>();
        if (character == null)
        {
            character = gameObject.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = 0.35f;
            character.center = new Vector3(0f, 0.9f, 0f);
        }

        if (GetComponent<SoulGroundGravity>() == null)
            gameObject.AddComponent<SoulGroundGravity>();
        if (GetComponent<SoulArenaBounds>() == null)
            gameObject.AddComponent<SoulArenaBounds>();
        if (GetComponent<SoulPlayerParryFlash>() == null)
            gameObject.AddComponent<SoulPlayerParryFlash>();
        if (GetComponent<SoulPlayerDashTrail>() == null)
            gameObject.AddComponent<SoulPlayerDashTrail>();
    }

    public bool IsDashing => dashRemaining > 0f;
    public bool IsParrying => parryRemaining > 0f;

    private void Update()
    {
        float dt = Time.deltaTime;
        if (parryRemaining > 0f) parryRemaining -= dt;
        if (invulnRemaining > 0f) invulnRemaining -= dt;
        if (dashCooldownRemaining > 0f) dashCooldownRemaining -= dt;
        if (dashRemaining > 0f) dashRemaining -= dt;
        if (attackLock > 0f) attackLock -= dt;
        if (IsDead)
        {
            IsMoving = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            // 보스 윈드업 중 패링 창을 살짝 넓혀 모션 읽기 학습을 돕는다.
            float window = parrySeconds;
            var boss = FindAnyObjectByType<SoulBossApproachSlash>();
            if (boss != null && boss.enabled && boss.IsWindup)
                window = parrySeconds + 0.06f;
            parryRemaining = window;
            // 대시 중 패링 입력 시 대시 종료(캔슬).
            dashRemaining = 0f;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
            TryLightSwing();

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();

        // 카메라 yaw에 맞춘 평면 이동(포켓봉크형 뷰).
        Quaternion yaw = Quaternion.Euler(0f, SoulCameraRig.ViewYaw, 0f);
        Vector3 world = yaw * input;
        if (world.sqrMagnitude > 0.0001f)
            dashDir = world.normalized;

        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.Space))
            && dashCooldownRemaining <= 0f && dashDir.sqrMagnitude > 0.0001f)
        {
            dashRemaining = dashSeconds;
            dashCooldownRemaining = dashCooldown;
            invulnRemaining = Mathf.Max(invulnRemaining, dashSeconds);
        }

        if (dashRemaining > 0f)
        {
            character.Move(dashDir * (dashSpeed * dt));
            transform.rotation = Quaternion.LookRotation(dashDir);
        }
        else if (world.sqrMagnitude > 0.0001f)
        {
            character.Move(world * (moveSpeed * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(world), 12f * dt);
        }
        else
            character.Move(Vector3.zero);

        bool moving = world.sqrMagnitude > 0.0001f || dashRemaining > 0f;
        IsMoving = moving;
        if (animator != null && moving != wasMoving)
        {
            animator.CrossFadeInFixedTime(moving ? walkState : idleState, 0.1f);
            wasMoving = moving;
        }
    }

    public void TakeContactDamage(float damage, GameObject source)
    {
        if (health <= 0f || invulnRemaining > 0f) return;
        if (parryRemaining > 0f)
        {
            parryRemaining = 0f;
            invulnRemaining = Mathf.Max(invulnRemaining, 0.25f);
            SoulCameraShake.Punch(0.2f);
            SoulArenaKillFeed.Push($"패링! ({source?.name ?? "적"})");
            GetComponent<SoulPlayerParryFlash>()?.ReflectSuccess();
            var bossFromHit = source != null ? source.GetComponentInParent<SoulBossApproachSlash>() : null;
            bossFromHit?.StunFromParry();
            SoulParryRiposteHint.Show();
            Debug.Log($"[SoulPipeline] 패링 성공 ({source?.name})", this);
            return;
        }

        health = Mathf.Max(0f, health - damage);
        invulnRemaining = invulnSeconds;
        SoulCameraShake.Punch(0.45f);
        GetComponent<SoulPlayerParryFlash>()?.HurtFlash();
        SoulArenaKillFeed.Push($"피격 -{damage:0} → {health:0}");
        Debug.Log($"[SoulPipeline] 피격 {damage:0.#} → HP {health:0.#}/{maxHealth:0.#}", this);
        if (IsDead)
            SoulArenaKillFeed.Push("플레이어 사망");
    }

    private void TryLightSwing()
    {
        if (attackLock > 0f || IsDead) return;
        attackLock = 0.22f;
        FaceNearestHostile();
        Vector3 origin = transform.position + Vector3.up * 1.0f + transform.forward * 0.7f;
        Collider[] hits = Physics.OverlapSphere(origin, 1.05f);
        int hitCount = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null || c.transform.IsChildOf(transform)) continue;
            var damageable = c.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;
            float dmg = 12f;
            var bossAi = c.GetComponentInParent<SoulBossApproachSlash>();
            if (bossAi != null && bossAi.IsRecovering)
            {
                dmg = 18f;
                SoulArenaKillFeed.Push("리커버리 추가타!");
            }
            damageable.TakeDamage(dmg);
            hitCount++;
            if (hitCount >= 3) break;
        }

        var arm = GetComponent<SoulFallbackPlayerArm>();
        arm?.Pulse();
        LightSwung?.Invoke();
    }

    private void FaceNearestHostile()
    {
        float best = 4.5f * 4.5f;
        Transform nearest = null;
        var healths = FindObjectsByType<SoulArenaDummyHealth>();
        for (int i = 0; i < healths.Length; i++)
        {
            SoulArenaDummyHealth h = healths[i];
            if (h == null || h.IsDead) continue;
            Vector3 d = h.transform.position - transform.position;
            d.y = 0f;
            float sq = d.sqrMagnitude;
            if (sq < best && sq > 0.01f)
            {
                best = sq;
                nearest = h.transform;
            }
        }

        if (nearest == null) return;
        Vector3 dir = nearest.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);
    }
}
