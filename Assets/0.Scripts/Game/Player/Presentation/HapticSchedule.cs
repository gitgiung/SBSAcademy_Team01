/// <summary>전투 타격 순간의 진동 길이와, 가벼운 진동이 겹치지 않는 간격입니다.</summary>
public enum HapticCue
{
    Parry,
    Guard,
    Hurt,
    Hit
}

public static class HapticSchedule
{
    public const long ParryMilliseconds = 40;
    public const long GuardMilliseconds = 22;
    public const long HurtMilliseconds = 35;
    public const long HitMilliseconds = 20;
    public const int ParryAmplitude = 255;
    public const int GuardAmplitude = 90;
    public const int HurtAmplitude = 200;
    public const int HitAmplitude = 55;
    public const long SoftGapMilliseconds = 90;

    public static long Milliseconds(HapticCue cue)
    {
        switch (cue)
        {
            case HapticCue.Parry: return ParryMilliseconds;
            case HapticCue.Guard: return GuardMilliseconds;
            case HapticCue.Hurt: return HurtMilliseconds;
            case HapticCue.Hit: return HitMilliseconds;
            default: return 0;
        }
    }

    public static int Amplitude(HapticCue cue)
    {
        switch (cue)
        {
            case HapticCue.Parry: return ParryAmplitude;
            case HapticCue.Guard: return GuardAmplitude;
            case HapticCue.Hurt: return HurtAmplitude;
            case HapticCue.Hit: return HitAmplitude;
            default: return 0;
        }
    }

    /// <summary>
    /// 패링과 피격은 항상 울립니다. 가드 막기와 공격 적중은 직전 진동 뒤 간격이 지나야 합니다.
    /// 패링과 피격도 그 간격을 다시 잡아, 바로 이어지는 가벼운 진동이 겹치지 않게 합니다.
    /// </summary>
    public static bool Accept(HapticCue cue, long nowMilliseconds, ref long nextSoftMilliseconds)
    {
        bool strong = cue == HapticCue.Parry || cue == HapticCue.Hurt;
        bool soft = cue == HapticCue.Guard || cue == HapticCue.Hit;
        if (!strong && !soft) return false;
        if (soft && nowMilliseconds < nextSoftMilliseconds) return false;
        nextSoftMilliseconds = nowMilliseconds + SoftGapMilliseconds;
        return true;
    }
}
