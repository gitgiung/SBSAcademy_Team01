using System;
using UnityEngine;

/// <summary>18분 생존의 1차 플레이테스트 설정. 현재 시간/카운트/보정 상태는 보관하지 않습니다.</summary>
[Serializable]
public sealed class SurvivalBalanceSettings
{
    [Min(.01f)] public float waveSeconds = 30f;
    [Min(1)] public int waveCount = 36;
    [Min(0)] public int firstWaveAttempts = 50;
    [Min(0)] public int attemptsIncrease = 10;
    [Min(1)] public int maxAttemptsPerFrame = 8;
    [Min(1)] public int maxActiveNormalEnemies = 130;
    [Min(.01f)] public float xpStageSeconds = 180f;
    public float[] rawXPByStage = { 2f, 3f, 4f, 6f, 7f, 9f };
    public float[] checkpointSeconds = { 180f, 360f, 540f, 720f, 900f };
    public int[] checkpointLevels = { 19, 35, 51, 70, 87 };
    [Min(1)] public int catchupOnGap = 3;
    [Min(0)] public int catchupOffGap = 1;
    [Min(1f)] public float catchupMultiplier = 1.25f;
    public double Duration => (double)waveSeconds * waveCount;

    public bool IsValid
    {
        get
        {
            if (!Positive(waveSeconds) || waveCount < 1 || firstWaveAttempts < 0 || attemptsIncrease < 0
                || maxAttemptsPerFrame < 1 || maxActiveNormalEnemies < 1 || !Positive(xpStageSeconds)
                || rawXPByStage == null || rawXPByStage.Length == 0 || checkpointSeconds == null
                || checkpointLevels == null || checkpointSeconds.Length != checkpointLevels.Length
                || catchupOffGap < 0 || catchupOnGap <= catchupOffGap || !Positive(catchupMultiplier)
                || catchupMultiplier < 1f) return false;
            foreach (float xp in rawXPByStage) if (!Positive(xp)) return false;
            for (int i = 0; i < checkpointSeconds.Length; i++)
                if (!Positive(checkpointSeconds[i]) || (i > 0 && checkpointSeconds[i] <= checkpointSeconds[i - 1])
                    || checkpointLevels[i] < 1 || checkpointLevels[i] > 100) return false;
            return true;
        }
    }

    private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    public int WaveIndex(double elapsed) => Math.Min(waveCount - 1, Math.Max(0, (int)(Math.Max(0, elapsed) / waveSeconds)));
    public int TargetAttempts(int wave) => (int)Math.Min(int.MaxValue,
        (long)firstWaveAttempts + (long)attemptsIncrease * Math.Max(0, wave));

    /// <summary>구간의 반 간격부터 균등 분산한 누적 예정 수. 마지막 시도도 종료 시각 전에 배치합니다.</summary>
    public int ScheduledAttempts(double elapsed)
    {
        elapsed = Math.Max(0, Math.Min(Duration, elapsed));
        int completed = Math.Min(waveCount, (int)(elapsed / waveSeconds));
        long count = (long)completed * firstWaveAttempts + (long)attemptsIncrease * completed * (completed - 1) / 2;
        if (completed < waveCount)
            count += Math.Min(TargetAttempts(completed), (long)Math.Floor(
                (elapsed - completed * (double)waveSeconds) * TargetAttempts(completed) / waveSeconds + .5));
        return (int)Math.Min(int.MaxValue, count);
    }

    public float RawXP(double elapsed) => rawXPByStage[Math.Min(rawXPByStage.Length - 1,
        Math.Max(0, (int)(Math.Max(0, elapsed) / xpStageSeconds)))];
}
