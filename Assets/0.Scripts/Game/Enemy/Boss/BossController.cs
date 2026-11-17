using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("2페이즈 테두리")]
    [SerializeField] private Material phaseTwoOutlineMaterial;

    [Header("일반 패턴 선택 거리")]
    [SerializeField, Min(0.01f)] private float closeRange = 4f;
    [SerializeField, Min(0.01f)] private float specialPatternDistance = 12f;

    [Header("일반 패턴 간격")]
    [SerializeField, Min(0f)] private float pattern1ToPattern2Delay = 1f;
    [SerializeField, Min(0f)] private float pattern2ToPattern1Delay = 3f;
    [SerializeField, Min(0f)] private float doubleSlashInterval = 0.5f;

    [Header("패턴 데미지 설정")]
    [SerializeField, Min(0f)] private float damage = 30f;
    [Header("2페이지 스텟 배수")]
    [SerializeField, Min(0f)] private float Multiple2 = 1.5f;

    [Header("2페이즈 원형 특수패턴")]
    [SerializeField, Min(0.01f)] private float phaseTwoAreaRadius = 8f;
    [Tooltip("안쪽 원이 최대 범위까지 커지는 시간입니다. 완전히 차면 공격 모션과 피해가 발생합니다.")]
    [SerializeField, Min(0f)] private float phaseTwoAreaDelay = 0.8f;
    [Tooltip("원형 공격 종료 후 재사용까지의 시간입니다. 일반 패턴 1·2 완료 조건도 함께 적용됩니다.")]
    [SerializeField, Min(0f)] private float phaseTwoAreaCooldown = 3f;

    [Header("등장 원형 공격")]
    [Tooltip("등장할 때만 사용하는 예고·피해 반경입니다. 피해량은 패턴 Damage를 사용합니다.")]
    [SerializeField, Min(0.01f)] private float entranceAreaRadius = 3.5f;


    [Header("전방 할퀴기")]
    [SerializeField, Min(0.01f)] private float slashRange = 2f;
    [SerializeField, Range(1f, 360f)] private float slashAngle = 90f;

    [Header("특수 돌진 패턴")]
    [Tooltip("돌진 도착을 판단하는 보스·플레이어 중심 거리입니다. 일반 공격의 Close Range와 독립적입니다.")]
    [SerializeField, Min(0.01f)] private float dashArrivalDistance = 4f;
    [Tooltip("돌진 이동의 기본 진행 시간입니다. 2페이즈는 이 값의 절반을 사용하며, 시간이 지나도 도착 거리까지 추적합니다.")]
    [SerializeField, Min(0.01f)] private float dashDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float dashEndDamageRadius = 2f;
    [Tooltip("공격 없이 추격만 이 시간 이상 이어지면 돌진합니다. 근거리 안에서는 돌진하지 않습니다.")]
    [SerializeField, Min(0.5f)] private float chaseDashDelay = 2.5f;

    private EnemyFollow follow;
    private EnemyHealth health;
    private Rigidbody2D body;
    private BossAttackHitVfx hitVfx;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer phaseTwoOutline;
    private Coroutine patternRoutine;
    private BossPattern nextNormalPattern = BossPattern.Pattern1;
    private float nextPatternTime;
    private float nextAreaPatternTime;
    private bool missedComboAreaPending;
    private bool normalComboDealtDamageOrWasParried;
    private bool firstComboSwipeResolved;
    private float baseMoveSpeed;
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
    private float chaseOnlyTime;
    private SpriteRenderer[] entranceVisuals;
    private bool[] entranceRenderingStates;
    private Collider2D[] entranceColliders;
    private bool[] entranceColliderStates;

    // Stop just inside melee range, beyond the movement sweep's tiny-step cutoff.
    private float FollowStoppingDistance => Mathf.Max(0f, closeRange - 0.001f);
    private float AttackDamage => CurrentPhase >= 2 ? damage * Multiple2 : damage;

    public Transform Target => target;
    public float CloseRange => closeRange;
    public float SpecialPatternDistance => specialPatternDistance;
    public float SlashRange => slashRange;
    public float SlashAngle => slashAngle;
    public float DashEndDamageRadius => dashEndDamageRadius;
    public float PhaseTwoAreaRadius => phaseTwoAreaRadius;
    public BossPattern ActivePattern { get; private set; } = BossPattern.None;
    public int CurrentPhase { get; private set; } = 1;
    public bool IsEntering => entranceRunning;

    /// <summary>Encounter가 비활성 보스를 켜기 전에 호출합니다.</summary>
    public void QueueEntrance() => entranceQueued = true;


    private void Awake()
    {
        follow = GetComponent<EnemyFollow>();
        baseMoveSpeed = follow.moveSpeed;
        follow.DisableHitKnockback();
        health = GetComponent<EnemyHealth>();
        body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (patternSelector == null) patternSelector = GetComponent<BossPatternSelector>();
        hitVfx = GetComponent<BossAttackHitVfx>();
        bodyRenderer = GetComponent<SpriteRenderer>();
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

        ResetPhaseForSpawn();
        health.Died += OnDied;
        if (CurrentPhase == 2)
            SetPhaseTwoOutline(true);
        follow.SetTarget(target, FollowStoppingDistance);
        follow.enabled = true;
        nextNormalPattern = BossPattern.Pattern1;
        missedComboAreaPending = false;
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

    private void ResetPhaseForSpawn()
    {
        if (phaseSpawnGeneration == health.SpawnGeneration) return;
        phaseSpawnGeneration = health.SpawnGeneration;
        CurrentPhase = 1;
        follow.moveSpeed = baseMoveSpeed;
        SetPhaseTwoOutline(false);
        nextAreaPatternTime = 0f;
        missedComboAreaPending = false;
        phaseEntryAreaPending = false;
    }

    private void UpdatePhase()
    {
        if (health == null) return;
        ResetPhaseForSpawn();
        if (health.IsDead || CurrentPhase >= 2 || health.CurrentHealth > health.MaxHealth * 0.5f)
            return;

        CurrentPhase = 2;
        SetPhaseTwoOutline(true);
        follow.moveSpeed = baseMoveSpeed * Multiple2;
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

    private void SetPhaseTwoOutline(bool visible)
    {
        if (visible && phaseTwoOutline == null && bodyRenderer != null && phaseTwoOutlineMaterial != null)
        {
            var visual = new GameObject("Phase Two Outline");
            visual.layer = bodyRenderer.gameObject.layer;
            visual.transform.SetParent(bodyRenderer.transform, false);
            visual.AddComponent<HitFlashExcluded>();
            phaseTwoOutline = visual.AddComponent<SpriteRenderer>();
            phaseTwoOutline.sharedMaterial = phaseTwoOutlineMaterial;
        }

        if (phaseTwoOutline != null) phaseTwoOutline.enabled = visible;
    }

    private void LateUpdate()
    {
        if (phaseTwoOutline == null || !phaseTwoOutline.enabled || bodyRenderer == null) return;

        phaseTwoOutline.sprite = bodyRenderer.sprite;
        phaseTwoOutline.flipX = bodyRenderer.flipX;
        phaseTwoOutline.flipY = bodyRenderer.flipY;
        phaseTwoOutline.sortingLayerID = bodyRenderer.sortingLayerID;
        phaseTwoOutline.sortingOrder = bodyRenderer.sortingOrder - 1;
        phaseTwoOutline.maskInteraction = bodyRenderer.maskInteraction;
        phaseTwoOutline.forceRenderingOff = bodyRenderer.forceRenderingOff || !bodyRenderer.enabled;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (follow != null) follow.SetTarget(target, FollowStoppingDistance);
    }

    private IEnumerator PatternLoop()
    {
        chaseOnlyTime = 0f;
        while (isActiveAndEnabled && !health.IsDead)
        {
            if (target == null || !target.gameObject.activeInHierarchy || Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            float distance = Vector2.Distance(body.position, target.position);
            bool isPhaseEntryArea = phaseEntryAreaPending;
            bool patternReady = isPhaseEntryArea ||
                (patternSelector.isActiveAndEnabled && Time.time >= nextPatternTime);
            BossPattern selected = patternReady ? SelectAvailablePattern(distance) : BossPattern.None;
            if (selected == BossPattern.None)
            {
                if (distance > closeRange) chaseOnlyTime += Time.deltaTime;
                else chaseOnlyTime = 0f;
                if (missedComboAreaPending || chaseOnlyTime < chaseDashDelay || distance <= closeRange || distance >= specialPatternDistance)
                {
                    yield return null;
                    continue;
                }
                selected = BossPattern.Pattern3;
            }
            chaseOnlyTime = 0f;

            switch (selected)
            {
                case BossPattern.Pattern1:
                    normalComboDealtDamageOrWasParried = false;
                    ActivePattern = BossPattern.Pattern1;
                    yield return PlaySlashAnimation(false);
                    firstComboSwipeResolved = attackHitApplied;
                    ActivePattern = BossPattern.None;
                    nextNormalPattern = BossPattern.Pattern2;
                    nextPatternTime = Time.time + pattern1ToPattern2Delay;
                    break;

                case BossPattern.Pattern2:
                    ActivePattern = BossPattern.Pattern2;
                    yield return PlaySlashAnimation(false);
                    bool rightSwipeResolved = attackHitApplied;
                    if (CurrentPhase < 2 && doubleSlashInterval > 0f)
                        yield return new WaitForSeconds(doubleSlashInterval);
                    yield return PlaySlashAnimation(true);
                    ActivePattern = BossPattern.None;
                    CompleteNormalCombo(firstComboSwipeResolved && rightSwipeResolved && attackHitApplied);
                    nextPatternTime = Time.time + (CurrentPhase >= 2 ? 1.5f : pattern2ToPattern1Delay);
                    break;

                case BossPattern.PhaseTwoArea:
                    ActivePattern = BossPattern.PhaseTwoArea;
                    yield return PerformPhaseTwoAreaAttack(isPhaseEntryArea);
                    ActivePattern = BossPattern.None;
                    nextAreaPatternTime = Time.time + phaseTwoAreaCooldown;
                    nextNormalPattern = BossPattern.Pattern1;
                    missedComboAreaPending = false;
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

    private void CompleteNormalCombo(bool allSwipesResolved)
    {
        nextNormalPattern = BossPattern.Pattern1;
        missedComboAreaPending = CurrentPhase >= 2 && allSwipesResolved && !normalComboDealtDamageOrWasParried && CanAttackTarget();
    }

    private BossPattern SelectAvailablePattern(float distance)
    {
        if (phaseEntryAreaPending)
        {
            phaseEntryAreaPending = false;
            return BossPattern.PhaseTwoArea;
        }

        if (missedComboAreaPending)
            return Time.time >= nextAreaPatternTime ? BossPattern.PhaseTwoArea : BossPattern.None;

        if (distance >= specialPatternDistance)
            return BossPattern.Pattern3;

        if (distance > closeRange)
            return BossPattern.None;

        return CurrentPhase >= 2 ? nextNormalPattern :
            patternSelector.SelectPattern(distance, (int)health.CurrentHealth, nextNormalPattern);
    }

    private IEnumerator PerformPhaseTwoAreaAttack(bool isPhaseEntryArea = false)
    {
        if (!CanAttackTarget()) yield break;
        bool wasFollowing = follow.enabled;
        follow.enabled = false;
        UpdateAnimatorLocomotion();
        float radius = entranceRunning ? entranceAreaRadius : phaseTwoAreaRadius;
        try
        {
            if (isPhaseEntryArea) health.SetInvulnerable(true);
            if (hitVfx != null) hitVfx.ShowArea(radius);
            float elapsed = 0f;
            float chargeDuration = Mathf.Max(0f, phaseTwoAreaDelay);
            while (elapsed < chargeDuration)
            {
                if (!CanAttackTarget()) yield break;
                yield return null;
                elapsed += Time.deltaTime;
                if (hitVfx != null) hitVfx.SetAreaChargeProgress(Mathf.Clamp01(elapsed / chargeDuration));
            }
            if (!CanAttackTarget()) yield break;
            if (hitVfx != null) hitVfx.SetAreaChargeProgress(1f);
            activeAreaRadius = radius;
            yield return PlaySlashAnimation(false, true);
        }
        finally
        {
            if (isPhaseEntryArea) health.SetInvulnerable(false);
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
        attackHitApplied = false;
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
        activeSlashRange = slashRange;
        if (!useArea && useLeftHand && CurrentPhase >= 2 && ActivePattern == BossPattern.Pattern2)
            activeSlashRange *= 1.5f;
        if (!useArea && hitVfx != null) hitVfx.ShowSlash(attackForward, activeSlashRange, slashAngle);
        try
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
            // 예고가 완전히 찬 뒤 공격 모션 시작과 피해를 같은 시점에 적용합니다.
            if (useArea) AnimationEvent_ApplyAreaDamage();

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
        Vector2 origin = body.position;
        float minimumDot = Mathf.Cos(Mathf.Clamp(slashAngle, 1f, 360f) * 0.5f * Mathf.Deg2Rad);
        var damagedReceivers = new HashSet<IContactDamageReceiver>();

        foreach (Collider2D hitCollider in Physics2D.OverlapCircleAll(origin, radius))
        {
            if (hitCollider == null) continue;
            var receiver = hitCollider.GetComponentInParent(typeof(IContactDamageReceiver)) as IContactDamageReceiver;
            if (receiver == null) continue;

            Vector2 offset = (Vector2)hitCollider.bounds.center - origin;
            if (offset.sqrMagnitude > radius * radius) continue;
            if (offset.sqrMagnitude > Mathf.Epsilon && Vector2.Dot(forward, offset.normalized) < minimumDot) continue;
            if (!damagedReceivers.Add(receiver)) continue;
            if (receiver is IContactDamageResultReceiver resultReceiver)
                normalComboDealtDamageOrWasParried |= resultReceiver.TryDamageOrTriggerParry(AttackDamage, gameObject);
            else
                receiver.TakeContactDamage(AttackDamage, gameObject);
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
        float effectiveDashDuration = Mathf.Max(0.0001f, dashDuration * (CurrentPhase >= 2 ? 0.5f : 1f));

        try
        {
            while (CanAttackTarget() && Vector2.Distance(body.position, target.position) > dashArrivalDistance)
            {
                yield return new WaitForFixedUpdate();
                if (!CanAttackTarget()) yield break;
                elapsed += Time.fixedDeltaTime;
                float progress = Mathf.Clamp01(elapsed / effectiveDashDuration);
                Vector2 nextPosition = Vector2.Lerp(dashStartPosition, target.position, progress);
                if (follow.MoveTo(nextPosition).sqrMagnitude < 0.00000001f) break;
            }

            // 마지막 이동의 물리 결과가 반영된 뒤 도착 피해를 판정합니다.
            yield return new WaitForFixedUpdate();
            if (CanAttackTarget() && Vector2.Distance(body.position, target.position) <= dashArrivalDistance)
                ApplyDashEndDamage(target.position);
        }
        finally
        {
            StopDashAnimation();
            if (isActiveAndEnabled && !health.IsDead)
            {
                follow.SetTarget(target, FollowStoppingDistance);
                follow.enabled = true;
            }
        }
    }

    private void StartDashAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            return;

        string stateName = CurrentPhase >= 2 ? "Base Layer.BossRun" : "Base Layer.body-charge";
        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning($"Boss Animator Controller is missing state '{stateName}'.", this);
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
                receiver.TakeContactDamage(AttackDamage, gameObject);
        }
    }

    private bool CanAttackTarget()
    {
        return target != null && target.gameObject.activeInHierarchy && !health.IsDead;
    }

    private void OnDied() => enabled = false;

    private void OnDisable()
    {
        SetPhaseTwoOutline(false);
        RevealEntrance();
        entranceQueued = false;
        StopDashAnimation();
        ActivePattern = BossPattern.None;
        if (health != null)
        {
            health.SetInvulnerable(false);
            health.Died -= OnDied;
        }
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
        phaseTwoAreaRadius = Mathf.Max(0.01f, phaseTwoAreaRadius);
        entranceAreaRadius = Mathf.Max(0.01f, entranceAreaRadius);
        phaseTwoAreaDelay = Mathf.Max(0f, phaseTwoAreaDelay);
        phaseTwoAreaCooldown = Mathf.Max(0f, phaseTwoAreaCooldown);
        slashRange = Mathf.Max(0.01f, slashRange);
        slashAngle = Mathf.Clamp(slashAngle, 1f, 360f);
        damage = Mathf.Max(0f, damage);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashArrivalDistance = Mathf.Max(0.01f, dashArrivalDistance);
        dashEndDamageRadius = Mathf.Max(0.01f, dashEndDamageRadius);
    }

}
