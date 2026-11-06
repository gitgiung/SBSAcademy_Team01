using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyFollow), typeof(EnemyHealth), typeof(Rigidbody2D))]
// Normal melee damage is synchronized to Animation Events on the directional swipe clips.
public sealed class BossController : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int FacingXParameter = Animator.StringToHash("FacingX");
    private static readonly int FacingYParameter = Animator.StringToHash("FacingY");

    [Header("참조")]
    [SerializeField] private Transform target;
    [SerializeField] private BossPatternSelector patternSelector;
    [SerializeField] private Animator animator;

    [Header("패턴 선택 거리")]
    [SerializeField, Min(0.01f)] private float closeRange = 4f;
    [SerializeField, Min(0.01f)] private float specialPatternDistance = 12f;

    [Header("일반 패턴 간격")]
    [SerializeField, Min(0f)] private float pattern1ToPattern2Delay = 1f;
    [SerializeField, Min(0f)] private float pattern2ToPattern1Delay = 3f;
    [SerializeField, Min(0f)] private float doubleSlashInterval = 0.5f;

    [Header("패턴 데미지 설정")]
    [SerializeField, Min(0f)] private float damage = 10f;
    [Header("2페이지 스텟 배수")]
    [SerializeField, Min(0f)] private float Multiple2 = 1.5f;

    [Header("2페이즈 원형 특수패턴")]
    [Tooltip("일반 패턴 순서와 관계없이 원형 특수패턴을 선택하는 거리입니다. 최솟값 초과, 최댓값 이하이며 돌진 조건이 겹치면 돌진이 우선입니다.")]
    [SerializeField] private Vector2 phaseTwoAreaDistance = new Vector2(4f, 14f);
    [SerializeField, Min(0.01f)] private float phaseTwoAreaRadius = 8f;
    [SerializeField, Min(0f)] private float phaseTwoAreaDelay = 0.8f;
    [Tooltip("원형 공격 종료 후 재사용까지의 시간입니다. 일반 패턴 1·2 완료 조건도 함께 적용됩니다.")]
    [SerializeField, Min(0f)] private float phaseTwoAreaCooldown = 3f;


    [Header("전방 할퀴기")]
    [SerializeField, Min(0.01f)] private float slashRange = 2f;
    [SerializeField, Range(1f, 360f)] private float slashAngle = 90f;

    [Header("특수 돌진 패턴")]
    [Tooltip("플레이어를 향해 이동하는 기본 진행 시간입니다. 돌진은 이 시간이 지나도 근거리까지 계속 추적합니다.")]
    [SerializeField, Min(0.01f)] private float dashDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float dashEndDamageRadius = 2f;

    private EnemyFollow follow;
    private EnemyHealth health;
    private Rigidbody2D body;
    private BossAttackHitVfx hitVfx;
    private Coroutine patternRoutine;
    private BossPattern nextNormalPattern = BossPattern.Pattern1;
    private float nextPatternTime;
    private float nextAreaPatternTime;
    private bool areaFollowupPending;
    private bool phaseEntryAreaPending;
    private bool attackAnimationRunning;
    private bool attackAnimationFinished;
    private bool attackHitApplied;
    private float activeSlashRange;
    private float activeAreaRadius;
    private bool locomotionMoving;
    private bool dashAnimationRunning;
    private int phaseSpawnGeneration = -1;
    private Vector2 attackForward;
    private Vector2 facingDirection = Vector2.down;
    private bool entranceQueued;
    private bool entranceRunning;
    private SpriteRenderer[] entranceVisuals;
    private bool[] entranceRenderingStates;
    private Collider2D[] entranceColliders;
    private bool[] entranceColliderStates;

    // Stop just inside melee range, beyond the movement sweep's tiny-step cutoff.
    private float FollowStoppingDistance => Mathf.Max(0f, closeRange - 0.001f);

    public Transform Target => target;
    public float CloseRange => closeRange;
    public float SpecialPatternDistance => specialPatternDistance;
    public float SlashRange => slashRange;
    public float SlashAngle => slashAngle;
    public float DashEndDamageRadius => dashEndDamageRadius;
    public Vector2 PhaseTwoAreaDistance => phaseTwoAreaDistance;
    public float PhaseTwoAreaRadius => phaseTwoAreaRadius;
    public BossPattern ActivePattern { get; private set; } = BossPattern.None;
    public int CurrentPhase { get; private set; } = 1;
    public bool IsEntering => entranceRunning;

    /// <summary>Encounter가 비활성 보스를 켜기 전에 호출합니다.</summary>
    public void QueueEntrance() => entranceQueued = true;

