using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyFollow), typeof(EnemyHealth), typeof(Rigidbody2D))]
public sealed class BossController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Transform target;
    [SerializeField] private BossPatternSelector patternSelector;

    [Header("패턴 선택 거리")]
    [SerializeField, Min(0.01f)] private float closeRange = 2f;
    [SerializeField, Min(0.01f)] private float specialPatternDistance = 10f;

    [Header("일반 패턴 간격")]
    [SerializeField, Min(0f)] private float pattern1ToPattern2Delay = 3f;
    [SerializeField, Min(0f)] private float pattern2ToPattern1Delay = 5f;
    [SerializeField, Min(0f)] private float doubleSlashInterval = 0.5f;

    [Header("전방 할퀴기")]
    [SerializeField, Min(0.01f)] private float slashRange = 2f;
    [SerializeField, Range(1f, 360f)] private float slashAngle = 90f;
    [SerializeField, Min(0f)] private float damage = 10f;

    [Header("특수 돌진 패턴")]
    [Tooltip("플레이어를 향해 이동하는 기본 진행 시간입니다. 돌진은 이 시간이 지나도 근거리까지 계속 추적합니다.")]
    [SerializeField, Min(0.01f)] private float dashDuration = 3f;
    [SerializeField, Min(0.01f)] private float dashEndDamageRadius = 2f;

    private EnemyFollow follow;
    private EnemyHealth health;
    private Rigidbody2D body;
    private Coroutine patternRoutine;
    private BossPattern nextNormalPattern = BossPattern.Pattern1;
    private float nextPatternTime;

    public Transform Target => target;
    public float CloseRange => closeRange;
    public float SpecialPatternDistance => specialPatternDistance;
    public float SlashRange => slashRange;
    public float SlashAngle => slashAngle;
    public float DashEndDamageRadius => dashEndDamageRadius;
    public BossPattern ActivePattern { get; private set; } = BossPattern.None;

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
        if (patternSelector == null) patternSelector = GetComponent<BossPatternSelector>();
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
        follow.SetTarget(target, closeRange);
        follow.enabled = true;
        nextNormalPattern = BossPattern.Pattern1;
        nextPatternTime = Time.time;
        patternRoutine = StartCoroutine(PatternLoop());
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (follow != null) follow.SetTarget(target, closeRange);
    }

    private IEnumerator PatternLoop()
    {
        while (isActiveAndEnabled && !health.IsDead)
        {
            if (target == null || !target.gameObject.activeInHierarchy || Time.timeScale <= 0f ||
                !patternSelector.isActiveAndEnabled || Time.time < nextPatternTime)
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
                    Debug.Log("패턴 1 시작!", this);
                    PerformForwardSlash();
                    yield return null;
                    ActivePattern = BossPattern.None;
                    nextNormalPattern = BossPattern.Pattern2;
                    nextPatternTime = Time.time + pattern1ToPattern2Delay;
                    break;

                case BossPattern.Pattern2:
                    ActivePattern = BossPattern.Pattern2;
                    Debug.Log("패턴 2 시작!", this);
                    PerformForwardSlash();
                    if (doubleSlashInterval > 0f)
                        yield return new WaitForSeconds(doubleSlashInterval);
                    if (CanAttackTarget()) PerformForwardSlash();
                    yield return null;
                    ActivePattern = BossPattern.None;
                    nextNormalPattern = BossPattern.Pattern1;
                    nextPatternTime = Time.time + pattern2ToPattern1Delay;
                    break;

                case BossPattern.Pattern3:
                    ActivePattern = BossPattern.Pattern3;
                    Debug.Log("패턴 3 시작!", this);
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
        if (distance >= specialPatternDistance)
            return BossPattern.Pattern3;

        if (distance > closeRange)
            return BossPattern.None;

        return patternSelector.SelectPattern(distance, (int)health.CurrentHealth, nextNormalPattern);
    }

    private void PerformForwardSlash()
    {
        if (!CanAttackTarget()) return;

        Vector2 origin = body.position;
        Vector2 targetOffset = (Vector2)target.position - origin;
        if (targetOffset.sqrMagnitude <= Mathf.Epsilon) return;

        // 할퀴기가 시작되는 순간 플레이어 위치를 향해 공격 방향을 정한다.
        Vector2 forward = targetOffset.normalized;
        float minimumDot = Mathf.Cos(Mathf.Clamp(slashAngle, 1f, 360f) * 0.5f * Mathf.Deg2Rad);
        var damagedReceivers = new HashSet<IContactDamageReceiver>();

        foreach (Collider2D hitCollider in Physics2D.OverlapCircleAll(origin, slashRange))
        {
            if (hitCollider == null) continue;
            var receiver = hitCollider.GetComponentInParent(typeof(IContactDamageReceiver)) as IContactDamageReceiver;
            if (receiver == null || !damagedReceivers.Add(receiver)) continue;

            Vector2 offset = (Vector2)hitCollider.bounds.center - origin;
            if (offset.sqrMagnitude > slashRange * slashRange) continue;
            if (offset.sqrMagnitude > Mathf.Epsilon && Vector2.Dot(forward, offset.normalized) < minimumDot) continue;
            receiver.TakeContactDamage(damage, gameObject);
        }
    }

    private IEnumerator DashToTrackedTarget()
    {
        if (!CanAttackTarget()) yield break;

        follow.enabled = false;
        Vector2 dashStartPosition = body.position;
        float elapsed = 0f;

        while (CanAttackTarget() && Vector2.Distance(body.position, target.position) > closeRange)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(elapsed / dashDuration);
            Vector2 trackedTargetPosition = target.position;
            body.MovePosition(Vector2.Lerp(dashStartPosition, trackedTargetPosition, progress));
        }

        // 플레이어와 적의 충돌을 유지한 상태로 마지막 돌진 이동을 처리한다.
        yield return new WaitForFixedUpdate();
        bool reachedTarget = CanAttackTarget() && Vector2.Distance(body.position, target.position) <= closeRange;
        if (reachedTarget) ApplyDashEndDamage(target.position);

        follow.SetTarget(target, closeRange);
        follow.enabled = true;
    }

    private void ApplyDashEndDamage(Vector2 dashEndCenter)
    {
        var damagedReceivers = new HashSet<IContactDamageReceiver>();
        foreach (Collider2D hitCollider in Physics2D.OverlapCircleAll(dashEndCenter, dashEndDamageRadius))
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
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }
        enabled = false;
    }

    private void OnDisable()
    {
        ActivePattern = BossPattern.None;
        if (health != null) health.Died -= OnDied;
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }
        if (follow != null) follow.enabled = false;
    }

    private void OnValidate()
    {
        closeRange = Mathf.Max(0.01f, closeRange);
        specialPatternDistance = Mathf.Max(closeRange + 0.01f, specialPatternDistance);
        pattern1ToPattern2Delay = Mathf.Max(0f, pattern1ToPattern2Delay);
        pattern2ToPattern1Delay = Mathf.Max(0f, pattern2ToPattern1Delay);
        doubleSlashInterval = Mathf.Max(0f, doubleSlashInterval);
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
