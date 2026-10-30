using UnityEngine;

/// <summary>씬의 분별 스폰 일정과 고정 개수 스포너를 소유합니다. 전역 매니저가 아닙니다.</summary>
[DisallowMultipleComponent]
public sealed class EnemySpawnDirector : MonoBehaviour
{
    [Header("씬 연결")]
    [Tooltip("IEnemySpawnPool을 구현하는 SceneEnemyPool 컴포넌트.")]
    [SerializeField] private MonoBehaviour poolProvider;
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private EnemySpawner spawnerTemplate;
    [Header("스폰 설정")]
    [SerializeField, Min(0)] private int spawnerCount = 1;
    [SerializeField, Min(0.01f)] private float offscreenMargin = 1f;
    [Tooltip("Element 0 = 0~59초, Element 1 = 60~119초. 끝나면 마지막 설정 유지.")]
    [SerializeField] private EnemySpawnMinute[] minutes = new EnemySpawnMinute[0];

    private EnemySpawner[] spawners;
    private int[] lastStartedMinute;
    private IEnemySpawnPool spawnPool;
    private bool initialized;
    private bool paused;

    public float ElapsedTime { get; private set; }
    public int CurrentMinute => Mathf.FloorToInt(ElapsedTime / 60f);
    public bool IsPaused => paused;
    public int SpawnerCount => initialized ? spawners.Length : spawnerCount;

    private void Start() { Initialize(); }
    private void Update() { Tick(Time.deltaTime); }

    public void Initialize()
    {
        if (initialized) return;
        spawnPool = poolProvider as IEnemySpawnPool;
        if (spawnPool == null || player == null || playerCamera == null || spawnerTemplate == null ||
            minutes == null || minutes.Length == 0 || spawnerCount < 0)
        {
            FailConfiguration("pool, player, camera, spawner template and minute settings are required.");
            return;
        }
        foreach (EnemySpawnMinute minute in minutes)
            if (minute == null || !minute.IsValid)
            {
                FailConfiguration("each minute needs valid enemy prefabs, n > 0, m >= 0 and l >= 1.");
                return;
            }

        spawnerTemplate.gameObject.SetActive(false);
        spawners = new EnemySpawner[spawnerCount];
        lastStartedMinute = new int[spawnerCount];
        for (int i = 0; i < spawners.Length; i++)
        {
            spawners[i] = i == 0 ? spawnerTemplate : Instantiate(spawnerTemplate, transform);
            spawners[i].gameObject.name = "Enemy Spawner " + (i + 1);
            lastStartedMinute[i] = -1;
        }
        initialized = true;
        if (!paused) ScheduleIdleSpawners();
    }

    public void Tick(float deltaTime)
    {
        if (!initialized || paused || !isActiveAndEnabled || deltaTime <= 0f) return;
        foreach (EnemySpawner spawner in spawners)
            if (spawner != null) spawner.Tick(deltaTime);
        ElapsedTime += deltaTime;
        // 새 작업은 지금부터 시간을 셉니다. 이전 프레임의 시간을 물려주지 않습니다.
        ScheduleIdleSpawners();
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (!initialized) return;
        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner == null) continue;
            if (paused) spawner.StopSpawning();
            else spawner.ResumeSpawning();
        }
    }

    private void ScheduleIdleSpawners()
    {
        int minuteIndex = CurrentMinute;
        EnemySpawnMinute settings = minutes[Mathf.Min(minuteIndex, minutes.Length - 1)];
        for (int i = 0; i < spawners.Length; i++)
        {
            EnemySpawner spawner = spawners[i];
            if (spawner == null || lastStartedMinute[i] == minuteIndex ||
                (spawner.gameObject.activeSelf && !spawner.IsComplete)) continue;
            spawner.gameObject.SetActive(false);
            spawner.Configure(spawnPool, player, settings.EnemyPrefabs, settings.SpawnInterval,
                settings.TotalSpawnCount, settings.BatchSpawnCount, playerCamera, offscreenMargin);
            lastStartedMinute[i] = minuteIndex;
            spawner.gameObject.SetActive(true);
        }
    }

    private void FailConfiguration(string message)
    {
        if (spawnerTemplate != null) spawnerTemplate.gameObject.SetActive(false);
        Debug.LogError("EnemySpawnDirector: " + message, this);
        enabled = false;
    }

    private void OnDestroy()
    {
        if (!initialized) return;
        foreach (EnemySpawner spawner in spawners)
            if (spawner != null)
            {
                spawner.StopSpawning();
                if (spawner != spawnerTemplate) Destroy(spawner.gameObject);
            }
    }
}
