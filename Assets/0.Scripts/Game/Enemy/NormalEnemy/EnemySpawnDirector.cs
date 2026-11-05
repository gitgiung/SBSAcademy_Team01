using System;
using UnityEngine;

/// <summary>씬의 단일 진행 시간과 전체 일반 스폰 시도 예산을 소유합니다.</summary>
[DisallowMultipleComponent]
public sealed class EnemySpawnDirector : MonoBehaviour
{
    [Header("씬 연결")]
    [SerializeField] private MonoBehaviour poolProvider;
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private EnemySpawner spawnerTemplate;
    [Header("스폰 위치와 종류")]
    [SerializeField, Min(0)] private int spawnerCount = 1;
    [SerializeField, Min(.01f)] private float offscreenMargin = 1f;
    [Tooltip("분별 적 종류를 유지합니다. 수량/간격은 아래 30초 생존 설정이 결정합니다.")]
    [SerializeField] private EnemySpawnMinute[] minutes = new EnemySpawnMinute[0];
    [Header("18분 생존 설정 (1차 플레이테스트용)")]
    [SerializeField] private SurvivalBalanceSettings settings = new SurvivalBalanceSettings();
    [SerializeField] private bool logWaveMetrics;
    public SurvivalBalanceSettings Settings => settings ?? (settings = new SurvivalBalanceSettings());
    public float ElapsedTime => (float)elapsed;
    public int CurrentMinute => (int)(elapsed / 60d);
    public bool IsPaused => paused;
    public int SpawnerCount => initialized ? spawners.Length : spawnerCount;
    public IEnemySpawnPool SpawnPool => spawnPool;
    public int AttemptedSpawns { get; private set; }
    public int SuccessfulSpawns { get; private set; }
    public int FrameSkippedAttempts { get; private set; }
    public int CapSkippedAttempts { get; private set; }
    public int PoolFailedAttempts { get; private set; }
    public SurvivalWaveMetrics LastSample { get; private set; }
    public event Action<float> TimeAdvanced;
    public event Action<SurvivalWaveMetrics> Sampled;
    private EnemySpawner[] spawners;
    private IEnemySpawnPool spawnPool;
    private bool initialized, paused;
    private double elapsed;
    private double pauseBoundary = double.PositiveInfinity;
    private int configuredMinute = -1, nextSpawner;
    private SurvivalWaveMetrics window;
    private int playerLevel = 1;
    private bool catchupActive;

    private void Start() { Initialize(); }
    private void Update() { Tick(Time.deltaTime); }
    public void Initialize()
    {
        if (initialized) return;
        spawnPool = poolProvider as IEnemySpawnPool;
        if (spawnPool == null || player == null || playerCamera == null || spawnerTemplate == null
            || minutes == null || minutes.Length == 0 || spawnerCount < 0 || !Settings.IsValid)
        { FailConfiguration(); return; }
        foreach (EnemySpawnMinute minute in minutes)
        {
            if (minute == null || minute.EnemyPrefabs == null || minute.EnemyPrefabs.Length == 0) { FailConfiguration(); return; }
            foreach (GameObject prefab in minute.EnemyPrefabs)
                if (prefab == null || !prefab.TryGetComponent<EnemyHealth>(out _) || !prefab.TryGetComponent<EnemyFollow>(out _)) { FailConfiguration(); return; }
        }
        spawnerTemplate.gameObject.SetActive(false);
        spawners = new EnemySpawner[spawnerCount];
        for (int i = 0; i < spawners.Length; i++)
        {
            spawners[i] = i == 0 ? spawnerTemplate : Instantiate(spawnerTemplate, transform);
            spawners[i].gameObject.name = "Enemy Spawner " + (i + 1);
        }
        initialized = true;
        ConfigureSpawners();
    }

    public void SetPauseBoundary(float time) { pauseBoundary = time >= 0 && !float.IsNaN(time) ? time : double.PositiveInfinity; }
    public void ClearPauseBoundary() { pauseBoundary = double.PositiveInfinity; }

