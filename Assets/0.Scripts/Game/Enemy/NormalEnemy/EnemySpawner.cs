using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class EnemySpawner : MonoBehaviour
{
    // 기존 씬의 참조와 단독 스포너 설정을 유지합니다.
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [SerializeField, Min(0)] private int totalSpawnCount = 5;
    [SerializeField, Min(1)] private int batchSpawnCount = 1;

    private IEnemySpawnPool spawnPool;
    private GameObject[] enemyPrefabs;
    private Camera playerCamera;
    private float offscreenMargin;
    private int spawnedCount;
    private float spawnTimer;
    private bool spawningStopped;
    private bool externallyDriven;

    public int SpawnedCount => spawnedCount;
    public bool IsComplete => spawnedCount >= totalSpawnCount;

    /// <summary>분별 설정을 복사해 한 작업의 종류와 수량을 고정합니다. 비활성 상태에서 구성합니다.</summary>
    public void Configure(IEnemySpawnPool pool, Transform target, GameObject[] prefabs,
        float interval, int total, int batch, Camera camera, float margin)
    {
        if (pool == null || target == null || camera == null || prefabs == null || prefabs.Length == 0)
            throw new ArgumentException("EnemySpawner requires pool, player, camera and enemy prefabs.");
        foreach (GameObject prefab in prefabs)
            if (prefab == null || !prefab.TryGetComponent<EnemyFollow>(out _) ||
                !prefab.TryGetComponent<EnemyHealth>(out _))
                throw new ArgumentException("Each pooled enemy prefab requires EnemyFollow and EnemyHealth on its root.");
        spawnPool = pool;
        player = target;
        enemyPrefabs = (GameObject[])prefabs.Clone();
        playerCamera = camera;
        spawnInterval = Mathf.Max(0.01f, interval);
        totalSpawnCount = Mathf.Max(0, total);
        batchSpawnCount = Mathf.Max(1, batch);
        offscreenMargin = Mathf.Max(0.01f, margin);
        spawnedCount = 0;
        spawnTimer = 0f;
        spawningStopped = false;
        externallyDriven = true;
        TryPlaceOutsideCamera();
    }

    private void OnEnable()
    {
        if (IsComplete) gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!externallyDriven) Tick(Time.deltaTime);
    }

    /// <summary>Director 또는 단독 Update가 전달하는 시간. 한 프레임에는 한 배치만 생성합니다.</summary>
    public void Tick(float deltaTime)
    {
        if (!isActiveAndEnabled || spawningStopped || IsComplete || deltaTime <= 0f) return;
        spawnTimer += deltaTime;
        if (spawnTimer < spawnInterval) return;
        spawnTimer = 0f;
        if (player == null) return;
        if (externallyDriven && !TryPlaceOutsideCamera()) return;

        int count = Mathf.Min(batchSpawnCount, totalSpawnCount - spawnedCount);
        for (int i = 0; i < count; i++)
        {
            if (!SpawnEnemy()) break;
            spawnedCount++;
        }
        if (IsComplete) gameObject.SetActive(false);
    }

    public void StopSpawning() { spawningStopped = true; }
    public void ResumeSpawning() { spawningStopped = false; }

    /// <summary>씬 전체의 시도 예산에서 한 번 소비합니다. 실패를 로컬 성공 수로 보충하지 않습니다.</summary>
    public bool TrySpawnScheduled(int sequence, out EnemySpawnFailure failure)
    {
        failure = EnemySpawnFailure.InvalidConfiguration;
        if (!isActiveAndEnabled || spawningStopped || spawnPool == null || player == null || !TryPlaceOutsideCamera()) return false;
        GameObject prefab = enemyPrefabs[sequence % enemyPrefabs.Length];
        if (!spawnPool.TrySpawn(prefab, transform.position, player, gameObject.scene))
        {
            failure = spawnPool is IEnemySpawnDiagnostics diagnostics ? diagnostics.LastSpawnFailure : EnemySpawnFailure.PoolUnavailable;
            return false;
        }
        spawnedCount++;
        failure = EnemySpawnFailure.None;
        return true;
    }

    public void RestartSpawning()
    {
        spawnedCount = 0;
        spawnTimer = 0f;
        spawningStopped = false;
        gameObject.SetActive(true);
    }

    private bool SpawnEnemy()
    {
        if (externallyDriven)
        {
            GameObject prefab = enemyPrefabs[spawnedCount % enemyPrefabs.Length];
            return spawnPool.TrySpawn(prefab, transform.position, player, gameObject.scene);
        }
        // 기존 씬의 단독 스포너는 연결 컴포넌트 없이도 계속 작동합니다.
        if (enemyPrefab == null || !enemyPrefab.TryGetComponent<EnemyFollow>(out _)) return false;
        Transform point = spawnPoints != null && spawnPoints.Length > 0
            ? spawnPoints[spawnedCount % spawnPoints.Length] : null;
        Vector3 position = point != null ? point.position : transform.position;
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(enemy, gameObject.scene);
        enemy.GetComponent<EnemyFollow>().SetTarget(player);
        enemy.SetActive(true);
        return true;
    }

    private bool TryPlaceOutsideCamera()
    {
        if (playerCamera == null) return false;
        float z = player.position.z;
        // XY 평면에 카메라 모서리를 투영합니다. 화면비와 카메라 회전도 반영합니다.
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 4; i++)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
            if (!plane.Raycast(ray, out float distance)) return false;
            Vector3 corner = ray.GetPoint(distance);
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }
        Vector3 position = new Vector3(UnityEngine.Random.Range(min.x, max.x),
            UnityEngine.Random.Range(min.y, max.y), z);
        switch (UnityEngine.Random.Range(0, 4))
        {
            case 0: position.x = min.x - offscreenMargin; break;
            case 1: position.x = max.x + offscreenMargin; break;
            case 2: position.y = min.y - offscreenMargin; break;
            default: position.y = max.y + offscreenMargin; break;
        }
        transform.position = position;
        return true;
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        totalSpawnCount = Mathf.Max(0, totalSpawnCount);
        batchSpawnCount = Mathf.Max(1, batchSpawnCount);
    }
}
