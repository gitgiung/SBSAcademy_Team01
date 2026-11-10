using UnityEngine;

/// <summary>
/// 임시 몸 1패턴: 추격 → 접근하며 팔을 올려 휘두르기.
/// 예고는 바닥 VFX가 아니라 접근 이동과 팔 모션이다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MotionBossApproachSwing : MonoBehaviour
{
    private enum Phase
    {
        Chase,
        Windup,
        Swing,
        Recovery
    }

    [SerializeField] private Transform target;
    [SerializeField] private Transform shoulder;
    [SerializeField] private MotionBossWeaponHitbox weaponHitbox;
    [SerializeField] private float chaseSpeed = 3.2f;
    [SerializeField] private float approachSpeed = 4.6f;
    [SerializeField] private float startSwingDistance = 3.2f;
    [SerializeField] private float windupSeconds = 0.45f;
    [SerializeField] private float swingSeconds = 0.28f;
    [SerializeField] private float recoverySeconds = 0.55f;
    [SerializeField] private float swingArcDegrees = 140f;
    [SerializeField] private float raisedArmDegrees = -35f;
    [SerializeField] private float damage = 18f;
    [SerializeField] private bool showFloorTelegraph;

    private Phase phase = Phase.Chase;
    private float phaseElapsed;
    private float armRestDegrees = 70f;
    private Rigidbody2D body;
    private Vector2 swingFacing = Vector2.right;

    public void Bind(Transform player, Transform shoulderBone, MotionBossWeaponHitbox hitbox)
    {
        target = player;
        shoulder = shoulderBone;
        weaponHitbox = hitbox;
        if (weaponHitbox != null)
            weaponHitbox.Configure(damage, gameObject);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.bodyType = RigidbodyType2D.Kinematic;
        }

        if (weaponHitbox != null)
            weaponHitbox.Configure(damage, gameObject);
        SetArmAngle(armRestDegrees);
        weaponHitbox?.SetArmed(false);

        if (showFloorTelegraph)
            Debug.LogWarning("[MotionBossPrototype] showFloorTelegraph는 프로토타입에서 기본 사용하지 않는다.", this);
    }

    private void FixedUpdate()
    {
        if (target == null || shoulder == null || body == null) return;

        float dt = Time.fixedDeltaTime;
        phaseElapsed += dt;
        Vector2 toTarget = (Vector2)target.position - body.position;
        float distance = toTarget.magnitude;
        Vector2 facing = distance > 0.001f ? toTarget / distance : swingFacing;

        switch (phase)
        {
            case Phase.Chase:
                MoveTowards(facing, chaseSpeed, dt);
                FaceArmIdle();
                if (distance <= startSwingDistance)
                    Enter(Phase.Windup, facing);
                break;

            case Phase.Windup:
                // 제자리 스윙이 아니라 다가오며 팔을 올린다.
                MoveTowards(swingFacing, approachSpeed * 0.55f, dt);
                SetArmAngle(Mathf.Lerp(armRestDegrees, raisedArmDegrees, phaseElapsed / windupSeconds));
                if (phaseElapsed >= windupSeconds)
                    Enter(Phase.Swing, swingFacing);
                break;

            case Phase.Swing:
                MoveTowards(swingFacing, approachSpeed, dt);
                float t = Mathf.Clamp01(phaseElapsed / swingSeconds);
                float swingAngle = Mathf.Lerp(raisedArmDegrees, raisedArmDegrees + swingArcDegrees, t);
                SetArmAngle(swingAngle);
                weaponHitbox?.SetArmed(t >= 0.15f && t <= 0.85f);
                if (phaseElapsed >= swingSeconds)
                    Enter(Phase.Recovery, swingFacing);
                break;

            case Phase.Recovery:
                weaponHitbox?.SetArmed(false);
                SetArmAngle(Mathf.Lerp(raisedArmDegrees + swingArcDegrees, armRestDegrees, phaseElapsed / recoverySeconds));
                if (phaseElapsed >= recoverySeconds)
                    Enter(Phase.Chase, facing);
                break;
        }
    }

    private void Enter(Phase next, Vector2 facing)
    {
        phase = next;
        phaseElapsed = 0f;
        swingFacing = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.right;
        if (next != Phase.Swing)
            weaponHitbox?.SetArmed(false);
    }

    private void MoveTowards(Vector2 facing, float speed, float dt)
    {
        Vector2 step = facing.normalized * (speed * dt);
        body.MovePosition(body.position + step);
        float scaleX = facing.x < 0f ? -1f : 1f;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * scaleX;
        transform.localScale = scale;
    }

    private void FaceArmIdle()
    {
        SetArmAngle(armRestDegrees);
    }

    private void SetArmAngle(float degrees)
    {
        if (shoulder == null) return;
        // localScale.x 반전 시 각도를 그대로 써서 팔이 몸 앞으로 휘두르도록 한다.
        shoulder.localRotation = Quaternion.Euler(0f, 0f, degrees);
    }
}
