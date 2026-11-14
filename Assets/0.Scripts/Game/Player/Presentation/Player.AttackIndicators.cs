using UnityEngine;

public sealed partial class Player
{
    private void LateUpdate() => RefreshAttackMpIndicators();

    private void RefreshAttackMpIndicators()
    {
        if (data == null || (lightAttackCooldown == null && heavyAttackCooldown == null)) return;

        var loadout = data.Loadout;
        if (lightAttackCooldown != null)
        {
            int lightId = loadout.GetEquippedSkillId(PlayerSkillButton.Light);
            bool ready = LightSkillCatalog.TryGet(lightId, out var skill) && data.CurrentMP >= skill.MpCost;
            if (lightAttackCooldown.activeSelf == ready) lightAttackCooldown.SetActive(!ready);
        }
        if (heavyAttackCooldown != null)
        {
            int heavyId = loadout.GetEquippedSkillId(PlayerSkillButton.Heavy);
            bool ready = CombatSkillTable.TryGet(heavyId, out var record) &&
                LightSkillCatalog.TryCreateHeavy(record, out var skill) && data.CurrentMP >= skill.MpCost;
            if (heavyAttackCooldown.activeSelf == ready) heavyAttackCooldown.SetActive(!ready);
        }
    }
}
