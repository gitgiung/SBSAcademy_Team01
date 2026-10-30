using UnityEngine;

public abstract class BossPatternSelector : MonoBehaviour
{
    public abstract BossPattern SelectPattern(float distanceToPlayer, int currentHealth);

    /// <summary>기존 선택기와의 호환성을 유지하면서 다음 일반 패턴을 선택한다.</summary>
    public virtual BossPattern SelectPattern(float distanceToPlayer, int currentHealth, BossPattern expectedPattern)
    {
        return SelectPattern(distanceToPlayer, currentHealth);
    }
}
