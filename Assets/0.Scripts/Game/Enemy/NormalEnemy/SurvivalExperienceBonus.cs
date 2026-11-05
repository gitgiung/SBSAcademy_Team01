using System;

/// <summary>회수 시 적용할 보정과 소수 잔여. 공유 설정 객체를 변경하지 않습니다.</summary>
public sealed class SurvivalExperienceBonus
{
    private readonly SurvivalBalanceSettings settings;
    private int nextCheckpoint;
    public bool Active { get; private set; }
    public double FractionalCarry { get; private set; }
    public double Multiplier => Active ? settings.catchupMultiplier : 1d;

    public SurvivalExperienceBonus(SurvivalBalanceSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Update(double elapsed, int level)
    {
        if (level >= 100) { Active = false; return; }
        while (nextCheckpoint < settings.checkpointSeconds.Length && elapsed >= settings.checkpointSeconds[nextCheckpoint])
        {
            int gap = settings.checkpointLevels[nextCheckpoint++] - level;
            if (gap >= settings.catchupOnGap) Active = true;
            else if (gap <= settings.catchupOffGap) Active = false;
        }
    }

    public double Award(double rawXP)
    {
        if (rawXP <= 0 || double.IsNaN(rawXP) || double.IsInfinity(rawXP)) return 0;
        double exact = rawXP * Multiplier + FractionalCarry;
        double awarded = Math.Floor(exact);
        FractionalCarry = exact - awarded;
        return awarded;
    }

    public void Reset()
    {
        Active = false;
        FractionalCarry = 0;
        nextCheckpoint = 0;
    }
}
