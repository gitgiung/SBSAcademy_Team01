using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>일반 적/XP의 씬 소유권과 lease를 추적합니다. 저장은 기존 Managers.Pool을 사용합니다.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed partial class SceneEnemyPool : MonoBehaviour, IEnemySpawnPool, IEnemySpawnDiagnostics, IEnemyExperienceDropper
{
    public const int MaxActiveEnemies = 130;
    private const float RecycleInterval = 0.25f;

    [SerializeField] private Camera recycleCamera;
    [SerializeField, Min(0.01f), Tooltip("카메라 화면 가장자리에서 이 거리보다 먼 일반 적을 반환합니다.")]
    private float offscreenRecycleMargin = 20f;

    private readonly List<KeyValuePair<PooledObject, uint>> recycleCandidates = new List<KeyValuePair<PooledObject, uint>>();
    private float recycleTimer;
    private readonly Dictionary<PooledObject, uint> activeEnemies = new Dictionary<PooledObject, uint>();
    private readonly Dictionary<PooledObject, uint> activePickups = new Dictionary<PooledObject, uint>();
    private readonly SurvivalBalanceSettings fallbackSettings = new SurvivalBalanceSettings();
    private PoolManager pool;
    private EnemySpawnDirector director;
    private bool closing;
    public int ActiveEnemyCount => activeEnemies.Count;
    public EnemySpawnFailure LastSpawnFailure { get; private set; }

    private void LateUpdate()
    {
        if (closing || activeEnemies.Count == 0 || recycleCamera == null || !recycleCamera.isActiveAndEnabled ||
            !recycleCamera.orthographic || Time.deltaTime <= 0f || (director != null && director.IsPaused)) return;

        recycleTimer -= Time.deltaTime;
        if (recycleTimer > 0f) return;
        recycleTimer = RecycleInterval;

        float viewHeight = 2f * recycleCamera.orthographicSize;
        float verticalPadding = offscreenRecycleMargin / viewHeight;
        float horizontalPadding = verticalPadding / recycleCamera.aspect;
        foreach (var entry in activeEnemies)
        {
            if (entry.Key == null) continue;
            Vector3 viewport = recycleCamera.WorldToViewportPoint(entry.Key.transform.position);
            if (viewport.x < -horizontalPadding || viewport.x > 1f + horizontalPadding ||
                viewport.y < -verticalPadding || viewport.y > 1f + verticalPadding)
                recycleCandidates.Add(entry);
        }

        // LeaseEnded updates activeEnemies during return. Validate each lease after collecting candidates.
        foreach (var entry in recycleCandidates)
            if (entry.Key != null && activeEnemies.TryGetValue(entry.Key, out uint lease) && lease == entry.Value)
                ReturnEnemy(entry.Key.gameObject);
        recycleCandidates.Clear();
    }

    private void ResolveDirector()
    {
        if (director != null) return;
        foreach (EnemySpawnDirector candidate in FindObjectsByType<EnemySpawnDirector>())
            if (candidate.gameObject.scene == gameObject.scene) { director = candidate; break; }
    }

    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene) => TrySpawn(prefab, position, target, scene, null);

    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene, System.Action<GameObject> spawned)
    {
        ResolveDirector();
        LastSpawnFailure = EnemySpawnFailure.InvalidConfiguration;
        if (closing || !isActiveAndEnabled || target == null || prefab == null || !scene.IsValid() || !scene.isLoaded
            || !prefab.TryGetComponent<EnemyHealth>(out _) || !prefab.TryGetComponent<EnemyFollow>(out _)) return false;
        fieldItemTarget = target;
        if (ActiveEnemyCount >= (director != null ? director.Settings.maxActiveNormalEnemies : MaxActiveEnemies))
        { LastSpawnFailure = EnemySpawnFailure.Capacity; return false; }
        if (pool == null) pool = Managers.Pool;
        LastSpawnFailure = EnemySpawnFailure.PoolUnavailable;
        if (pool == null) return false;
        try
        {
            pool.Pop(prefab, position, Quaternion.identity, initialize: enemy =>
            {
                enemy.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(enemy, scene);
                var health = enemy.GetComponent<EnemyHealth>();
                health.PrepareForSpawn(this, (director != null ? director.Settings : fallbackSettings).RawXP(director != null ? director.ElapsedTime : 0));
                health.Died -= HandleEnemyDied;
                health.Died += HandleEnemyDied;
                health.Defeated -= HandleEnemyDefeated;
                health.Defeated += HandleEnemyDefeated;
                enemy.GetComponent<EnemyFollow>().SetTarget(target);
                var item = enemy.GetComponent<PooledObject>();
                item.LeaseEnded += HandleEnemyReturned;
                activeEnemies.Add(item, item.LeaseVersion);
                if (director != null) director.RecordActiveCount(ActiveEnemyCount);
                spawned?.Invoke(enemy);
            });
            LastSpawnFailure = EnemySpawnFailure.None;
            return true;
        }
        catch (System.Exception exception) { Debug.LogException(exception, this); return false; }
    }

    private void HandleEnemyDied() { if (director != null) director.RecordKill(); }
    private void HandleEnemyReturned(PooledObject item, uint lease)
    {
        item.LeaseEnded -= HandleEnemyReturned;
        if (item.TryGetComponent<EnemyHealth>(out var health))
        {
            health.Died -= HandleEnemyDied;
            health.Defeated -= HandleEnemyDefeated;
        }
        if (activeEnemies.TryGetValue(item, out uint owned) && owned == lease) activeEnemies.Remove(item);
    }
    private void HandlePickupReturned(PooledObject item, uint lease)
    {
        item.LeaseEnded -= HandlePickupReturned;
        if (activePickups.TryGetValue(item, out uint owned) && owned == lease) activePickups.Remove(item);
    }

    public void ReturnEnemy(GameObject enemy)
    {
        if (enemy == null || !enemy.TryGetComponent<PooledObject>(out var item) || !activeEnemies.TryGetValue(item, out uint lease)) return;
        if (!item.IsRented || item.LeaseVersion != lease) { activeEnemies.Remove(item); return; }
        if (enemy.TryGetComponent<EnemyFollow>(out var follow)) follow.SetTarget(null);
        item.ReturnToPool(lease);
    }

    public bool TryDropExperience(GameObject prefab, Vector3 position, Transform target, float rawExperience)
    {
        if (closing || !isActiveAndEnabled || prefab == null || !prefab.TryGetComponent<IPooledExperiencePickup>(out _)) return false;
        if (pool == null) pool = Managers.Pool;
        if (pool == null) return false;
        try
        {
            pool.Pop(prefab, position, Quaternion.identity, initialize: pickup =>
            {
                pickup.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(pickup, gameObject.scene);
                var reward = pickup.GetComponent<IPooledExperiencePickup>();
                reward.PrepareForSpawn(target, rawExperience, HandleCollected);
                var item = pickup.GetComponent<PooledObject>();
                activePickups.Add(item, item.LeaseVersion);
                item.LeaseEnded += HandlePickupReturned;
            });
            if (director != null)
            {
                float amount = rawExperience >= 0 ? rawExperience : prefab.GetComponent<IPooledExperiencePickup>().RawExperience;
                director.RecordDrop(amount);
            }
            return true;
        }
        catch (System.Exception exception) { Debug.LogException(exception, this); return false; }
    }

    private void HandleCollected(float raw, float awarded) { if (director != null) director.RecordCollection(raw, awarded); }
    private void OnApplicationQuit() { closing = true; activeEnemies.Clear(); activePickups.Clear(); activeFieldItems.Clear(); }
    private void OnDisable() { ReturnAll(); }
    private void OnDestroy() { closing = true; ReturnAll(); }
    private void ReturnAll() { ReturnOwned(activeEnemies); ReturnOwned(activePickups); DiscardFieldItems(); }
    private static void ReturnOwned(Dictionary<PooledObject, uint> items)
    {
        var remaining = new List<KeyValuePair<PooledObject, uint>>(items);
        foreach (var pair in remaining) if (pair.Key != null) pair.Key.ReturnToPool(pair.Value);
        items.Clear();
    }
}
