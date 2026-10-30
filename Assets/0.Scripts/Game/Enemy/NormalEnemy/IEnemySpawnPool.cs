using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>게임 스폰 규칙과 공통 풀을 연결하는 경계. Enemy 어셈블리는 Framework에 의존하지 않습니다.</summary>
public interface IEnemySpawnPool
{
    bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene);
    void ReturnEnemy(GameObject enemy);
}
