using UnityEngine;

public sealed class RandomBossPatternSelector : BossPatternSelector
{
    public override BossPattern SelectPattern(float distanceToPlayer, int currentHealth)
    {
        return (BossPattern)Random.Range(0, 3);
    }
}
