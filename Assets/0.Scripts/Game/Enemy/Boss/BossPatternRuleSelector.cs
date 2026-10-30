public sealed class BossPatternRuleSelector : BossPatternSelector
{
    public override BossPattern SelectPattern(float distanceToPlayer, int currentHealth)
    {
        return BossPattern.Pattern1;
    }

    public override BossPattern SelectPattern(float distanceToPlayer, int currentHealth, BossPattern expectedPattern)
    {
        return expectedPattern == BossPattern.Pattern2 ? BossPattern.Pattern2 : BossPattern.Pattern1;
    }
}
