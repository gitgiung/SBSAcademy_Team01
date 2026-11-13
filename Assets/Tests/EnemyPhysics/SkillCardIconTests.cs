#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class SkillCardIconTests
{
    [TestCase(10011)]
    [TestCase(20007)]
    [TestCase(30001)]
    public void SelectionAndInventoryUseTheSamePptIconWithBlankJsonPath(int skillId)
    {
        Sprite expected = Resources.Load<Sprite>("SkillIcons/Skill_" + skillId);
        Assert.IsNotNull(expected);
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var record = new CombatSkillRecord { Id = skillId, Name = "아이콘 테스트", Icon = "", Rarity = "Common" };
        var card = (LevelUpCardData)typeof(CombatPopupController).GetMethod("CreateSkillCard", flags)
            .Invoke(null, new object[] { record, new List<SkillRarityRecord>(), null });
        Assert.AreSame(expected, card.Icon);
        var owned = new Data.SkillData { DataId = skillId, IconLabel = "" };
        Assert.AreSame(expected, typeof(PlayerStatusPanel).GetMethod("SkillIcon", flags)
            .Invoke(null, new object[] { owned }));
    }

    [Test]
    public void AllRuntimeSkillsHaveImportedDefaultIcons()
    {
        var table = CombatSkillTable.CreateSnapshot();
        Assert.AreEqual(46, table.skills.Count);
        foreach (var skill in table.skills)
            Assert.IsNotNull(SkillCardIcons.Get(skill.Id), skill.Id + " " + skill.Name);
    }
}
#endif
