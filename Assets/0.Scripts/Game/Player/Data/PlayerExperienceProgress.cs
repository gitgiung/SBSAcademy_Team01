using System;

/// <summary>레벨·잔여 XP·미완료 선택의 단일 런타임 상태. UI와 Unity 수명을 소유하지 않습니다.</summary>
public sealed class PlayerExperienceProgress
{
    private readonly PlayerExperienceSettings settings;
    public int Level { get; private set; } = 1;
    public double Experience { get; private set; }
    public int RequiredXP => settings.RequiredXP(Level);
    public int PendingLevelUps { get; private set; }

    public PlayerExperienceProgress(PlayerExperienceSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public bool Add(double amount)
    {
        if (amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)
            || double.IsInfinity(Experience + amount)) return false;
        return SetExperience(Experience + amount);
    }

    public bool SetExperience(double amount)
    {
        if (amount < 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return false;
        Experience = amount;
        while (Level < PlayerExperienceSettings.MaxLevel && Experience >= RequiredXP)
        {
            Experience -= RequiredXP;
            Level++;
            PendingLevelUps++;
        }
        return true;
    }

    public bool CompleteLevelUp()
    {
        if (PendingLevelUps == 0) return false;
        PendingLevelUps--;
        return true;
    }

    public void CancelPendingLevelUps() => PendingLevelUps = 0;

    public void Reset()
    {
        Level = 1;
        Experience = 0;
        PendingLevelUps = 0;
    }
}
