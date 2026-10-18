using UnityEngine;

public abstract class BossPatternSelector : MonoBehaviour
{
    public abstract BossPattern SelectPattern(float distanceToPlayer, int currentHealth);
}
