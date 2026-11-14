using System;
using UnityEngine;

public enum EnemyRosterMode { ManualWaves, StageMixing }

/// <summary>6단계 프리팹의 교체 비율과 종류별 비중만 계산합니다. 시간은 Director가 소유합니다.</summary>
[Serializable]
public sealed class EnemySpawnProgression
{
    public const int StageCount = 6;
    public const float StageSeconds = 180f;

    [Tooltip("3분 경계부터 이전 단계를 교체하는 시간입니다. 0이면 즉시 교체합니다. 플레이테스트 초기값 90초.")]
    [SerializeField, Range(0, StageSeconds)] private float transitionSeconds = 90f;
    [SerializeField] private EnemySpawnArchetype[] types =
    {
        new EnemySpawnArchetype("Enemy_movespeed2"),
        new EnemySpawnArchetype("Enemy_movespeed3"),
        new EnemySpawnArchetype("Enemy_movespeed4"),
        new EnemySpawnArchetype("ZombieHound")
    };

    public bool IsValid
    {
        get
        {
            if (float.IsNaN(transitionSeconds) || float.IsInfinity(transitionSeconds) ||
                transitionSeconds < 0 || transitionSeconds > StageSeconds || types == null || types.Length == 0) return false;
            long earlyTotal = 0, lateTotal = 0;
            foreach (EnemySpawnArchetype type in types)
            {
                if (type == null || !type.IsValid) return false;
                earlyTotal += type.EarlyWeight;
                lateTotal += type.LateWeight;
            }
            return earlyTotal > 0 && lateTotal > 0;
        }
    }

    /// <summary>고정 슬롯을 반환하며 0 가중치는 기존 스케줄러에서 제외합니다. 웨이브 경계에서만 호출합니다.</summary>
    public void BuildRoster(double elapsed, double duration, out GameObject[] prefabs, out int[] weights)
    {
        if (!IsValid || double.IsNaN(elapsed) || double.IsInfinity(elapsed) ||
            double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0)
            throw new ArgumentException("단계 혼합의 프리팹, 가중치, 전환 시간과 진행 시간을 확인하세요.");

        double time = Math.Max(0, Math.Min(duration, elapsed));
        int stage = (int)Math.Min(StageCount - 1, time / StageSeconds);
        double mix = stage == 0 || transitionSeconds == 0 ? 1 :
            Math.Min(1, (time - stage * StageSeconds) / transitionSeconds);
        double progress = time / duration;
        prefabs = new GameObject[types.Length * StageCount];
        weights = new int[prefabs.Length];
        for (int i = 0; i < types.Length; i++)
        {
            EnemySpawnArchetype type = types[i];
            double typeWeight = type.EarlyWeight + (type.LateWeight - type.EarlyWeight) * progress;
            for (int s = 0; s < StageCount; s++)
            {
                int index = i * StageCount + s;
                prefabs[index] = type.StagePrefabs[s];
                double share = s == stage ? mix : stage > 0 && s == stage - 1 ? 1 - mix : 0;
                // 정수 상대 가중치의 1/1000 단위까지 보간합니다. 각 종류의 최대 가중치는 100입니다.
                weights[index] = (int)Math.Round(typeWeight * share * 1000, MidpointRounding.AwayFromZero);
            }
        }
    }
}

[Serializable]
public sealed class EnemySpawnArchetype
{
    [SerializeField] private string typeName;
    [Tooltip("게임 시작 시 종류별 상대 가중치. 0이면 초반에 나오지 않습니다.")]
    [SerializeField, Range(0, 100)] private int earlyWeight = 1;
    [Tooltip("게임 종료 시 종류별 상대 가중치. 중간은 진행 시간에 따라 보간합니다.")]
    [SerializeField, Range(0, 100)] private int lateWeight = 1;
    [Tooltip("0–3분, 3–6분, …, 15–18분 프리팹 순서입니다.")]
    [SerializeField] private GameObject[] stagePrefabs = new GameObject[EnemySpawnProgression.StageCount];

    public EnemySpawnArchetype(string typeName) { this.typeName = typeName; }
    public int EarlyWeight => earlyWeight;
    public int LateWeight => lateWeight;
    public GameObject[] StagePrefabs => stagePrefabs;
    public bool IsValid
    {
        get
        {
            if (earlyWeight < 0 || earlyWeight > 100 || lateWeight < 0 || lateWeight > 100 ||
                stagePrefabs == null || stagePrefabs.Length != EnemySpawnProgression.StageCount) return false;
            foreach (GameObject prefab in stagePrefabs)
                if (prefab == null || !prefab.TryGetComponent<EnemyHealth>(out _) ||
                    !prefab.TryGetComponent<EnemyFollow>(out _)) return false;
            return true;
        }
    }
}
