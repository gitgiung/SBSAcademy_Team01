using UnityEngine;

/// <summary>Contract for a pickup spawned by an enemy in the EnemyMvp.Runtime assembly.</summary>
public interface IExperiencePickup
{
    void Initialize(Transform target);
}