#if UNITY_EDITOR
    private Mesh slashRangeGizmoMesh;
    private Mesh dashEndRangeGizmoMesh;
    private float cachedSlashRange = -1f;
    private float cachedSlashAngle = -1f;
    private float cachedDashEndRadius = -1f;
#endif

    private void Awake()
    {
        follow = GetComponent<EnemyFollow>();
        health = GetComponent<EnemyHealth>();
        body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (patternSelector == null) patternSelector = GetComponent<BossPatternSelector>();
        hitVfx = GetComponent<BossAttackHitVfx>();
    }

    private void OnEnable()
    {
        if (health == null || health.IsDead)
        {
            enabled = false;
            return;
        }

        if (patternSelector == null)
        {
            Debug.LogWarning("BossController requires a BossPatternSelector.", this);
            enabled = false;
            return;
        }

        health.Died += OnDied;
        follow.SetTarget(target, FollowStoppingDistance);
        follow.enabled = true;
        nextNormalPattern = BossPattern.Pattern1;
        areaFollowupPending = false;
        nextPatternTime = Time.time;
        locomotionMoving = false;
        UpdateAnimatorLocomotion();
        if (entranceQueued)
        {
            entranceQueued = false;
            patternRoutine = StartCoroutine(EntranceThenPatternLoop());
        }
        else patternRoutine = StartCoroutine(PatternLoop());
    }

    private IEnumerator EntranceThenPatternLoop()
    {
        entranceRunning = true;
        follow.enabled = false;
        health.EnsureInitialized();
        // 기존 VFX 자식은 유지하고 보스 모습과 몸체 판정만 숨깁니다.
        entranceVisuals = GetComponentsInChildren<SpriteRenderer>(true);
        entranceRenderingStates = new bool[entranceVisuals.Length];
        for (int i = 0; i < entranceVisuals.Length; i++)
        {
            entranceRenderingStates[i] = entranceVisuals[i].forceRenderingOff;
            if (entranceVisuals[i].GetComponent<HitFlashExcluded>() == null)
                entranceVisuals[i].forceRenderingOff = true;
        }
        entranceColliders = GetComponentsInChildren<Collider2D>(true);
        entranceColliderStates = new bool[entranceColliders.Length];
        for (int i = 0; i < entranceColliders.Length; i++)
        {
            entranceColliderStates[i] = entranceColliders[i].enabled;
            entranceColliders[i].enabled = false;
        }
        try
        {
            // VFX의 Awake가 끝난 뒤 기존 원형 예고를 실행합니다.
            yield return null;
            ActivePattern = BossPattern.PhaseTwoArea;
            yield return PerformPhaseTwoAreaAttack();
        }
        finally
        {
            RevealEntrance();
            ActivePattern = BossPattern.None;
        }
        if (!isActiveAndEnabled || health.IsDead) yield break;
        follow.SetTarget(target, FollowStoppingDistance);
        follow.enabled = true;
        yield return PatternLoop();
    }

    private void RevealEntrance()
    {
        if (!entranceRunning) return;
        entranceRunning = false;
        if (entranceVisuals != null)
            for (int i = 0; i < entranceVisuals.Length; i++)
                if (entranceVisuals[i] != null) entranceVisuals[i].forceRenderingOff = entranceRenderingStates[i];
        if (entranceColliders != null)
            for (int i = 0; i < entranceColliders.Length; i++)
                if (entranceColliders[i] != null && !health.IsDead)
                    entranceColliders[i].enabled = entranceColliderStates[i];
        entranceVisuals = null;
        entranceColliders = null;
    }

    private void Update()
    {
        UpdatePhase();
        UpdateAnimatorLocomotion();
    }

    private void UpdatePhase()
    {
        if (health == null) return;

        if (phaseSpawnGeneration != health.SpawnGeneration)
        {
            phaseSpawnGeneration = health.SpawnGeneration;
            CurrentPhase = 1;
            nextAreaPatternTime = 0f;
            areaFollowupPending = false;
            phaseEntryAreaPending = false;
        }

        if (health.IsDead || CurrentPhase >= 2 || health.CurrentHealth > health.MaxHealth * 0.5f)
            return;

        CurrentPhase = 2;
        damage = damage * Multiple2;
        follow.moveSpeed = follow.moveSpeed * Multiple2;
        pattern2ToPattern1Delay = 1.5f;
        Debug.Log("2페이지 돌입!", this);
        phaseEntryAreaPending = true;
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }
        StopDashAnimation();
        attackAnimationRunning = false;
        attackAnimationFinished = false;
        ActivePattern = BossPattern.None;
        if (hitVfx != null) hitVfx.Hide();
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
        {
            int idleHash = Animator.StringToHash("Base Layer.BossIdle");
            if (animator.HasState(0, idleHash)) animator.Play(idleHash, 0, 0f);
        }
        follow.SetTarget(target, FollowStoppingDistance);
        follow.enabled = true;
        patternRoutine = StartCoroutine(PatternLoop());
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (follow != null) follow.SetTarget(target, FollowStoppingDistance);
    }

    private IEnumerator PatternLoop()
    {
        while (isActiveAndEnabled && !health.IsDead)
        {
            if (target == null || !target.gameObject.activeInHierarchy || Time.timeScale <= 0f ||
                (!phaseEntryAreaPending && (!patternSelector.isActiveAndEnabled || Time.time < nextPatternTime)))
            {
                yield return null;
                continue;
            }

            float distance = Vector2.Distance(body.position, target.position);
            BossPattern selected = SelectAvailablePattern(distance);
            if (selected == BossPattern.None)
            {
                // 근거리와 특수 패턴 거리 사이에서는 공격하지 않고 플레이어를 추적한다.
                yield return null;
                continue;
            }

            switch (selected)
            {
                case BossPattern.Pattern1:
                    ActivePattern = BossPattern.Pattern1;
                    yield return PlaySlashAnimation(false);
                    ActivePattern = BossPattern.None;
                    nextNormalPattern = BossPattern.Pattern2;
                    nextPatternTime = Time.time + pattern1ToPattern2Delay;
                    break;

                case BossPattern.Pattern2:
                    ActivePattern = BossPattern.Pattern2;
                    yield return PlaySlashAnimation(false);
                    if (CurrentPhase < 2 && doubleSlashInterval > 0f)
                        yield return new WaitForSeconds(doubleSlashInterval);
                    if (CanAttackTarget()) yield return PlaySlashAnimation(true);
                    ActivePattern = BossPattern.None;
                    nextNormalPattern = BossPattern.Pattern1;
                    areaFollowupPending = false;
                    nextPatternTime = Time.time + pattern2ToPattern1Delay;
                    break;

                case BossPattern.PhaseTwoArea:
                    ActivePattern = BossPattern.PhaseTwoArea;
                    yield return PerformPhaseTwoAreaAttack();
                    ActivePattern = BossPattern.None;
                    nextAreaPatternTime = Time.time + phaseTwoAreaCooldown;
                    nextNormalPattern = BossPattern.Pattern1;
                    areaFollowupPending = true;
                    // 원형 공격 뒤에는 근거리에서 일반 패턴 1과 2를 먼저 완료한다.
                    nextPatternTime = Time.time;
                    break;

                case BossPattern.Pattern3:
                    ActivePattern = BossPattern.Pattern3;
                    Debug.Log("돌진 시작!", this);
                    yield return DashToTrackedTarget();
                    ActivePattern = BossPattern.None;
                    // 특수 패턴 전에 대기 중이던 일반 패턴을 이어간다.
                    nextPatternTime = Time.time;
                    break;
            }
        }
    }

    private BossPattern SelectAvailablePattern(float distance)
    {
        if (phaseEntryAreaPending)
        {
            phaseEntryAreaPending = false;
            return BossPattern.PhaseTwoArea;
        }

        if (distance >= specialPatternDistance)
            return BossPattern.Pattern3;

        if (areaFollowupPending)
            return distance <= closeRange ? nextNormalPattern : BossPattern.None;

        if (CurrentPhase >= 2 && Time.time >= nextAreaPatternTime &&
            distance > phaseTwoAreaDistance.x && distance <= phaseTwoAreaDistance.y)
            return BossPattern.PhaseTwoArea;

        if (distance > closeRange)
            return BossPattern.None;

        return patternSelector.SelectPattern(distance, (int)health.CurrentHealth, nextNormalPattern);
    }

    private IEnumerator PerformPhaseTwoAreaAttack()
    {
        if (!CanAttackTarget()) yield break;
        bool wasFollowing = follow.enabled;
        follow.enabled = false;
        UpdateAnimatorLocomotion();
        float radius = phaseTwoAreaRadius;
        try
        {
            if (hitVfx != null) hitVfx.ShowArea(radius);
            if (phaseTwoAreaDelay > 0f)
                yield return new WaitForSeconds(phaseTwoAreaDelay);
            if (!CanAttackTarget()) yield break;
            activeAreaRadius = radius;
            yield return PlaySlashAnimation(false, true);
        }
        finally
        {
            if (hitVfx != null) hitVfx.HideArea();
            if (wasFollowing && isActiveAndEnabled && CanAttackTarget())
            {
                follow.SetTarget(target, FollowStoppingDistance);
                follow.enabled = true;
            }
        }
    }

    private IEnumerator PlaySlashAnimation(bool useLeftHand, bool useArea = false)
    {
        if (!CanAttackTarget()) yield break;

        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
        {
            Debug.LogError("Boss attack animation requires an enabled Animator with the Boss Animator Controller.", this);
            yield break;
        }

        Vector2 targetOffset = (Vector2)target.position - body.position;
        if (targetOffset.sqrMagnitude <= Mathf.Epsilon && !useArea) yield break;

        // Lock facing at the start so the visible attack and hit arc stay aligned during windup.
        attackForward = targetOffset.sqrMagnitude > Mathf.Epsilon ? targetOffset.normalized : Vector2.right;
        string direction = GetAttackDirection(attackForward);
        string stateName = useArea ? "Base Layer.BossSwipeBoth" :
            useLeftHand ? $"Base Layer.Boss_SwipeLeft_{direction}" : $"Base Layer.Boss_Swipe_{direction}";
        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogError($"Boss Animator Controller is missing state '{stateName}'.", this);
            yield break;
        }

        bool wasFollowing = follow.enabled;
        follow.enabled = false;
        attackAnimationRunning = true;
        attackAnimationFinished = false;
        attackHitApplied = false;
        activeSlashRange = slashRange;
        if (!useArea && useLeftHand && CurrentPhase >= 2 && ActivePattern == BossPattern.Pattern2)
            activeSlashRange *= 1.5f;
        if (!useArea && hitVfx != null) hitVfx.ShowSlash(attackForward, activeSlashRange, slashAngle);
        try
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);

            while (CanAttackTarget() && !attackAnimationFinished)
            {
                if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
                    break;
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash != stateHash || state.normalizedTime >= 1f)
                    break;
                if (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).fullPathHash != stateHash)
                    break;
                yield return null;
            }
        }
        finally
        {
            attackAnimationRunning = false;
            if (!useArea && hitVfx != null) hitVfx.HideSlash();
            if (wasFollowing && isActiveAndEnabled && CanAttackTarget())
            {
                follow.SetTarget(target, FollowStoppingDistance);
                follow.enabled = true;
            }
        }
    }
    /// <summary>Animation Event at the impact frame of each swipe clip.</summary>
    public void AnimationEvent_ApplySlashDamage()
    {
        if (!attackAnimationRunning || attackHitApplied || ActivePattern == BossPattern.PhaseTwoArea ||
            !CanAttackTarget()) return;
        attackHitApplied = true;
        PerformForwardSlash(attackForward, activeSlashRange);
    }

    public void AnimationEvent_ApplyAreaDamage()
    {
        if (!isActiveAndEnabled || !attackAnimationRunning || attackHitApplied ||
            ActivePattern != BossPattern.PhaseTwoArea || !CanAttackTarget()) return;
        attackHitApplied = true;
        bool isEntranceImpact = entranceRunning;
        RevealEntrance();
        if (hitVfx != null)
        {
            if (isEntranceImpact) hitVfx.ShowEntranceBurst(activeAreaRadius);
            else hitVfx.ShowAreaBurst(activeAreaRadius);
        }
        ApplyCircleDamage(body.position, activeAreaRadius);
    }

    /// <summary>Animation Event at the end of each swipe clip.</summary>
    public void AnimationEvent_AttackAnimationFinished()
    {
        if (attackAnimationRunning) attackAnimationFinished = true;
    }

    private void PerformForwardSlash(Vector2 forward, float radius)
    {
        if (!CanAttackTarget()) return;

        Vector2 origin = body.position;
        float minimumDot = Mathf.Cos(Mathf.Clamp(slashAngle, 1f, 360f) * 0.5f * Mathf.Deg2Rad);
        var damagedReceivers = new HashSet<IContactDamageReceiver>();

        foreach (Collider2D hitCollider in Physics2D.OverlapCircleAll(origin, radius))
        {
            if (hitCollider == null) continue;
            var receiver = hitCollider.GetComponentInParent(typeof(IContactDamageReceiver)) as IContactDamageReceiver;
            if (receiver == null || !damagedReceivers.Add(receiver)) continue;

            Vector2 offset = (Vector2)hitCollider.bounds.center - origin;
            if (offset.sqrMagnitude > radius * radius) continue;
            if (offset.sqrMagnitude > Mathf.Epsilon && Vector2.Dot(forward, offset.normalized) < minimumDot) continue;
            receiver.TakeContactDamage(damage, gameObject);
        }
    }

    private static string GetAttackDirection(Vector2 forward)
    {
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        int octant = Mathf.RoundToInt(angle / 45f) % 8;
        switch (octant)
        {
            case 0: return "East";
            case 1: return "NorthEast";
            case 2: return "North";
            case 3: return "NorthWest";
            case 4: return "West";
            case 5: return "SouthWest";
            case 6: return "South";
            default: return "SouthEast";
        }
    }

    private void UpdateAnimatorLocomotion()
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled) return;

        bool hasTarget = target != null && target.gameObject.activeInHierarchy;
        float distance = hasTarget ? Vector2.Distance(body.position, target.position) : 0f;
        if (hasTarget && !(attackAnimationRunning && ActivePattern == BossPattern.PhaseTwoArea))
        {
            Vector2 offset = (Vector2)target.position - body.position;
            if (offset.sqrMagnitude > Mathf.Epsilon)
                facingDirection = GetOctantDirection(offset);
        }

        animator.SetFloat(FacingXParameter, facingDirection.x);
        animator.SetFloat(FacingYParameter, facingDirection.y);
        if (!hasTarget || !follow.enabled)
        {
            locomotionMoving = false;
        }
        else if (ActivePattern == BossPattern.Pattern3)
        {
            locomotionMoving = true;
        }
        else if (locomotionMoving)
        {
            locomotionMoving = distance > closeRange + 0.02f;
        }
        else
        {
            locomotionMoving = distance > closeRange + 0.1f;
        }

        animator.SetBool(IsMovingParameter, locomotionMoving);
    }

    private static Vector2 GetOctantDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int octant = Mathf.RoundToInt(angle / 45f);
        float quantizedAngle = octant * 45f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(quantizedAngle), Mathf.Sin(quantizedAngle));
    }

    private IEnumerator DashToTrackedTarget()
    {
        if (!CanAttackTarget()) yield break;

        follow.enabled = false;
        StartDashAnimation();
        Vector2 dashStartPosition = body.position;
        float elapsed = 0f;

        while (CanAttackTarget() && Vector2.Distance(body.position, target.position) > closeRange)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(elapsed / dashDuration);
            Vector2 trackedTargetPosition = target.position;
            Vector2 nextPosition = Vector2.Lerp(dashStartPosition, trackedTargetPosition, progress);
            if (follow.MoveTo(nextPosition).sqrMagnitude < 0.00000001f)
                break; // A solid body blocks the dash; do not wait forever behind it.
        }

        // 플레이어와 적의 충돌을 유지한 상태로 마지막 돌진 이동을 처리한다.
        yield return new WaitForFixedUpdate();
        bool reachedTarget = CanAttackTarget() && Vector2.Distance(body.position, target.position) <= closeRange;
        if (reachedTarget) ApplyDashEndDamage(target.position);
        StopDashAnimation();

        follow.SetTarget(target, FollowStoppingDistance);
        follow.enabled = true;
    }

    private void StartDashAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            return;

        int stateHash = Animator.StringToHash("Base Layer.body-charge");
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning("Boss Animator Controller is missing the body-charge state.", this);
            return;
        }

        UpdateAnimatorLocomotion();
        dashAnimationRunning = true;
        animator.Play(stateHash, 0, 0f);
    }

    private void StopDashAnimation()
    {
        if (!dashAnimationRunning) return;
        dashAnimationRunning = false;
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            return;

        int idleHash = Animator.StringToHash("Base Layer.BossIdle");
        if (animator.HasState(0, idleHash)) animator.Play(idleHash, 0, 0f);
    }

    private void ApplyDashEndDamage(Vector2 dashEndCenter)
    {
        if (hitVfx != null) hitVfx.ShowDashImpact(dashEndCenter, dashEndDamageRadius);
        ApplyCircleDamage(dashEndCenter, dashEndDamageRadius);
    }

    private void ApplyCircleDamage(Vector2 center, float radius)
    {
        var damagedReceivers = new HashSet<IContactDamageReceiver>();
        foreach (Collider2D hitCollider in Physics2D.OverlapCircleAll(center, radius))
        {
            if (hitCollider == null) continue;
            var receiver = hitCollider.GetComponentInParent(typeof(IContactDamageReceiver)) as IContactDamageReceiver;
            if (receiver != null && damagedReceivers.Add(receiver))
                receiver.TakeContactDamage(damage, gameObject);
        }
    }

    private bool CanAttackTarget()
    {
        return target != null && target.gameObject.activeInHierarchy && !health.IsDead;
    }

    private void OnDied()
    {
        StopDashAnimation();
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }
        attackAnimationRunning = false;
        locomotionMoving = false;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetBool(IsMovingParameter, false);
        enabled = false;
    }

    private void OnDisable()
    {
        RevealEntrance();
        entranceQueued = false;
        StopDashAnimation();
        ActivePattern = BossPattern.None;
        if (health != null) health.Died -= OnDied;
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }
        attackAnimationRunning = false;
        attackAnimationFinished = false;
        locomotionMoving = false;
        if (hitVfx != null) hitVfx.Hide();
        if (follow != null) follow.enabled = false;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetBool(IsMovingParameter, false);
    }

    private void OnValidate()
    {
        closeRange = Mathf.Max(0.01f, closeRange);
        specialPatternDistance = Mathf.Max(closeRange + 0.01f, specialPatternDistance);
        pattern1ToPattern2Delay = Mathf.Max(0f, pattern1ToPattern2Delay);
        pattern2ToPattern1Delay = Mathf.Max(0f, pattern2ToPattern1Delay);
        doubleSlashInterval = Mathf.Max(0f, doubleSlashInterval);
        phaseTwoAreaDistance.x = Mathf.Max(0f, phaseTwoAreaDistance.x);
        phaseTwoAreaDistance.y = Mathf.Max(phaseTwoAreaDistance.x, phaseTwoAreaDistance.y);
        phaseTwoAreaRadius = Mathf.Max(0.01f, phaseTwoAreaRadius);
        phaseTwoAreaDelay = Mathf.Max(0f, phaseTwoAreaDelay);
        phaseTwoAreaCooldown = Mathf.Max(0f, phaseTwoAreaCooldown);
        slashRange = Mathf.Max(0.01f, slashRange);
        slashAngle = Mathf.Clamp(slashAngle, 1f, 360f);
        damage = Mathf.Max(0f, damage);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashEndDamageRadius = Mathf.Max(0.01f, dashEndDamageRadius);
#if UNITY_EDITOR
        cachedSlashRange = -1f;
        cachedSlashAngle = -1f;
        cachedDashEndRadius = -1f;
#endif
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        EnsureRangeGizmoMeshes();
        if (slashRangeGizmoMesh == null || dashEndRangeGizmoMesh == null) return;

        Color previousColor = Gizmos.color;
        Vector2 forward = target != null ? (Vector2)target.position - (Vector2)transform.position : Vector2.right;
        if (forward.sqrMagnitude <= Mathf.Epsilon) forward = Vector2.right;
        Quaternion slashRotation = Quaternion.FromToRotation(Vector3.right, forward.normalized);

        Vector3 dashEndCenter = target != null ? target.position : transform.position;
        Gizmos.color = new Color(1f, 0f, 0f, 0.85f);
        DrawCircleRangeOutline(transform.position, closeRange);
        DrawCircleRangeOutline(transform.position, specialPatternDistance);

        Gizmos.color = new Color(1f, 0f, 0f, 0.22f);
        Gizmos.DrawMesh(slashRangeGizmoMesh, transform.position, slashRotation);
        Gizmos.DrawMesh(dashEndRangeGizmoMesh, dashEndCenter);

        Gizmos.color = new Color(1f, 0f, 0f, 0.85f);
        DrawSlashRangeOutline(transform.position, forward.normalized, slashRange, slashAngle);
        DrawCircleRangeOutline(dashEndCenter, dashEndDamageRadius);
        Gizmos.color = previousColor;
    }

    private void EnsureRangeGizmoMeshes()
    {
        if (slashRangeGizmoMesh == null || !Mathf.Approximately(cachedSlashRange, slashRange) ||
            !Mathf.Approximately(cachedSlashAngle, slashAngle))
        {
            if (slashRangeGizmoMesh == null)
                slashRangeGizmoMesh = new Mesh { name = "Boss Slash Range Gizmo", hideFlags = HideFlags.HideAndDontSave };
            UpdateSectorMesh(slashRangeGizmoMesh, slashRange, slashAngle);
            cachedSlashRange = slashRange;
            cachedSlashAngle = slashAngle;
        }

        if (dashEndRangeGizmoMesh == null || !Mathf.Approximately(cachedDashEndRadius, dashEndDamageRadius))
        {
            if (dashEndRangeGizmoMesh == null)
                dashEndRangeGizmoMesh = new Mesh { name = "Boss Dash End Range Gizmo", hideFlags = HideFlags.HideAndDontSave };
            UpdateSectorMesh(dashEndRangeGizmoMesh, dashEndDamageRadius, 360f);
            cachedDashEndRadius = dashEndDamageRadius;
        }
    }

    private static void UpdateSectorMesh(Mesh mesh, float radius, float angle)
    {
        const int segmentCount = 48;
        var vertices = new List<Vector3>(segmentCount + 2) { Vector3.zero };
        var triangles = new List<int>(segmentCount * 6);
        int actualSegments = Mathf.Max(1, Mathf.CeilToInt(segmentCount * angle / 360f));

        for (int i = 0; i <= actualSegments; i++)
        {
            float currentAngle = (-angle * 0.5f + angle * i / actualSegments) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(currentAngle) * radius, Mathf.Sin(currentAngle) * radius, 0f));
        }

        for (int i = 0; i < actualSegments; i++)
        {
            int first = i + 1;
            int second = i + 2;
            // 양면에서 보이도록 정방향과 역방향 삼각형을 모두 만든다.
            triangles.Add(0);
            triangles.Add(first);
            triangles.Add(second);
            triangles.Add(0);
            triangles.Add(second);
            triangles.Add(first);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private static void DrawSlashRangeOutline(Vector3 center, Vector2 forward, float radius, float angle)
    {
        const int segmentCount = 24;
        Vector2 startDirection = Quaternion.Euler(0f, 0f, -angle * 0.5f) * forward;
        Vector3 previousPoint = center + (Vector3)(startDirection * radius);
        Vector3 firstPoint = previousPoint;

        for (int i = 1; i <= segmentCount; i++)
        {
            float stepAngle = angle * i / segmentCount;
            Vector2 direction = Quaternion.Euler(0f, 0f, stepAngle) * startDirection;
            Vector3 nextPoint = center + (Vector3)(direction * radius);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }

        Gizmos.DrawLine(center, firstPoint);
        Gizmos.DrawLine(center, previousPoint);
    }

    private static void DrawCircleRangeOutline(Vector3 center, float radius)
    {
        const int segmentCount = 48;
        Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = 360f * i / segmentCount * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    private void OnDestroy()
    {
        Mesh slashMesh = slashRangeGizmoMesh;
        Mesh dashEndMesh = dashEndRangeGizmoMesh;
        slashRangeGizmoMesh = null;
        dashEndRangeGizmoMesh = null;

        if (Application.isPlaying)
        {
            if (slashMesh != null) Destroy(slashMesh);
            if (dashEndMesh != null) Destroy(dashEndMesh);
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (slashMesh != null) DestroyImmediate(slashMesh);
            if (dashEndMesh != null) DestroyImmediate(dashEndMesh);
        };
    }
#endif
}
