using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>씬의 일반 몬스터 소유자. 기존 Managers.Pool을 사용하고 동시 활성 제한을 적용합니다.</summary>
[DisallowMultipleComponent]
public sealed class SceneEnemyPool : MonoBehaviour, IEnemySpawnPool
{
    public const int MaxActiveEnemies = 130;
    private readonly Dictionary<GameObject, uint> activeEnemies = new Dictionary<GameObject, uint>();
    private readonly List<GameObject> staleEnemies = new List<GameObject>();
    private PoolManager pool;
    private bool closing;

    public int ActiveEnemyCount
    {
        get
        {
            // 외부 Destroy나 공통 풀 Clear 이후에도 제한이 영구히 잠기지 않도록 정리합니다.
            staleEnemies.Clear();
            foreach (var pair in activeEnemies)
                if (pair.Key == null || !pair.Key.TryGetComponent<PooledObject>(out var item) ||
                    !item.IsRented || item.LeaseVersion != pair.Value)
                    staleEnemies.Add(pair.Key);
            foreach (GameObject enemy in staleEnemies) activeEnemies.Remove(enemy);
            return activeEnemies.Count;
        }
    }

    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene)
    {
        if (closing || !isActiveAndEnabled || target == null || prefab == null || !scene.IsValid() || !scene.isLoaded ||
            !prefab.TryGetComponent<EnemyHealth>(out _) ||
            !prefab.TryGetComponent<EnemyFollow>(out _) || ActiveEnemyCount >= MaxActiveEnemies)
            return false;

        if (pool == null) pool = Managers.Pool;
        if (pool == null) return false;
        GameObject spawned = null;
        try
        {
            pool.Pop(prefab, position, Quaternion.identity, initialize: enemy =>
            {
                spawned = enemy;
                // 활성 몬스터는 씬 루트에 두어 스포너 비활성화와 씬 종료 수명이 분리됩니다.
                enemy.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(enemy, scene);
                enemy.GetComponent<EnemyHealth>().PrepareForSpawn(this);
                enemy.GetComponent<EnemyFollow>().SetTarget(target);
                activeEnemies.Add(enemy, enemy.GetComponent<PooledObject>().LeaseVersion);
            });
            return true;
        }
        catch
        {
            if (spawned != null) activeEnemies.Remove(spawned);
            throw;
        }
    }

    public void ReturnEnemy(GameObject enemy)
    {
        if (enemy == null || !activeEnemies.TryGetValue(enemy, out uint lease)) return;
        activeEnemies.Remove(enemy);
        // 공통 풀을 통해 이미 반납·재대여됐다면 이전 소유자는 현재 상태에도 손대지 않습니다.
        if (!enemy.TryGetComponent<PooledObject>(out var item) ||
            !item.IsRented || item.LeaseVersion != lease) return;
        if (enemy.TryGetComponent<EnemyFollow>(out var follow)) follow.SetTarget(null);
        item.ReturnToPool(lease);
    }

    private void OnDisable() { ReturnAll(); }

    private void OnDestroy()
    {
        closing = true;
        ReturnAll();
    }

    private void ReturnAll()
    {
        if (activeEnemies.Count == 0) return;
        // Managers를 조회해 종료 중 새 전역 객체를 만들지 않습니다.
        GameObject[] remaining = new GameObject[activeEnemies.Count];
        activeEnemies.Keys.CopyTo(remaining, 0);
        foreach (GameObject enemy in remaining) ReturnEnemy(enemy);
        activeEnemies.Clear();
    }
}
