using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyFollow), typeof(EnemyHealth))]
public sealed class BossController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private BossPatternSelector patternSelector;
    [SerializeField, Min(0.01f)] private float attackDistance = 2f;
    [SerializeField, Min(0.01f)] private float patternInterval = 2f;

    private EnemyFollow follow;
    private EnemyHealth health;
    private float nextPatternTime;

    private void Awake()
    {
        follow = GetComponent<EnemyFollow>();
        health = GetComponent<EnemyHealth>();
        if (patternSelector == null) patternSelector = GetComponent<BossPatternSelector>();
    }

    private void OnEnable()
    {
        if (health.IsDead)
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
        follow.SetTarget(target, Mathf.Max(0.01f, attackDistance));
        follow.enabled = true;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (follow != null) follow.SetTarget(target, Mathf.Max(0.01f, attackDistance));
    }

    private void Update()
    {
        follow.SetTarget(target, Mathf.Max(0.01f, attackDistance));
        if (health.IsDead || target == null || !target.gameObject.activeInHierarchy ||
            patternSelector == null || !patternSelector.isActiveAndEnabled || Time.timeScale <= 0f)
            return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > Mathf.Max(0.01f, attackDistance) + 0.001f || Time.time < nextPatternTime)
            return;

        nextPatternTime = Time.time + Mathf.Max(0.01f, patternInterval);
        BossPattern selected = patternSelector.SelectPattern(distance, health.CurrentHealth);
        LogPattern(selected);
    }

    private void LogPattern(BossPattern pattern)
    {
        // This is the future execution boundary; no damage, projectiles or dash are performed yet.
        switch (pattern)
        {
            case BossPattern.Pattern1:
                Debug.Log("[Boss] 임시 패턴1: 연속 근접 3연 공격 (로그 전용)", this);
                break;
            case BossPattern.Pattern2:
                Debug.Log("[Boss] 임시 패턴2: 중거리 견제 공격 (로그 전용)", this);
                break;
            case BossPattern.Pattern3:
                Debug.Log("[Boss] 임시 패턴3: 돌진 공격 (로그 전용)", this);
                break;
            default:
                Debug.LogWarning("Boss selector returned an unknown pattern.", this);
                break;
        }
    }

    private void OnDied()
    {
        enabled = false;
    }

    private void OnDisable()
    {
        if (health != null) health.Died -= OnDied;
        if (follow != null) follow.enabled = false;
    }

    private void OnValidate()
    {
        attackDistance = Mathf.Max(0.01f, attackDistance);
        patternInterval = Mathf.Max(0.01f, patternInterval);
    }
}
