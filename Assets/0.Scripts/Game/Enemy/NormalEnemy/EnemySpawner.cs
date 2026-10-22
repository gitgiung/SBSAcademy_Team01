using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [SerializeField, Min(0)] private int totalSpawnCount = 5;

    private int spawnedCount;
    private Coroutine spawnRoutine;
    private bool spawningStopped;

    private void OnEnable()
    {
        BeginSpawning();
    }

    private void BeginSpawning()
    {
        if (spawningStopped) return;
        if (spawnedCount >= totalSpawnCount)
            return;

        if (!HasRequiredReferences())
            return;

        // Spawn before starting the coroutine so a one-enemy wave leaves no stale handle.
        SpawnEnemy();
        if (spawnedCount < totalSpawnCount)
            spawnRoutine = StartCoroutine(SpawnRemaining());
    }

    public void StopSpawning()
    {
        spawningStopped = true;
        CancelSpawnRoutine();
    }

    public void RestartSpawning()
    {
        CancelSpawnRoutine();
        spawnedCount = 0;
        spawningStopped = false;
        if (isActiveAndEnabled) BeginSpawning();
    }

    private IEnumerator SpawnRemaining()
    {
        while (spawnedCount < totalSpawnCount)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
            if (!HasRequiredReferences())
                break;

            SpawnEnemy();
        }

        spawnRoutine = null;
    }

    private bool HasRequiredReferences()
    {
        if (player != null && enemyPrefab != null && enemyPrefab.TryGetComponent<EnemyFollow>(out _))
            return true;

        Debug.LogWarning("EnemySpawner requires a player and an enemy prefab with EnemyFollow on its root.", this);
        return false;
    }

    private void SpawnEnemy()
    {
        Transform point = spawnPoints != null && spawnPoints.Length > 0
            ? spawnPoints[spawnedCount % spawnPoints.Length]
            : null;
        Vector3 position = point != null ? point.position : transform.position;
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(enemy, gameObject.scene);
        enemy.GetComponent<EnemyFollow>().SetTarget(player);
        enemy.SetActive(true);
        spawnedCount++;
    }

    private void OnDisable()
    {
        CancelSpawnRoutine();
    }

    private void CancelSpawnRoutine()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        totalSpawnCount = Mathf.Max(0, totalSpawnCount);
    }
}
