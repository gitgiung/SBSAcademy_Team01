using UnityEngine;

/// <summary>
/// Main 보스 클립의 애니메이션 이벤트 수신용. SoulArena에서는 판정을
/// SoulBossApproachSlash·손 히트박스가 소유하므로 여기서는 아무것도 하지 않는다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulSpriteAnimationEventSink : MonoBehaviour
{
    public int SlashEvents { get; private set; }

    public void AnimationEvent_ApplySlashDamage() => SlashEvents++;
    public void AnimationEvent_ApplyAreaDamage() { }
    public void AnimationEvent_AttackAnimationFinished() { }
}
