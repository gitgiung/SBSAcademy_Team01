using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>진행 시간 기준의 보스 등장 설정. 진행 상태는 씬의 Encounter가 소유합니다.</summary>
[CreateAssetMenu(menuName = "Game/Boss Encounter Schedule", fileName = "BossEncounterSchedule")]
public sealed class BossEncounterSchedule : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [Tooltip("보스전 시간을 제외한 진행 타이머 기준, 초 단위입니다.")]
        [SerializeField, Min(.01f)] private float appearTime;
        [Tooltip("루트에 EnemyHealth와 지원 보스 컴포넌트를 가진 프리팹. 미지정 회차는 건너뜁니다.")]
        [SerializeField] private GameObject bossPrefab;
        [Tooltip("Boss Spawn Anchor에서 떨어진 월드 좌표 오프셋입니다.")]
        [SerializeField] private Vector3 spawnOffset;

        public float AppearTime => appearTime;
        public GameObject BossPrefab => bossPrefab;
        public Vector3 SpawnOffset => spawnOffset;

        public Entry(float appearTime, GameObject bossPrefab = null, Vector3 spawnOffset = default)
        {
            this.appearTime = appearTime;
            this.bossPrefab = bossPrefab;
            this.spawnOffset = spawnOffset;
        }

        // Unity 직렬화 및 Inspector 항목 추가용.
        public Entry() { }
    }

    [SerializeField] private Entry[] entries =
    {
        new Entry(180f), new Entry(360f), new Entry(540f),
        new Entry(720f), new Entry(900f), new Entry(1080f)
    };

    public IReadOnlyList<Entry> Entries => entries ?? Array.Empty<Entry>();

    public bool TryValidateTimes(out string error)
    {
        float previous = 0f;
        for (int i = 0; i < Entries.Count; i++)
        {
            Entry entry = Entries[i];
            if (entry == null || !Finite(entry.AppearTime) || entry.AppearTime <= previous)
            {
                error = $"BossEncounterSchedule: {i + 1}회차 등장 시각은 양수이며 이전 회차보다 커야 합니다.";
                return false;
            }
            Vector3 offset = entry.SpawnOffset;
            if (!Finite(offset.x) || !Finite(offset.y) || !Finite(offset.z))
            {
                error = $"BossEncounterSchedule: {i + 1}회차 생성 오프셋이 유효하지 않습니다.";
                return false;
            }
            previous = entry.AppearTime;
        }
        error = null;
        return true;
    }

    public static bool IsSupportedBoss(GameObject prefab)
    {
        if (prefab == null || !prefab.TryGetComponent<EnemyHealth>(out var health) || !health.enabled)
            return false;
        return (prefab.TryGetComponent<BossController>(out var beast) && beast.enabled &&
                prefab.GetComponent<BossPatternSelector>() != null) ||
               (prefab.TryGetComponent<HordeLordBoss>(out var horde) && horde.enabled) ||
               (prefab.TryGetComponent<ZombieSlimeBoss>(out var slime) && slime.enabled);
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
