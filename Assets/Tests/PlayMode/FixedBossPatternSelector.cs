#if UNITY_EDITOR
public sealed class FixedBossPatternSelector : BossPatternSelector
{
    public int Calls { get; private set; }
    public float LastDistance { get; private set; }
    public int LastHealth { get; private set; }

    public override BossPattern SelectPattern(float distanceToPlayer, int currentHealth)
    {
        Calls++;
        LastDistance = distanceToPlayer;
        LastHealth = currentHealth;
        return BossPattern.Pattern2;
    }
}
#endif
