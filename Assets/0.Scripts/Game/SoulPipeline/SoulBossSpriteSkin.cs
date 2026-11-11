using UnityEngine;

/// <summary>
/// SoulArena 보스에 MainScene BeastZombie 스프라이트·애니메이터를 빌보드로 입힌다.
/// 추격·윈드업·히트박스 판정은 SoulBossApproachSlash가 소유하고, 이 컴포넌트는 표시만 담당한다.
/// 휘두르기 클립의 타격 프레임이 손 히트박스가 켜지는 순간에 오도록 재생 속도를 맞춘다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulBossSpriteSkin : MonoBehaviour
{
    public const string BodyName = "MainBossSprite";
    private const string ImpactEvent = "AnimationEvent_ApplySlashDamage";
    private const float FallbackImpactSeconds = 0.79f;

    private static readonly int FacingXParam = Animator.StringToHash("FacingX");
    private static readonly int FacingYParam = Animator.StringToHash("FacingY");
    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");

    private SoulBossApproachSlash boss;
    private SoulArenaDummyHealth health;
    private Transform body;
    private SpriteRenderer bodyRenderer;
    private Animator bodyAnimator;
    private Transform shadow;
    private Material bodyMaterial;
    private Material shadowMaterial;
    private float scale;
    private Vector2 facing = Vector2.down;
    private Vector3 lastPosition;
    private bool wasAttacking;
    private Transform player;
    private float lift;

    public SpriteRenderer BodyRenderer => bodyRenderer;
    public Animator BodyAnimator => bodyAnimator;
    public Vector2 Facing => facing;
    public float Scale => scale;
    public string LastAttackState { get; private set; }
    public float LastImpactSeconds { get; private set; }
    public bool Applied => bodyAnimator != null && bodyAnimator.runtimeAnimatorController != null;

    /// <summary>보스에 스킨을 붙인다. 참조 에셋이 없으면 기존 3D 몸을 유지하고 null.</summary>
    public static SoulBossSpriteSkin Apply(SoulBossApproachSlash target)
    {
        if (target == null) return null;
        var existing = target.GetComponent<SoulBossSpriteSkin>();
        if (existing != null) return existing;
        var skin = target.gameObject.AddComponent<SoulBossSpriteSkin>();
        return skin.Applied ? skin : null;
    }

    private void Awake()
    {
        boss = GetComponent<SoulBossApproachSlash>();
        health = GetComponent<SoulArenaDummyHealth>();
        var set = Resources.Load<SoulArenaSpriteSet>(SoulArenaSpriteSet.ResourcePath);
        if (set == null || set.BossController == null)
        {
            Debug.LogWarning($"[SoulPipeline] Resources/{SoulArenaSpriteSet.ResourcePath} 보스 컨트롤러 없음 → 3D 몸 유지", this);
            Destroy(this);
            return;
        }

        scale = set.BossScale;
        body = SoulSpriteBillboard.CreateBody(transform, BodyName, scale, set.BossController,
            out bodyRenderer, out bodyAnimator, out bodyMaterial);
        body.gameObject.AddComponent<SoulSpriteAnimationEventSink>();
        shadow = SoulSpriteBillboard.CreateBlobShadow(transform, new Vector2(2.4f, 1.2f), out shadowMaterial);

        GetComponent<SoulBossWindupFlash>()?.Rebind();
        lastPosition = transform.position;
        LateUpdate();
        bodyAnimator.Update(0f);
        LateUpdate();
    }

    private void OnDestroy()
    {
        if (bodyMaterial != null) Destroy(bodyMaterial);
        if (shadowMaterial != null) Destroy(shadowMaterial);
    }

    private void LateUpdate()
    {
        if (body == null) return;
        SoulSpriteBillboard.Place(body, bodyRenderer, transform.position, scale, ref lift);
        if (shadow != null) shadow.rotation = Quaternion.Euler(90f, 0f, 0f);

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        bool dead = health != null && health.IsDead;
        bool active = !dead && boss != null && boss.enabled;
        bool attacking = active && boss.IsAttacking;

        // 공격 중에는 시작 방향을 고정해 휘두르는 방향과 손 히트가 어긋나지 않게 한다.
        if (!attacking) UpdateFacing();
        if (attacking && !wasAttacking) StartSwipe();
        if (!attacking && wasAttacking) bodyAnimator.speed = 1f;
        wasAttacking = attacking;

        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        bodyAnimator.SetFloat(FacingXParam, facing.x);
        bodyAnimator.SetFloat(FacingYParam, facing.y);
        bodyAnimator.SetBool(IsMovingParam, active && !attacking && speed > 0.3f);
        if (dead) bodyAnimator.speed = 0f;
    }

    private void UpdateFacing()
    {
        if (player == null)
        {
            var avatar = FindAnyObjectByType<SoulPipelineAvatar>();
            if (avatar != null) player = avatar.transform;
        }

        Vector3 world = player != null ? player.position - transform.position : transform.forward;
        Vector2 screenDir = SoulSpriteBillboard.ToScreenDirection(world, SoulSpriteBillboard.ViewRotation());
        if (screenDir.sqrMagnitude > 0.0001f) facing = SoulSpriteBillboard.Quantize8(screenDir);
    }

    private void StartSwipe()
    {
        UpdateFacing();
        string clipName = "Boss_Swipe_" + SoulSpriteBillboard.OctantName(facing);
        int state = Animator.StringToHash("Base Layer." + clipName);
        if (!bodyAnimator.HasState(0, state)) return;

        float impact = FindImpactSeconds(clipName);
        float untilArmed = boss.WindupSeconds + boss.HitArmedStart * boss.AttackSeconds;
        bodyAnimator.speed = Mathf.Clamp(impact / Mathf.Max(0.05f, untilArmed), 0.5f, 3f);
        bodyAnimator.Play(state, 0, 0f);
        LastAttackState = clipName;
        LastImpactSeconds = impact;
    }

    private float FindImpactSeconds(string clipName)
    {
        foreach (var clip in bodyAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip == null || clip.name != clipName) continue;
            foreach (var e in clip.events)
                if (e.functionName == ImpactEvent) return e.time;
        }
        return FallbackImpactSeconds;
    }
}
