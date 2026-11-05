using UnityEngine;

/// <summary>Contract for a pickup spawned by an enemy in the EnemyMvp.Runtime assembly.</summary>
public interface IExperiencePickup
{
    void Initialize(Transform target);
}

/// <summary>일반 몬스터 처치 보상의 풀 초기화 경계. Framework 타입은 노출하지 않습니다.</summary>
public interface IPooledExperiencePickup : IExperiencePickup
{
    float RawExperience { get; }
    void PrepareForSpawn(Transform target, float rawExperience, System.Action<float, float> collected);
}

public interface IEnemyExperienceDropper
{
    bool TryDropExperience(GameObject prefab, Vector3 position, Transform target, float rawExperience);
}
