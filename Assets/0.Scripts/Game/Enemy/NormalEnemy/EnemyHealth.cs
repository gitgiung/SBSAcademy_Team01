using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("적 스탯 설정")]
    [SerializeField, Min(1f)] private float maxHealth = 3f;

    [Header("피격 이펙트 설정")]
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.white;

    [Header("UI 설정")]
    [SerializeField] private Slider healthSlider;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public event System.Action Died;

    private SpriteRenderer[] visuals;
    private Color[] originalColors;
    private Coroutine hitFlash;
    private Collider2D[] colliders;
    private bool[] colliderStates;
    private Rigidbody2D body;
    private bool bodySimulated;
    private EnemyFollow follow;
    private bool followEnabled;
    private bool initialized;
    private IEnemySpawnPool spawnPool;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        CurrentHealth = Mathf.Max(1f, maxHealth);

        // 자식 오브젝트를 포함한 모든 스프라이트 렌더러 수집 및 초기 색상 저장
        visuals = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
        {
            originalColors[i] = visuals[i].color;
        }
        colliders = GetComponentsInChildren<Collider2D>(true);
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderStates[i] = colliders[i].enabled;
        body = GetComponent<Rigidbody2D>();
        bodySimulated = body != null && body.simulated;
        follow = GetComponent<EnemyFollow>();
        followEnabled = follow != null && follow.enabled;
    }

    /// <summary>풀 initialize에서 호출합니다. 최초 Awake보다 먼저 호출돼도 안전합니다.</summary>
    public void PrepareForSpawn(IEnemySpawnPool owner)
    {
        EnsureInitialized();
        spawnPool = owner;
        CurrentHealth = Mathf.Max(1f, maxHealth);
        IsDead = false;
        StopHitFlash();
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
        if (body != null)
        {
            body.simulated = bodySimulated;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
        if (follow != null) follow.enabled = followEnabled;
        UpdateHealthUI();
    }

    /// <summary>보스 전환 등 보상 없는 제거. 풀 소유 객체는 파괴하지 않습니다.</summary>
    public void Despawn()
    {
        IEnemySpawnPool owner = spawnPool;
        spawnPool = null;
        if (owner != null) owner.ReturnEnemy(gameObject);
        else
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateHealthUI();
    }

    /// <summary>
    /// IDamageable 데미지 처리 함수
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead || !isActiveAndEnabled)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        UpdateHealthUI();

        if (CurrentHealth <= 0f)
        {
            Die();
            return;
        }

        // 피격 시 흰색 반짝임 효과
        if (hitFlash != null) StopCoroutine(hitFlash);
        hitFlash = StartCoroutine(Flash());
    }

    /// <summary>
    /// int 타입을 받는 IDamageable 오버로딩
    /// </summary>
    public void TakeDamage(int   damage)
    {
        TakeDamage((float)damage);
    }

    /// <summary>
    /// 피격 색상 변경 코루틴
    /// </summary>
    private IEnumerator Flash()
    {
        foreach (var visual in visuals)
        {
            if (visual != null) visual.color = hitColor;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, hitFlashDuration));

        RestoreColors();
        hitFlash = null;
    }

    /// <summary>
    /// 체력 UI 슬라이더 갱신
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthSlider != null && maxHealth > 0f)
        {
            healthSlider.value = CurrentHealth / maxHealth;
        }
    }

    /// <summary>
    /// 사망 처리 로직
    /// </summary>
    private void Die()
    {
        IsDead = true;

        // 진행 중인 피격 코루틴 중단 및 색상 복원
        if (hitFlash != null)
        {
            StopCoroutine(hitFlash);
            hitFlash = null;
        }
        RestoreColors();

        // 추적 컴포넌트 비활성화
        if (TryGetComponent<EnemyFollow>(out var follow)) follow.enabled = false;

        // 모든 충돌체 및 물리 연산 끄기
        foreach (var collider in GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;

        if (TryGetComponent<Rigidbody2D>(out var body)) body.simulated = false;

        try { Died?.Invoke(); }
        finally { Despawn(); }
    }

    private void OnDisable()
    {
        StopHitFlash();
    }

    private void StopHitFlash()
    {
        if (hitFlash != null)
        {
            StopCoroutine(hitFlash);
            hitFlash = null;
        }
        RestoreColors();
    }

    private void RestoreColors()
    {
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null) visuals[i].color = originalColors[i];
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
    }
}
