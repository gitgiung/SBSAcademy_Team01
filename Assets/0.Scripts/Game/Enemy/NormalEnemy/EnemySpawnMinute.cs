using System;
using UnityEngine;

[Serializable]
public sealed class EnemySpawnMinute
{
    [Tooltip("이 분에 사용할 종류. 여러 개면 목록 순서대로 순환합니다.")]
    [SerializeField] private GameObject[] enemyPrefabs = new GameObject[0];
    [Tooltip("n: 배치 간격(초). 첫 배치도 이 시간 후 생성합니다.")]
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [Tooltip("m: 스포너 하나가 생성할 총 마릿수.")]
    [SerializeField, Min(0)] private int totalSpawnCount = 5;
    [Tooltip("l: 한 배치에서 생성할 마릿수.")]
    [SerializeField, Min(1)] private int batchSpawnCount = 1;

    public GameObject[] EnemyPrefabs => enemyPrefabs;
    public float SpawnInterval => spawnInterval;
    public int TotalSpawnCount => totalSpawnCount;
    public int BatchSpawnCount => batchSpawnCount;

    public bool IsValid
    {
        get
        {
            if (spawnInterval < 0.01f || float.IsNaN(spawnInterval) || float.IsInfinity(spawnInterval) ||
                totalSpawnCount < 0 || batchSpawnCount < 1 || enemyPrefabs == null || enemyPrefabs.Length == 0)
                return false;
            foreach (GameObject prefab in enemyPrefabs)
                if (prefab == null || !prefab.TryGetComponent<EnemyFollow>(out _) ||
                    !prefab.TryGetComponent<EnemyHealth>(out _)) return false;
            return true;
        }
    }
}
