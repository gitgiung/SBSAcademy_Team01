using System;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class Player
{
    /// <summary>장착한 전략 스킬을 시전합니다.</summary>
    public bool TryUseTacticalSkill()
    {
        // 약공격 잠금과 별개로 전략은 기존 보호막처럼 사망·정지 중에만 거절합니다.
        if (data == null || hasDied || data.IsDead || Time.timeScale <= 0f) return false;
        if (Time.time < nextTacticalReadyTime) return false;
        int id = data.Loadout != null ? data.Loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) : 0;
        if (id == 0) return false;
        if (!CombatSkillTable.TryGet(id, out CombatSkillRecord record)) return false;
        int cost = TacticalUseCost(record);
        if (cost > 0 && GetTacticalUsesRemaining(id) < cost) return false;
        if (!TacticalSkillEffect.TryApply(data, record)) return false;
        if (cost > 0) ConsumeTacticalUses(id, cost);
        tacticalCooldownDuration = Mathf.Max(0f, record.Cooldown);
        nextTacticalReadyTime = Time.time + tacticalCooldownDuration;
        return true;
    }

    /// <summary>Skill 버튼에 표시할 현재 전략의 남은 스테이지 횟수입니다.</summary>
    public int GetEquippedTacticalUsesRemaining()
    {
        int id = data != null && data.Loadout != null
            ? data.Loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) : 0;
        return GetTacticalUsesRemaining(id);
    }

    public int GetTacticalUsesRemaining(int skillId)
    {
        if (skillId <= 0) return 0;
        if (!tacticalUsesRemaining.TryGetValue(skillId, out int remaining))
        {
            remaining = TacticalStageMaxUses;
            tacticalUsesRemaining[skillId] = remaining;
        }
        return remaining;
    }

    public void ResetTacticalStageUses()
    {
        tacticalUsesRemaining.Clear();
        TacticalUsesChanged?.Invoke();
    }

    private void ConsumeTacticalUses(int skillId, int cost)
    {
        int remaining = Mathf.Max(0, GetTacticalUsesRemaining(skillId) - Mathf.Max(1, cost));
        tacticalUsesRemaining[skillId] = remaining;
        TacticalUsesChanged?.Invoke();
    }

    private static int TacticalUseCost(CombatSkillRecord record)
    {
        if (record == null) return 0;
        if (!string.Equals(record.CostType, "Count", StringComparison.OrdinalIgnoreCase)) return 0;
        return Mathf.Max(1, Mathf.RoundToInt(record.CostAmount));
    }

    /// <summary>이전 보호막 버튼 경로 호환용입니다.</summary>
    public void TryUseShieldSkill() => TryUseTacticalSkill();

    private void BindTacticalButton()
    {
        if (shieldButton == null)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].gameObject.scene == gameObject.scene && buttons[i].name == "Skill")
                {
                    shieldButton = buttons[i];
                    break;
                }
            }
        }
        if (shieldButton == null) return;
        // 약공격과 같이 포인터 다운에서 바로 시전합니다. Inspector 영구 이벤트와 무관합니다.
        shieldButton.onClick.RemoveListener(TryUseShieldSkill);
        Util.GetOrAddComponent<PlayerTacticalSkillButton>(shieldButton.gameObject).Bind(this);
        Util.GetOrAddComponent<SprintCooldownUI>(shieldButton.gameObject).BindTactical(this);
    }
}
