using System.Collections;
using UnityEngine;

public sealed partial class Player
{
    public bool TryUseLightSkill()
    {
        if (!CanAcceptGameplayInput || IsHeavyCharging) return false;
        return TryCastLightSkill(CurrentAim());
    }

    public void NotifyLightAttackAnimationProgress(int actionId, float elapsed) => lightSkill?.OnAnimationProgress(actionId, elapsed);
    public void NotifyLightAttackAnimationFinished(int actionId) => lightSkill?.OnAnimationFinished(actionId);

    /// <summary>장착된 약공격만 시전합니다. 미장착 또는 실패 시 MP를 쓰지 않습니다.</summary>
    public bool TryCastLightSkill(Vector2 aim)
    {
        int id = data != null && data.Loadout != null
            ? data.Loadout.GetEquippedSkillId(PlayerSkillButton.Light) : 0;
        if (id == 0) return false;
        if (!LightSkillCatalog.TryGet(id, out LightSkillCast skill)) return false;
        if (!TryBeginLightAttack(skill.MpCost, out int actionId)) return false;
        if (aim.sqrMagnitude < 0.0001f)
            aim = view != null && view.FacingDirection.sqrMagnitude > 0.0001f ? view.FacingDirection : Vector2.right;
        float stackDamage = data.ScaleAttackDamage(skill.Damage(data.attackDamage), skill.Element)
            * (data.Loadout != null ? data.Loadout.GetAttackSkillDamageMultiplier(skill.Id) : 1f);
        StartCoroutine(CastShots(skill, aim.normalized, stackDamage, true, actionId));
        return true;
    }

    private IEnumerator CastShots(LightSkillCast skill, Vector2 aim, float damage, bool lockUntilEnd, int actionId)
    {
        skill = PrepareSkill(skill);
        float projectileSize = 1f + data.StatCards.Value(PlayerStat.Projectile_Size);
        int shots = Mathf.Max(1, skill.Projectiles);
        float spacing = skill.ScatterVolley ? 0f : skill.ShotSpacing;
        // 태양 탄막은 태양 문양을 먼저 그린 뒤, 긴 가시 끝에서 적을 쫓는 미사일을 한 발씩 쏩니다.
        SunBarrageCrest crest = SunCrestFor(skill);
        int firstSpike = 0;
        if (crest != null)
        {
            crest.Play(crest.DrawSeconds + spacing * (shots - 1) + SunBarrageCrest.LingerSeconds);
            firstSpike = crest.NearestSpike(aim);
            yield return new WaitForSeconds(crest.DrawSeconds);
        }
        for (int i = 0; i < shots; i++)
        {
            if (!isActiveAndEnabled || hasDied || data == null || data.IsDead) break;
            if (!lockUntilEnd && Time.timeScale <= 0f) yield break;
            if (crest != null) SpawnSunMissile(skill, crest, firstSpike + i, aim, damage, projectileSize);
            else
            {
                float spread = shots == 1 || skill.StraightRapidFire ? 0f : (i - (shots - 1) * 0.5f) * 8f;
                Vector2 direction = (Vector2)(Quaternion.Euler(0f, 0f, spread) * (Vector3)aim);
                SpawnLightSkill(skill, direction, damage, projectileSize);
            }
            if (i < shots - 1 && spacing > 0f) yield return new WaitForSeconds(spacing);
        }
        if (!lockUntilEnd) yield break;
        float hold = skill.ScatterVolley ? 0.15f : 0.28f + skill.RepeatSpacing * Mathf.Max(0, skill.Hits - 1);
        if (hold > 0f) yield return new WaitForSeconds(hold);
        CompleteLightAttack(actionId);
    }

    private LightSkillCast PrepareSkill(LightSkillCast skill) => skill.Presented().WithStatUpgrades(
        (int)data.StatCards.Value(PlayerStat.Projectile_Count), 1f + data.StatCards.Value(PlayerStat.Skill_Range));

    /// <summary>보유한 약·강 스킬을 표의 패시브 주기마다 같은 모양으로 발사합니다. MP와 입력 잠금은 쓰지 않습니다.</summary>
    private void TickOwnedPassives(float deltaTime)
    {
        if (IsHeavyCharging || data == null || data.Loadout == null || combat == null) return;
        for (int i = 0; i < ownedPassiveIds.Length; i++)
        {
            Data.SkillData owned = data.Loadout.GetAttackSkill(i);
            int id = owned != null ? owned.DataId : 0;
            if (ownedPassiveIds[i] != id) { ownedPassiveIds[i] = id; ownedPassiveTimers[i] = 0f; }
            if (!CombatSkillTable.TryGet(id, out CombatSkillRecord record)
                || !record.TryOwnedPassive(out float cooldown, out float scale)
                || !LightSkillCast.TryFrom(record, out LightSkillCast skill)) continue;
            ownedPassiveTimers[i] = Mathf.Max(0f, ownedPassiveTimers[i] - deltaTime / data.PassiveCooldownMultiplier);
            if (ownedPassiveTimers[i] > 0f) continue;
            if (!combat.TryGetClosestEnemyDirection(PrepareSkill(skill).Range, out Vector2 direction)) continue;
            ownedPassiveTimers[i] = cooldown;
            float stackMul = data.Loadout.GetAttackSkillDamageMultiplier(id);
            float damage = data.ScaleAttackDamage(skill.Damage(data.attackDamage), skill.Element);
            StartCoroutine(CastShots(skill, direction, damage * scale * stackMul * PassiveDamageFactor, false, 0));
        }
    }

    public bool TryBeginLightAttack(float mpCost, out int actionId)
    {
        actionId = 0;
        if (!CanAcceptGameplayInput || IsHeavyCharging || movement.IsDashing || !defense.CanStart) return false;
        if (!actions.TryBeginLightAttack(mpCost, out actionId))
        {
            if (mpCost > data.CurrentMP) resourceGaugePreview?.FlashInsufficientMp();
            return false;
        }
        joystickController?.StopMovement();
        view.SetMovementAnimation(Vector2.zero, false, false, dashDuration);
        return true;
    }

    /// <summary>애니메이션 실행에 보관한 ID를 반환합니다. 이전 동작의 늦은 종료는 무시합니다.</summary>
    public bool CompleteLightAttack(int actionId) => actions != null && actions.CompleteLightAttack(actionId);
}
