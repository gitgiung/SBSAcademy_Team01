using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 거리를 유지하며 기존 일반 몬스터를 소환하고 느린 구체를 던지는 무리군주.
/// 시작 체력의 절반 이하가 되면 소환과 구체가 늘어나는 2페이즈로 바뀐다.
/// 마리 수와 거리는 임시 값이다. 전투 전체를 멈추지 않는다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyFollow), typeof(EnemyHealth), typeof(Rigidbody2D))]
public sealed class HordeLordBoss : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int FacingXParameter = Animator.StringToHash("FacingX");
    private static readonly int FacingYParameter = Animator.StringToHash("FacingY");

    [Header("참조")]
    [SerializeField] private Transform target;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private HordeLordOrb orbPrefab;
    [SerializeField] private MonoBehaviour spawnPoolBehaviour;

    [Header("거리")]
    [SerializeField, Min(0.5f)] private float innerDistance = 3.5f;
        [SerializeField, Min(1f)] private float outerDistance = 7.5f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.1f;
    [SerializeField, Min(0.2f)] private float summonRadius = 1.6f;

    [Header("구체")]
    [SerializeField, Min(0f)] private float orbDamage = 10f;
    [SerializeField, Min(0.1f)] private float orbSpeed = 5f;
    [SerializeField, Min(0.2f)] private float orbLifetime = 3.5f;

    [Header("간격")]
    [SerializeField, Min(0f)] private float phaseOneGap = 2.8f;
    [SerializeField, Min(0f)] private float phaseTwoGap = 1.6f;
    [SerializeField, Min(0.05f)] private float castSeconds = 0.55f;

    private struct TrackedSummon
    {
        public GameObject Enemy;
        public int Generation;
    }

    private readonly List<TrackedSummon> summons = new List<TrackedSummon>();
    private readonly List<HordeLordOrb> orbs = new List<HordeLordOrb>();
    private EnemyFollow follow;
    private EnemyHealth health;
    private Rigidbody2D body;
    private IEnemySpawnPool spawnPool;
    private Coroutine patternRoutine;
    private float maxHealthAtStart;
    private float nextPatternTime;
    private bool nextActionIsSummon = true;
    private bool phaseTwoAnnounced;
    private bool casting;
    private bool missingPoolWarned;
    private bool missingMinionWarned;
    private bool missingOrbWarned;
    private Vector2 facingDirection = Vector2.down;

    public EnemyHealth Health
    {
        get
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();
            return health;
        }
    }
    public Transform Target => target;
    public bool IsPhaseTwo => HordeLordRules.IsPhaseTwo(
        health != null ? health.CurrentHealth : 0f, maxHealthAtStart);

    private void Awake()
    {
        follow = GetComponent<EnemyFollow>();
        follow.DisableHitKnockback();
        health = GetComponent<EnemyHealth>();
        body = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spawnPoolBehaviour is IEnemySpawnPool assigned)
            spawnPool = assigned;
    }

    private void OnEnable()
    {
        if (health == null || health.IsDead)
        {
            enabled = false;
            return;
        }

        ResolveTarget();
        if (spawnPool == null)
            spawnPool = FindScenePool();
        if (follow != null)
            follow.SetTarget(target, 1000f);

        maxHealthAtStart = Mathf.Max(1f, health.CurrentHealth);
        nextPatternTime = Time.time + 0.6f;
        nextActionIsSummon = true;
        phaseTwoAnnounced = IsPhaseTwo;
        summons.Clear();
        health.Died += OnDied;
        patternRoutine = StartCoroutine(PatternLoop());
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= OnDied;
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }
        casting = false;
    }

    private void FixedUpdate()
    {
        if (casting || health == null || health.IsDead || body == null || follow == null)
            return;
        if (target == null || !target.gameObject.activeInHierarchy || Time.timeScale <= 0f)
            return;

        Vector2 offset = (Vector2)target.position - body.position;
        float distance = offset.magnitude;
        HordeLordRules.Stance stance = HordeLordRules.ChooseStance(distance, innerDistance, outerDistance);
        if (stance == HordeLordRules.Stance.Hold || distance <= Mathf.Epsilon)
            return;

        Vector2 away = ((Vector2)body.position - (Vector2)target.position).normalized;
        float band = stance == HordeLordRules.Stance.Retreat ? innerDistance : outerDistance;
        Vector2 goal = (Vector2)target.position + away * band;
        Vector2 next = Vector2.MoveTowards(body.position, goal, moveSpeed * Time.fixedDeltaTime);
        follow.MoveTo(next);
    }

    private void Update()
    {
        AnnouncePhaseIfNeeded();
        UpdateAnimator();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (follow != null)
            follow.SetTarget(target, 1000f);
    }

    private IEnumerator PatternLoop()
    {
        while (isActiveAndEnabled && health != null && !health.IsDead)
        {
            if (target == null || !target.gameObject.activeInHierarchy || Time.timeScale <= 0f ||
                Time.time < nextPatternTime)
            {
                yield return null;
                continue;
            }

            AnnouncePhaseIfNeeded();
            bool phaseTwo = IsPhaseTwo;
            bool summon = nextActionIsSummon;
            if (summon && AliveSummonCount() >= HordeLordRules.AliveCap(phaseTwo))
                summon = false;
            else
                nextActionIsSummon = !nextActionIsSummon;

            yield return Cast(summon, phaseTwo);
            nextPatternTime = Time.time + (IsPhaseTwo ? phaseTwoGap : phaseOneGap);
        }
    }

    private IEnumerator Cast(bool summon, bool phaseTwo)
    {
        casting = true;
        FaceTarget();
        string directionName = DirectionName(facingDirection);
        int stateHash = Animator.StringToHash($"HordeLord_Cast_{directionName}");
        bool played = animator != null && animator.runtimeAnimatorController != null &&
                      animator.isActiveAndEnabled && animator.HasState(0, stateHash);
        if (played)
        {
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
        }

        bool released = false;
        float elapsed = 0f;
        float releaseAt = castSeconds * 0.45f;
        while (elapsed < castSeconds && isActiveAndEnabled && health != null && !health.IsDead)
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            if (!released && elapsed >= releaseAt)
            {
                released = true;
                if (summon)
                    SummonPack(phaseTwo);
                else
                    ThrowOrbs(phaseTwo);
            }

            if (played)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (!info.loop && info.normalizedTime >= 0.95f)
                    break;
            }
            yield return null;
        }

        if (!released && isActiveAndEnabled && health != null && !health.IsDead)
        {
            if (summon)
                SummonPack(phaseTwo);
            else
                ThrowOrbs(phaseTwo);
        }

        casting = false;
        if (animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled &&
            animator.HasState(0, Animator.StringToHash("HordeLordIdle")))
            animator.Play("HordeLordIdle", 0, 0f);
    }

    private void SummonPack(bool phaseTwo)
    {
        if (minionPrefab == null)
        {
            if (!missingMinionWarned)
            {
                missingMinionWarned = true;
                Debug.LogWarning("무리군주 소환에는 일반 몬스터 프리팹이 필요합니다.", this);
            }
            return;
        }

        if (spawnPool == null)
            spawnPool = FindScenePool();
        if (spawnPool == null)
        {
            if (!missingPoolWarned)
            {
                missingPoolWarned = true;
                Debug.LogWarning("무리군주 소환에는 씬의 SceneEnemyPool이 필요합니다.", this);
            }
            return;
        }

        if (target == null)
            return;

        PruneSummons();
        int count = HordeLordRules.SummonCount(phaseTwo);
        int room = HordeLordRules.AliveCap(phaseTwo) - AliveSummonCount();
        count = Mathf.Min(count, Mathf.Max(0, room));
        Vector2 forward = facingDirection;
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = HordeLordRules.SummonOffset(i, count, forward, summonRadius);
            Vector3 position = body.position + offset;
            position.z = transform.position.z;
            bool recorded = false;
            bool ok = spawnPool.TrySpawn(minionPrefab, position, target, gameObject.scene, enemy =>
            {
                if (enemy == null || recorded)
                    return;
                int generation = enemy.TryGetComponent<EnemyHealth>(out EnemyHealth summoned)
                    ? summoned.SpawnGeneration : 0;
                summons.Add(new TrackedSummon { Enemy = enemy, Generation = generation });
                recorded = true;
            });
            if (!ok)
                break;
        }
    }

    private void ThrowOrbs(bool phaseTwo)
    {
        if (orbPrefab == null)
        {
            if (!missingOrbWarned)
            {
                missingOrbWarned = true;
                Debug.LogWarning("무리군주 구체 프리팹이 없습니다.", this);
            }
            return;
        }

        int count = HordeLordRules.OrbCount(phaseTwo);
        Vector2 forward = facingDirection;
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = HordeLordRules.OrbDirection(i, count, forward);
            Vector3 position = body.position + direction * 0.7f;
            position.z = transform.position.z;
            HordeLordOrb orb = Instantiate(orbPrefab, position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(orb.gameObject, gameObject.scene);
            orb.Launch(direction, orbSpeed, orbDamage, orbLifetime, gameObject);
            orbs.Add(orb);
        }
    }

    private void AnnouncePhaseIfNeeded()
    {
        if (phaseTwoAnnounced || !IsPhaseTwo)
            return;
        phaseTwoAnnounced = true;
        nextPatternTime = Time.time;
        Debug.Log("무리군주가 2페이즈로 넘어갑니다.", this);
    }

    private void OnDied()
    {
        casting = false;
        for (int i = summons.Count - 1; i >= 0; i--)
        {
            TrackedSummon summon = summons[i];
            if (!StillOwned(summon, out EnemyHealth summonHealth))
                continue;
            if (!summonHealth.IsDead)
                summonHealth.Despawn();
        }
        summons.Clear();
        for (int i = orbs.Count - 1; i >= 0; i--)
        {
            if (orbs[i] != null)
                Destroy(orbs[i].gameObject);
        }
        orbs.Clear();
    }

    private int AliveSummonCount()
    {
        PruneSummons();
        return summons.Count;
    }

    private void PruneSummons()
    {
        for (int i = summons.Count - 1; i >= 0; i--)
        {
            if (!StillOwned(summons[i], out EnemyHealth summonHealth) || summonHealth.IsDead)
                summons.RemoveAt(i);
        }
    }

    private static bool StillOwned(TrackedSummon summon, out EnemyHealth summonHealth)
    {
        summonHealth = null;
        if (summon.Enemy == null || !summon.Enemy.activeInHierarchy)
            return false;
        if (!summon.Enemy.TryGetComponent<EnemyHealth>(out summonHealth))
            return false;
        return summonHealth.SpawnGeneration == summon.Generation;
    }

    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            return;

        if (!casting)
            FaceTarget();

        bool moving = false;
        if (!casting && target != null && target.gameObject.activeInHierarchy)
        {
            float distance = Vector2.Distance(body.position, target.position);
            moving = HordeLordRules.ChooseStance(distance, innerDistance, outerDistance) != HordeLordRules.Stance.Hold;
        }

        animator.SetFloat(FacingXParameter, facingDirection.x);
        animator.SetFloat(FacingYParameter, facingDirection.y);
        animator.SetBool(IsMovingParameter, moving);
    }

    private void FaceTarget()
    {
        if (target == null)
            return;
        Vector2 offset = (Vector2)target.position - (Vector2)body.position;
        if (offset.sqrMagnitude <= Mathf.Epsilon)
            return;
        facingDirection = SnapOctant(offset);
    }

    private void ResolveTarget()
    {
        if (target != null)
            return;

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IContactDamageReceiver && behaviour.gameObject.scene == gameObject.scene)
            {
                target = behaviour.transform;
                return;
            }
        }
    }

    private IEnemySpawnPool FindScenePool()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IEnemySpawnPool pool && behaviour.gameObject.scene == gameObject.scene)
                return pool;
        }
        return null;
    }

    private static Vector2 SnapOctant(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;
        int octant = Mathf.RoundToInt(angle / 45f) % 8;
        float snapped = octant * 45f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped));
    }

    private static string DirectionName(Vector2 forward)
    {
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;
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
}
