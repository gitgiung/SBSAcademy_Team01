using System;
using UnityEngine;

// Keeps enemy lists readable from scenes serialized before wave weights were added.
[Serializable]
public sealed class EnemySpawnMinute
{
    [SerializeField] private GameObject[] enemyPrefabs = Array.Empty<GameObject>();
    public GameObject[] EnemyPrefabs => enemyPrefabs;
}
