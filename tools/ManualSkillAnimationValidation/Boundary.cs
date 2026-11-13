using System;
using UnityEngine;

// 독립 검증용 주변 경계. 실제 PlayerView/PlayerDefenseVfx/Animator는 원본을 사용한다.
public class Player : MonoBehaviour
{
    public const float DefaultParryVfxScale = 1f;
    public float ParryVfxScale => 1f;
    public int ActiveLightAttackId => 0;
    public void NotifyLightAttackAnimationProgress(int id, float elapsed) { }
    public void NotifyLightAttackAnimationFinished(int id) { }
}
public class PlayerData
{
    public float currentHealth = 10, maxHealth = 10, maxMP = 10, maxEXP = 10;
    public float CurrentMP => 10;
    public float CurrentEXP => 0;
    public event Action<float, float> HealthChanged, MPChanged, EXPChanged;
}
public static class UiFontScale { public static float Size(float value) => value * 1.3f; }
public class UiFontScaledMarker : MonoBehaviour { }