    public void Tick(float deltaTime)
    {
        if (!initialized || paused || !isActiveAndEnabled || Time.timeScale <= 0 || deltaTime <= 0
            || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || elapsed >= Settings.Duration) return;
        double end = Math.Min(Settings.Duration, Math.Min(elapsed + deltaTime, pauseBoundary));
        int budget = Settings.maxAttemptsPerFrame;
        while (elapsed < end && !paused)
        {
            ConfigureSpawners();
            int wave = Settings.WaveIndex(elapsed);
            double boundary = Math.Min(end, (wave + 1) * (double)Settings.waveSeconds);
            int due = Settings.ScheduledAttempts(boundary) - Settings.ScheduledAttempts(elapsed);
            elapsed = boundary;
            // 생성 순간의 시계를 먼저 확정하므로 rawXP가 이 시각에 고정됩니다.
            int execute = elapsed < Settings.Duration && spawners.Length > 0 ? Math.Min(due, budget) : 0;
            window.frameSkipped += due - execute;
            FrameSkippedAttempts += due - execute;
            for (int i = 0; i < execute; i++)
            {
                var spawner = spawners[nextSpawner++ % spawners.Length];
                AttemptedSpawns++; window.attempted++;
                if (spawner.TrySpawnScheduled(AttemptedSpawns - 1, out EnemySpawnFailure failure))
                { SuccessfulSpawns++; window.successful++; }
                else if (failure == EnemySpawnFailure.Capacity) { CapSkippedAttempts++; window.capSkipped++; }
                else { PoolFailedAttempts++; window.poolFailed++; }
            }
            budget -= execute;
            TimeAdvanced?.Invoke(ElapsedTime);
            if (Math.Abs(elapsed - (wave + 1) * (double)Settings.waveSeconds) < .000001) PublishSample(wave);
        }
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (!initialized) return;
        foreach (EnemySpawner spawner in spawners)
            if (spawner != null) { if (paused) spawner.StopSpawning(); else spawner.ResumeSpawning(); }
    }

    private void ConfigureSpawners()
    {
        if (configuredMinute == CurrentMinute) return;
        var types = minutes[Math.Min(CurrentMinute, minutes.Length - 1)].EnemyPrefabs;
        foreach (EnemySpawner spawner in spawners)
        {
            spawner.gameObject.SetActive(false);
            spawner.Configure(spawnPool, player, types, 1f, int.MaxValue, 1, playerCamera, offscreenMargin);
            spawner.gameObject.SetActive(true);
            if (paused) spawner.StopSpawning();
        }
        configuredMinute = CurrentMinute;
    }

    public void RecordActiveCount(int count) { window.peakActive = Math.Max(window.peakActive, count); }
    public void RecordKill() { window.normalKills++; }
    public void RecordDrop(float raw) { window.xpDropped += raw; }
    public void RecordCollection(float raw, float awarded) { window.rawCollected += raw; window.xpAwarded += awarded; }
    public void RecordPlayerState(int level, bool bonus) { playerLevel = level; catchupActive = bonus; }
    private void PublishSample(int wave)
    {
        window.elapsedSeconds = ElapsedTime; window.waveIndex = wave; window.targetAttempts = Settings.TargetAttempts(wave);
        window.activeNormal = spawnPool is IEnemySpawnDiagnostics diagnostics ? diagnostics.ActiveEnemyCount : 0;
        window.playerLevel = playerLevel; window.catchupActive = catchupActive;
        LastSample = window;
        Sampled?.Invoke(window);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (logWaveMetrics) Debug.Log(window.ToCsv(), this);
#endif
        window = new SurvivalWaveMetrics { peakActive = window.activeNormal };
    }
    private void FailConfiguration()
    {
        if (spawnerTemplate != null) spawnerTemplate.gameObject.SetActive(false);
        Debug.LogError("EnemySpawnDirector: 풀/플레이어/카메라/스포너/적 종류 및 생존 설정을 확인하세요.", this);
        enabled = false;
    }
    private void OnDestroy()
    {
        if (!initialized) return;
        foreach (EnemySpawner spawner in spawners)
            if (spawner != null) { spawner.StopSpawning(); if (spawner != spawnerTemplate) Destroy(spawner.gameObject); }
    }

#if UNITY_EDITOR
    [ContextMenu("관측/최근 30초 지표 출력")]
    private void PrintLastSample()
    {
        Debug.Log(SurvivalWaveMetrics.CsvHeader, this);
        Debug.Log(LastSample.ToCsv(), this);
    }

    [ContextMenu("테스트/진행시간 30초 앞으로")]
    private void AdvancePreview()
    {
        if (Application.isPlaying) Tick(30f);
    }
#endif
}
