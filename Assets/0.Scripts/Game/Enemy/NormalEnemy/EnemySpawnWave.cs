using System;
using UnityEngine;

[Serializable]
public sealed class EnemySpawnWave
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private GameObject prefab;
        [Tooltip("다른 항목과 비교하는 상대 비율입니다. 0이면 등장하지 않습니다.")]
        [SerializeField, Min(0)] private int weight = 1;

        public GameObject Prefab => prefab;
        public int Weight => weight;
    }

    [SerializeField] private Entry[] enemies = Array.Empty<Entry>();
    public Entry[] Enemies => enemies;

    public bool IsValid
    {
        get
        {
            if (enemies == null) return false;

            bool hasPositiveWeight = false;
            foreach (Entry entry in enemies)
            {
                if (entry == null || entry.Weight < 0 || entry.Prefab == null ||
                    !entry.Prefab.TryGetComponent<EnemyHealth>(out _) ||
                    !entry.Prefab.TryGetComponent<EnemyFollow>(out _)) return false;

                hasPositiveWeight |= entry.Weight > 0;
            }
            return hasPositiveWeight;
        }
    }
}
