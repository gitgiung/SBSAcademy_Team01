using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExperiencePickup : MonoBehaviour, IPooledExperiencePickup, IPoolable
{
    [System.Serializable]
    public sealed class ExperienceColorBand
    {
        [Min(0f)] public float minimumExperience;
        public Color color = Color.white;

        public ExperienceColorBand(float minimumExperience, Color color)
        {
            this.minimumExperience = minimumExperience;
            this.color = color;
        }
    }

    [Header("획득 설정")]
    [Min(0f)]
    [SerializeField] private float experienceAmount; // 획득시 경험치량
    [Min(0f)]
    [SerializeField] private float attractionSpeed; // 플레이어에게 끌려가는 속도. 0이면 끌려가지 않음
    [Tooltip("추적 중 초당 증가하는 속도입니다. 0이면 기존 등속 이동을 유지합니다.")]
    [SerializeField, Min(0f)] private float attractionAcceleration;

    [Header("경험치별 색상 (임시값, 프리팹 Inspector에서 변경)")]
    [Tooltip("미지정 시 자신 또는 자식의 SpriteRenderer를 사용합니다. 공유 머티리얼은 변경하지 않습니다.")]
    [SerializeField] private SpriteRenderer pickupRenderer;
    [Tooltip("최소 경험치 이상인 구간 중 가장 큰 최소값을 사용합니다. 배열 순서는 무관하며 같은 최소값은 앞 항목 우선입니다.")]
    [SerializeField] private ExperienceColorBand[] experienceColors =
    {
        new ExperienceColorBand(0f, new Color32(77, 166, 255, 255)),
        new ExperienceColorBand(3f, new Color32(82, 214, 129, 255)),
        new ExperienceColorBand(4f, new Color32(255, 212, 92, 255)),
        new ExperienceColorBand(6f, new Color32(181, 140, 255, 255)),
        new ExperienceColorBand(7f, new Color32(255, 158, 74, 255)),
        new ExperienceColorBand(9f, new Color32(255, 94, 126, 255))
    };

    // 후속 획득 보정과 무관하게 보석에 담긴 원래 경험치량입니다.
    public float RawExperience => experienceAmount;

    private PlayerData playerData;
    private bool collected;
    private PooledObject pooledItem;
    private uint pickupLease;
    private float defaultExperience;
    private bool defaultCaptured;
    private System.Action<float, float> collectedReport;
    private float attractionElapsedTime;
    private bool attracted;
    // 자석 아이템으로 끌려가는 중이면 0보다 큽니다. 획득 범위와 기본 흡입 속도 대신 이 최소 속도를 씁니다.
    private float magnetSpeed;
    private Vector3 motionPosition;
    private Vector3 baseScale;
    private bool baseScaleCaptured;
    private bool motionReady;
    private float visualTime;

    public void OnPoolRent()
    {
        if (!defaultCaptured) { defaultExperience = experienceAmount; defaultCaptured = true; }
        pooledItem = GetComponent<PooledObject>();
        pickupLease = pooledItem != null ? pooledItem.LeaseVersion : 0;
        collected = false;
        playerData = null;
        collectedReport = null;
        attractionElapsedTime = 0f;
        attracted = false;
        magnetSpeed = 0f;
        motionReady = false;
        visualTime = 0f;
        RestoreScale();
    }

    public void OnPoolReturn()
    {
        attractionElapsedTime = 0f;
        attracted = false;
        magnetSpeed = 0f;
        motionReady = false;
        visualTime = 0f;
        RestoreScale();
        collected = true;
        playerData = null;
        collectedReport = null;
    }

    public void PrepareForSpawn(Transform target, float rawExperience, System.Action<float, float> report)
    {
        Initialize(target);
        SetExperienceAmount(rawExperience >= 0 ? rawExperience : defaultExperience);
        collectedReport = report;
    }

    private void Awake()
    {
        ResolvePlayerData(null);
    }

    private void OnEnable()
    {
        if (!baseScaleCaptured)
        {
            baseScale = transform.localScale;
            baseScaleCaptured = true;
        }
        motionPosition = transform.position;
        motionReady = true;
        RefreshExperienceColor();
    }

    public void Initialize(Transform target)
    {
        attractionElapsedTime = 0f;
        attracted = false;
        magnetSpeed = 0f;
        motionPosition = transform.position;
        motionReady = true;
        ResolvePlayerData(target);
        RefreshExperienceColor();
    }

    /// <summary>
    /// 자석 아이템: 획득 범위 밖이거나 기본 흡입 속도가 0이어도 플레이어에게 끌려가기 시작합니다.
    /// 이미 흡입 중이던 보석의 누적 가속은 유지합니다.
    /// </summary>
    public bool BeginMagnetAttraction(float minimumSpeed)
    {
        if (collected || !isActiveAndEnabled || float.IsNaN(minimumSpeed) || float.IsInfinity(minimumSpeed)
            || minimumSpeed <= 0f) return false;
        if (playerData == null) ResolvePlayerData(null);
        if (playerData == null || playerData.IsDead) return false;
        magnetSpeed = Mathf.Max(magnetSpeed, minimumSpeed);
        attracted = true;
        return true;
    }

    /// <summary>드롭 생성자가 경험치량을 주입할 때 외형도 함께 갱신합니다. 기존 float 획득값과 호환됩니다.</summary>
    public bool SetExperienceAmount(float amount)
    {
        if (amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return false;
        experienceAmount = amount;
        RefreshExperienceColor();
        return true;
    }

    [ContextMenu("외형/경험치 색상 미리보기")]
    public void RefreshExperienceColor()
    {
        if (pickupRenderer == null) pickupRenderer = GetComponent<SpriteRenderer>();
        if (pickupRenderer == null) pickupRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (pickupRenderer == null) return;

        Color selectedColor = Color.white;
        float selectedMinimum = -1f;
        if (experienceColors != null)
            foreach (ExperienceColorBand band in experienceColors)
            {
                if (band == null || band.minimumExperience < 0f || float.IsNaN(band.minimumExperience)
                    || float.IsInfinity(band.minimumExperience)) continue;
                if (experienceAmount >= band.minimumExperience && band.minimumExperience > selectedMinimum)
                {
                    selectedMinimum = band.minimumExperience;
                    selectedColor = band.color;
                }
            }
        // 원본 스프라이트의 명암/색에 tint만 적용하며 material 인스턴스를 생성하지 않습니다.
        pickupRenderer.color = selectedColor;
    }

    private void OnDisable()
    {
        attractionElapsedTime = 0f;
        attracted = false;
        magnetSpeed = 0f;
        motionReady = false;
        RestoreScale();
    }

    private void Update()
    {
        if (motionReady) transform.position = motionPosition;
        UpdateAttraction(Time.deltaTime);
        motionPosition = transform.position;
        motionReady = true;
        ApplyIdleMotion(Time.deltaTime);
    }

    private void ApplyIdleMotion(float deltaTime)
    {
        if (!baseScaleCaptured || collected || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            RestoreScale();
            return;
        }
        visualTime += deltaTime;
        float sway = Mathf.Sin(visualTime * 2.6f) * 0.07f;
        float pulse = 1f + Mathf.Sin(visualTime * 3.4f) * 0.07f;
        transform.position = new Vector3(motionPosition.x + sway, motionPosition.y, motionPosition.z);
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z * pulse);
    }

    private void RestoreScale()
    {
        if (baseScaleCaptured) transform.localScale = baseScale;
    }

    private void UpdateAttraction(float deltaTime)
    {
        bool magnetized = magnetSpeed > 0f;
        float startSpeed = float.IsNaN(attractionSpeed) || float.IsInfinity(attractionSpeed)
            ? 0f : Mathf.Max(0f, attractionSpeed);
        if (magnetized) startSpeed = Mathf.Max(startSpeed, magnetSpeed);
        if (collected || playerData == null || playerData.IsDead || startSpeed <= 0f)
        {
            attractionElapsedTime = 0f;
            attracted = false;
            return;
        }

        if (magnetized) attracted = true;
        else
        {
            float pickupDistance = Mathf.Max(0f, playerData.PickupDistance);
            if (pickupDistance <= 0f)
            {
                attractionElapsedTime = 0f;
                attracted = false;
                return;
            }

            if (!attracted)
            {
                Vector2 offset = (Vector2)playerData.transform.position - (Vector2)transform.position;
                if (offset.sqrMagnitude > pickupDistance * pickupDistance)
                {
                    attractionElapsedTime = 0f;
                    return;
                }
                attracted = true;
            }
        }

        if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        float acceleration = float.IsNaN(attractionAcceleration) || float.IsInfinity(attractionAcceleration)
            ? 0f : Mathf.Max(0f, attractionAcceleration);
        // 구간 평균 속도로 이동해 프레임 간격이 달라도 같은 추적 시간에는 같은 거리를 이동합니다.
        float distance = (startSpeed + acceleration * (attractionElapsedTime + deltaTime * .5f)) * deltaTime;
        attractionElapsedTime += deltaTime;

        transform.position = Vector3.MoveTowards(
            transform.position,
            playerData.transform.position,
            distance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other.GetComponentInParent<PlayerData>());
    }

    public bool TryCollect(PlayerData collector)
    {
        if (collected || !isActiveAndEnabled || collector == null ||
            (pooledItem != null && (!pooledItem.IsRented || pooledItem.LeaseVersion != pickupLease))) return false;
        // AddEXP의 알림에서 다른 Trigger가 재진입해도 이 보석은 한 번만 지급합니다.
        PooledObject item = pooledItem;
        uint lease = pickupLease;
        float raw = experienceAmount;
        var report = collectedReport;
        bool wasPooled = item != null;
        collected = true;
        bool returnReward = true;
        try
        {
            if (!collector.TryCollectExperience(raw, out float awarded))
            {
                returnReward = false;
                if (!wasPooled || (item != null && item.LeaseVersion == lease)) collected = false;
                return false;
            }
            report?.Invoke(raw, awarded);
            return true;
        }
        finally
        {
            // XP 알림에서 같은 인스턴스가 재대여돼도 이전 대여만 반환합니다.
            if (returnReward)
            {
                if (wasPooled) { if (item != null) item.ReturnToPool(lease); }
                else Destroy(gameObject);
            }
        }
    }

    private void ResolvePlayerData(Transform target)
    {
        if (target != null)
        {
            playerData = target.GetComponent<PlayerData>();
            if (playerData == null)
                playerData = target.GetComponentInParent<PlayerData>();
            if (playerData == null)
                playerData = target.GetComponentInChildren<PlayerData>();
        }

        if (playerData == null)
            playerData = UnityEngine.Object.FindAnyObjectByType<PlayerData>();
    }
}
