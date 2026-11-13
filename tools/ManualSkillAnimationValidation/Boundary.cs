using System;
using UnityEngine;

// 독립 검증용 주변 경계. 실제 PlayerView/PlayerDefenseVfx/Animator는 원본을 사용한다.
public class Player : MonoBehaviour
{
    public const float DefaultParryVfxScale = 1f;
    public float ParryVfxScale => 1f;
    internal PlayerLightSkill Skill;
    internal PlayerActionGate Gate;
    public object TimingSkill => Skill;
    public bool TimingLocked => Gate.IsInputLocked;
    public void CheckTimingAnimation() => Skill.CheckAnimation();
    public bool CanAcceptGameplayInput => Gate != null && !Gate.IsInputLocked;
    public int ActiveLightAttackId => Skill != null ? Skill.ActionId : 0;
    public void InitializeTiming(object view, PlayerData data)
    {
        Gate = new PlayerActionGate(data);
        Skill = new PlayerLightSkill(this, data, (PlayerView)view);
    }
    public bool TryBeginLightAttack(float cost, out int id) => Gate.TryBeginLightAttack(cost, out id);
    public bool CompleteLightAttack(int id) => Gate.CompleteLightAttack(id);
    public void NotifyLightAttackAnimationProgress(int id, float elapsed) => Skill?.OnAnimationProgress(id, elapsed);
    public void NotifyLightAttackAnimationFinished(int id) => Skill?.OnAnimationFinished(id);
}
public class PlayerData
{
    public float currentHealth = 10, maxHealth = 10, maxMP = 10, maxEXP = 10;
    public float CurrentMP => 10;
    public float CurrentEXP => 0;
    public event Action<float, float> HealthChanged, MPChanged, EXPChanged;
    public bool IsDead;
    public float bulletSpeed = 5, AttackDamage = 1;
    public ValidationLoadout Loadout = new ValidationLoadout();
    public float ScaleAttackDamage(float damage) => damage;
    public bool TrySpendMP(float cost) => true;
    public void RestoreMP(float cost) { }
}
public enum PlayerSkillButton { Light, Heavy }
public class ValidationLoadout
{
    public int UnlockedAttackSlots => 0;
    public int GetEquippedSkillId(PlayerSkillButton button) => 0;
    public Data.SkillData GetAttackSkill(int slot) => null;
}
namespace Data
{
    public class SkillData
    {
        public int DataId;
        public bool ActiveConfigured;
        public float MPCost, BaseDamage, DamageMultiplier, AnimImpactDuration;
        public string AnimName;
    }
}
public static class UiFontScale { public static float Size(float value) => value * 1.3f; }
public class UiFontScaledMarker : MonoBehaviour { }
