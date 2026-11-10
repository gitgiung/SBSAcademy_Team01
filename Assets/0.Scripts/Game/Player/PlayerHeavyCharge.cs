using System;
using UnityEngine;

[Serializable]
public sealed class PlayerHeavyChargeSettings
{
    [Tooltip("강공격 차징 사용 여부. MP, 최대 차징 시간, 슈퍼아머는 스킬 표를 사용합니다.")]
    public bool Configured;
    [Min(0f)] public float MPCost;
    public float[] StageTimes = new float[0];
    public float[] StageBonuses = new float[0];
    public bool SuperArmor;
}

public readonly struct HeavyChargeRelease
{
    public int ChargeId { get; }
    public int Stage { get; }
    public float Elapsed { get; }
    public float Multiplier { get; }
    public Vector2 Direction { get; }
    public HeavyChargeRelease(int chargeId, int stage, float elapsed, float multiplier, Vector2 direction)
    { ChargeId = chargeId; Stage = stage; Elapsed = elapsed; Multiplier = multiplier; Direction = direction; }
}

/// <summary>차징 기반의 비용·단계·방향 스냅샷. 실제 강공격 효과는 후속 실행부가 소유합니다.</summary>
internal sealed class PlayerHeavyCharge
{
    private readonly PlayerData data;
    private readonly Func<bool> contextValid;
    private int version;
    private float[] times;
    private float[] bonuses;
    public bool IsStarting { get; private set; }
    public bool IsCharging { get; private set; }
    public int CurrentId => IsCharging ? version : 0;
    public int Stage { get; private set; }
    public float Elapsed { get; private set; }
    public float Multiplier { get; private set; } = 1f;
    public Vector2 Direction { get; private set; }
    public bool SuperArmor { get; private set; }
    public float Progress => IsCharging ? Mathf.Clamp01(Elapsed / times[2]) : 0f;
    public PlayerHeavyCharge(PlayerData data, Func<bool> contextValid)
    { this.data = data; this.contextValid = contextValid; }
    private static bool Nonnegative(float n) => n >= 0 && !float.IsNaN(n) && !float.IsInfinity(n);
    private static bool Valid(PlayerHeavyChargeSettings s)
    {
        if (s == null || !s.Configured || s.StageTimes == null || s.StageBonuses == null
            || s.StageTimes.Length != 3 || s.StageBonuses.Length != 3 || s.StageTimes[0] != 0) return false;
        float total = 1;
        for (int i = 0; i < 3; i++)
        {
            if (!Nonnegative(s.StageTimes[i]) || !Nonnegative(s.StageBonuses[i])
                || (i > 0 && s.StageTimes[i] <= s.StageTimes[i - 1])) return false;
            total += s.StageBonuses[i];
        }
        return Nonnegative(total);
    }
    public bool TryBegin(PlayerHeavyChargeSettings settings, Vector2 direction, CombatSkillRecord skill)
    {
        if (IsStarting || IsCharging || !Valid(settings) || skill == null || data == null || data.IsDead
            || Time.timeScale <= 0 || !contextValid()) return false;
        // 동기 MP 알림이 설정을 변경해도 승인한 실행의 설정은 바뀌지 않습니다.
        float cost = skill.CostAmount;
        float fullChargeTime = skill.CastTime;
        if (!Nonnegative(cost) || !Nonnegative(fullChargeTime) || fullChargeTime == 0f) return false;
        var capturedTimes = (float[])settings.StageTimes.Clone();
        capturedTimes[1] = capturedTimes[1] / capturedTimes[2] * fullChargeTime;
        capturedTimes[2] = fullChargeTime;
        var capturedBonuses = (float[])settings.StageBonuses.Clone();
        bool armor = skill.SuperArmor;
        int id = NextVersion(); IsStarting = true;
        bool paid = data.TrySpendMP(cost);
        if (!paid || id != version || !IsStarting || data.IsDead || Time.timeScale <= 0 || !contextValid())
        {
            if (id == version) Cancel();
            if (paid) data.RestoreMP(cost); // 실제 차징을 시작하지 못한 경우만 복구합니다.
            return false;
        }
        times = capturedTimes; bonuses = capturedBonuses; SuperArmor = armor;
        IsStarting = false; IsCharging = true; Elapsed = 0; Direction = Vector2.down;
        UpdateDirection(direction); UpdateStage(); return true;
    }
    public void Tick(float delta, Vector2 direction)
    {
        if (!IsCharging) return;
        if (data == null || data.IsDead) { Cancel(); return; }
        if (Time.timeScale <= 0 || !Nonnegative(delta)) return;
        if (!contextValid()) { Cancel(); return; }
        Elapsed = Mathf.Min(times[2], Elapsed + delta); UpdateDirection(direction); UpdateStage();
    }
    private void UpdateDirection(Vector2 direction)
    {
        if (!float.IsNaN(direction.x) && !float.IsInfinity(direction.x) && !float.IsNaN(direction.y)
            && !float.IsInfinity(direction.y) && direction.sqrMagnitude > .0001f) Direction = direction.normalized;
    }
    private void UpdateStage()
    {
        Stage = 0; Multiplier = 1;
        for (int i = 0; i < times.Length; i++)
            if (Elapsed >= times[i]) { Stage++; Multiplier += bonuses[i]; }
    }
    public bool TryRelease(int id, out HeavyChargeRelease result)
    {
        result = default;
        if (!IsCharging || id != version || Time.timeScale <= 0) return false;
        if (data == null || data.IsDead || !contextValid()) { Cancel(); return false; }
        result = new HeavyChargeRelease(id, Stage, Elapsed, Multiplier, Direction);
        Cancel(); return true;
    }
    public void Cancel()
    {
        NextVersion(); IsStarting = false; IsCharging = false; Stage = 0; Elapsed = 0;
        Multiplier = 1; SuperArmor = false; times = null; bonuses = null;
    }
    private int NextVersion()
    { unchecked { version++; } if (version == 0) version++; return version; }
}
