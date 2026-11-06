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

    [Header("UI 설정(보스한테만 해당)")]
    [SerializeField] private Slider healthSlider;

    [Header("경험치 드랍")]
    [SerializeField] private GameObject experiencePickupPrefab; // 적 사망 시 드랍할 경험치 오브젝트 프리팹. null이면 드랍하지 않음

    public float CurrentHealth { get; private set; }
    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public bool IsDead { get; private set; }
    public int SpawnGeneration { get; private set; }
    public event System.Action Died;
    public static event System.Action<EnemyHealth, float> DamageApplied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDamageObservers() => DamageApplied = null;

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
    private IEnemyExperienceDropper experienceDropper;
    public float SpawnRawExperience { get; private set; } = -1f;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        CurrentHealth = Mathf.Max(1f, maxHealth);

        // 그림자와 독립 VFX는 각자의 색을 유지합니다.
        SpriteRenderer[] found = GetComponentsInChildren<SpriteRenderer>(true);
        int visualCount = 0;
        for (int i = 0; i < found.Length; i++)
            if (!IsExcludedFromHitFlash(found[i])) visualCount++;
        visuals = new SpriteRenderer[visualCount];
        originalColors = new Color[visualCount];
        for (int i = 0, visualIndex = 0; i < found.Length; i++)
        {
            if (IsExcludedFromHitFlash(found[i])) continue;
            visuals[visualIndex] = found[i];
            originalColors[visualIndex] = found[i].color;
            visualIndex++;
        }
        GroundShadow.Attach(GetComponent<SpriteRenderer>());
        colliders = GetComponentsInChildren<Collider2D>(true);
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderStates[i] = colliders[i].enabled;
        body = GetComponent<Rigidbody2D>();
        bodySimulated = body != null && body.simulated;
        follow = GetComponent<EnemyFollow>();
        followEnabled = follow != null && follow.enabled;
        UpdateHealthUI();
    }

    private static bool IsExcludedFromHitFlash(SpriteRenderer renderer)
    {
        return GroundShadow.IsShadow(renderer) || renderer.GetComponent<HitFlashExcluded>() != null;
    }

    /// <summary>풀 initialize에서 호출합니다. 최초 Awake보다 먼저 호출돼도 안전합니다.</summary>
    public void PrepareForSpawn(IEnemySpawnPool owner)
    {
        PrepareForSpawn(owner, -1f);
    }

    public void PrepareForSpawn(IEnemySpawnPool owner, float rawExperience)
    {
        EnsureInitialized();
        spawnPool = owner;
        experienceDropper = owner as IEnemyExperienceDropper;
        SpawnRawExperience = rawExperience;
        SpawnGeneration++;
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

    public void SetExperienceDropper(IEnemyExperienceDropper dropper) { experienceDropper = dropper; }

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
        DamageApplied?.Invoke(this, damage);

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
        if (healthSlider == null) return;
        // 보스/적의 체력 표시는 좌우 UI 탐색이나 포인터로 편집할 수 없습니다.
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.navigation = new Navigation { mode = Navigation.Mode.None };
        healthSlider.interactable = false;
        float ratio = maxHealth > 0f && !float.IsNaN(maxHealth) && !float.IsInfinity(maxHealth)
            && !float.IsNaN(CurrentHealth) && !float.IsInfinity(CurrentHealth)
            ? Mathf.Clamp01(CurrentHealth / maxHealth) : 0f;
        healthSlider.SetValueWithoutNotify(Mathf.Lerp(healthSlider.minValue, healthSlider.maxValue, ratio));
    }

    /// <summary>
    /// 사망 처리 로직
    /// </summary>
    private void Die()
    {
        int dyingGeneration = SpawnGeneration;
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

        try
        {
            SpawnExperiencePickup();
            Died?.Invoke();
        }
        // 사망 알림에서 반환/재대여했다면 새로운 생명은 이전 사망이 정리하지 않습니다.
        finally { if (SpawnGeneration == dyingGeneration) Despawn(); }
    }

    private void SpawnExperiencePickup()
    {
        if (experiencePickupPrefab == null) return;

        if (experienceDropper != null)
        {
            experienceDropper.TryDropExperience(experiencePickupPrefab, transform.position,
                follow != null ? follow.Target : null, SpawnRawExperience);
            return;
        }

        GameObject pickupObject = Instantiate(experiencePickupPrefab, transform.position, Quaternion.identity);
        if (pickupObject.TryGetComponent<IExperiencePickup>(out var pickup))
            pickup.Initialize(follow != null ? follow.Target : null);
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
