using UnityEngine;

/// <summary>
/// 휴머노이드 보스 1패턴: 추격 → 윈드업(다가오며 팔 올림) → 휘두르기 → 회복.
/// 예고 본체는 접근+모션. 바닥 VFX 없음.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulBossApproachSlash : MonoBehaviour
{
    private enum Phase { Chase, Windup, Attack, Recovery }

    [SerializeField] private Transform target;
    [SerializeField] private float chaseSpeed = 3.6f;
    [SerializeField] private float windupSpeed = 2.0f;
    [SerializeField] private float approachSpeed = 4.8f;
    [SerializeField] private float startAttackDistance = 2.85f;
    [SerializeField] private float windupSeconds = 0.48f;
    [SerializeField] private float attackSeconds = 0.8f;
    [SerializeField] private float recoverySeconds = 0.7f;
    [SerializeField] private float postRecoveryGap = 0.35f;
    [SerializeField] private float hitArmedStart = 0.28f;
    [SerializeField] private float hitArmedEnd = 0.72f;
    [SerializeField] private float damage = 18f;
    [SerializeField] private string chaseState = "Walk";
    [SerializeField] private string attackState = "Slash";
    [SerializeField] private string idleState = "Idle";

    private Phase phase = Phase.Chase;
    private float phaseElapsed;
    private float rechaseLock;
    private Animator animator;
    private CharacterController character;
    private SoulWeaponHitbox3D hitbox;
    private Vector3 attackFacing = Vector3.forward;

    public bool IsAttacking => phase == Phase.Attack || phase == Phase.Windup;
    public bool IsWindup => phase == Phase.Windup;
    public bool IsRecovering => phase == Phase.Recovery;
    public float AttackNormalizedTime
    {
        get
        {
            if (phase == Phase.Windup && windupSeconds > 0f)
                return Mathf.Clamp01(phaseElapsed / windupSeconds) * 0.35f;
            if (phase == Phase.Attack && attackSeconds > 0f)
                return 0.35f + Mathf.Clamp01(phaseElapsed / attackSeconds) * 0.65f;
            return 0f;
        }
    }

    public void Bind(Transform playerTarget, SoulWeaponHitbox3D weaponHitbox)
    {
        target = playerTarget;
        hitbox = weaponHitbox;
        hitbox?.Configure(damage, gameObject);
    }

    /// <summary>패링 성공 시 보스를 리커버리로 강제해 반격 창을 연다.</summary>
    public void StunFromParry()
    {
        if (!enabled || phase == Phase.Recovery) return;
        hitbox?.SetArmed(false);
        Enter(Phase.Recovery, attackFacing);
        rechaseLock = postRecoveryGap + 0.15f;
        SoulCameraShake.Punch(0.3f);
    }

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        character = GetComponent<CharacterController>();
        if (character == null)
        {
            character = gameObject.AddComponent<CharacterController>();
            character.height = 2f;
            character.radius = 0.4f;
            character.center = new Vector3(0f, 1f, 0f);
        }

        if (hitbox == null)
            hitbox = GetComponentInChildren<SoulWeaponHitbox3D>(true);
        hitbox?.Configure(damage, gameObject);
        hitbox?.SetArmed(false);

        if (GetComponent<SoulGroundGravity>() == null)
            gameObject.AddComponent<SoulGroundGravity>();
        if (GetComponent<SoulArenaBounds>() == null)
            gameObject.AddComponent<SoulArenaBounds>();
        if (GetComponent<SoulBossWindupFlash>() == null)
            gameObject.AddComponent<SoulBossWindupFlash>();
    }

    private void Update()
    {
        var health = GetComponent<SoulArenaDummyHealth>();
        if (health != null && health.IsDead)
        {
            hitbox?.SetArmed(false);
            return;
        }

        if (!TryResolveTarget())
        {
            hitbox?.SetArmed(false);
            return;
        }

        float dt = Time.deltaTime;
        phaseElapsed += dt;
        if (rechaseLock > 0f) rechaseLock -= dt;

        Vector3 to = target.position - transform.position;
        to.y = 0f;
        float distance = to.magnitude;
        Vector3 facing = distance > 0.001f ? to / distance : attackFacing;

        switch (phase)
        {
            case Phase.Chase:
                Move(facing, chaseSpeed, dt);
                Face(facing, dt);
                if (rechaseLock <= 0f && distance <= startAttackDistance)
                {
                    // 윈드업 진입 시 살짝 측면으로 커밋해 직선 추격과 구분.
                    Vector3 side = Vector3.Cross(Vector3.up, facing);
                    if (Random.value < 0.5f) side = -side;
                    Vector3 commit = (facing * 0.85f + side * 0.35f).normalized;
                    Enter(Phase.Windup, commit);
                }
                break;

            case Phase.Windup:
                // 제자리 예고가 아니라, 다가오며 팔을 올린다.
                Move(attackFacing, windupSpeed, dt);
                Face(attackFacing, dt);
                hitbox?.SetArmed(false);
                if (phaseElapsed >= windupSeconds)
                    Enter(Phase.Attack, attackFacing);
                break;

            case Phase.Attack:
                Move(attackFacing, approachSpeed, dt);
                Face(attackFacing, dt);
                float t = Mathf.Clamp01(phaseElapsed / attackSeconds);
                bool armed = t >= hitArmedStart && t <= hitArmedEnd;
                hitbox?.SetArmed(armed);
                if (armed) ProbeHandOverlap();
                if (phaseElapsed >= attackSeconds)
                    Enter(Phase.Recovery, attackFacing);
                break;

            case Phase.Recovery:
                hitbox?.SetArmed(false);
                // 휘두른 뒤 물러나며 반격 창. 직후 즉시 재윈드업하지 않음.
                Move(-attackFacing, chaseSpeed * 0.7f, dt);
                Face(facing, dt);
                if (phaseElapsed >= recoverySeconds)
                {
                    rechaseLock = postRecoveryGap;
                    Enter(Phase.Chase, facing);
                }
                break;
        }
    }

    private bool TryResolveTarget()
    {
        if (target == null)
        {
            var avatar = FindFirstObjectByType<SoulPipelineAvatar>();
            if (avatar == null || avatar.IsDead) return false;
            target = avatar.transform;
            return true;
        }

        var avatarHp = target.GetComponent<SoulPipelineAvatar>();
        return avatarHp == null || !avatarHp.IsDead;
    }

    private void Enter(Phase next, Vector3 facing)
    {
        phase = next;
        phaseElapsed = 0f;
        attackFacing = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector3.forward;
        hitbox?.SetArmed(false);

        if (animator == null) return;
        if (next == Phase.Attack || next == Phase.Windup)
            animator.CrossFadeInFixedTime(attackState, 0.05f);
        else if (next == Phase.Chase)
            animator.CrossFadeInFixedTime(chaseState, 0.1f);
        else
            animator.CrossFadeInFixedTime(idleState, 0.1f);
    }

    private void Move(Vector3 facing, float speed, float dt)
    {
        character.Move(facing * (speed * dt));
    }

    private void Face(Vector3 facing, float dt)
    {
        if (facing.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), 10f * dt);
    }

    private void ProbeHandOverlap()
    {
        if (hitbox == null) return;
        Collider[] hits = Physics.OverlapSphere(hitbox.transform.position, 0.38f, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<IContactDamageReceiver>() == null) continue;
            hitbox.TryHitCollider(c);
        }
    }
}
