using System;
using System.Reflection;

internal static class Program
{
    private static int tests, failures;
    private static void Check(bool condition) { if (!condition) throw new Exception("assertion failed"); }
    private static bool Unequip(PlayerLoadout loadout, PlayerSkillButton button)
    {
        MethodInfo method = typeof(PlayerLoadout).GetMethod("TryUnequip");
        if (method == null) throw new Exception("TryUnequip is missing");
        return (bool)method.Invoke(loadout, new object[] { button });
    }
    private static PlayerLoadout Owned()
    {
        var loadout = new PlayerLoadout();
        Check(loadout.TryAddAttackSkill(new Data.SkillData { DataId = 10001, Name = "불꽃 탄환" }));
        Check(loadout.TryAddAttackSkill(new Data.SkillData { DataId = 10002, Name = "화염 폭발" }));
        Check(loadout.TryAddAttackSkill(new Data.SkillData { DataId = 20001, Name = "화염 망치" }));
        return loadout;
    }
    private static void Test(string name, Action action)
    {
        tests++;
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    private static int Main()
    {
        Test("unequip preserves ownership and emits once", () => {
            var l = Owned(); Check(l.TryEquip(PlayerSkillButton.Light, 10001));
            int changes = 0; l.Changed += () => changes++;
            Check(Unequip(l, PlayerSkillButton.Light)); Check(l.GetEquippedSkillId(PlayerSkillButton.Light) == 0);
            Check(l.GetAttackSkill(0).DataId == 10001); Check(changes == 1);
            Check(!Unequip(l, PlayerSkillButton.Light)); Check(changes == 1);
            Check(l.TryEquip(PlayerSkillButton.Light, 10001));
        });
        Test("replacement and independent heavy assignment", () => {
            var l = Owned(); Check(l.TryEquip(PlayerSkillButton.Light, 10001));
            Check(l.TryEquip(PlayerSkillButton.Heavy, 20001)); Check(l.TryEquip(PlayerSkillButton.Light, 10002));
            Check(l.GetEquippedSkillId(PlayerSkillButton.Light) == 10002);
            Check(Unequip(l, PlayerSkillButton.Light)); Check(l.GetEquippedSkillId(PlayerSkillButton.Heavy) == 20001);
        });
        Test("wrong categories and unowned IDs are refused", () => {
            var l = Owned(); Check(!l.TryEquip(PlayerSkillButton.Light, 20001));
            Check(!l.TryEquip(PlayerSkillButton.Heavy, 10001)); Check(!l.TryEquip(PlayerSkillButton.Light, 10003));
            Check(!Unequip(l, (PlayerSkillButton)99));
        });
        Test("locked slots and removed equipped skills", () => {
            var l = Owned(); Check(l.GetAttackSkill(6) == null);
            for (int id = 10003; id <= 10005; id++) Check(l.TryAddAttackSkill(new Data.SkillData { DataId = id }));
            Check(!l.TryAddAttackSkill(new Data.SkillData { DataId = 10006 }));
            Check(!l.TryEquip(PlayerSkillButton.Light, 10006));
            Check(l.TryEquip(PlayerSkillButton.Light, 10001)); Check(l.RemoveSkill(10001));
            Check(l.GetEquippedSkillId(PlayerSkillButton.Light) == 0);
        });
        Test("duplicate skill upgrades stack without taking a slot", () => {
            var l = new PlayerLoadout();
            var skill = new Data.SkillData { DataId = 10001, Name = "불꽃 탄환" };
            Check(l.TryAddOrUpgradeAttackSkill(skill));
            Check(l.GetAttackSkillStack(10001) == 1);
            Check(PlayerLoadout.DamageMultiplierForStack(1) == 1f);
            Check(l.TryAddOrUpgradeAttackSkill(skill));
            Check(l.GetAttackSkillStack(10001) == 2);
            Check(l.GetAttackSkillDamageMultiplier(10001) == 1.2f);
            Check(l.GetAttackSkill(1) == null);
            Check(l.TryAddOrUpgradeAttackSkill(skill));
            Check(l.GetAttackSkillStack(10001) == 3);
            Check(l.GetAttackSkillDamageMultiplier(10001) == 1.5f);
            Check(l.IsAttackSkillAtMaxStack(10001));
            Check(!l.TryAddOrUpgradeAttackSkill(skill));
            Check(!l.TryAddAttackSkill(skill));
            Check(l.RemoveSkill(10001));
            Check(l.GetAttackSkillStack(10001) == 0);
        });
        Test("tactical ownership equip and free slot", () => {
            var l = new PlayerLoadout();
            Check(l.HasFreeTacticalSlot);
            Check(l.TryAddTacticalSkill(new Data.SkillData { DataId = 30002, Name = "복원 치유" }));
            Check(l.TryEquip(PlayerSkillButton.Tactical, 30002));
            Check(l.GetEquippedSkillId(PlayerSkillButton.Tactical) == 30002);
            Check(!l.TryAddTacticalSkill(new Data.SkillData { DataId = 30002, Name = "복원 치유" }));
            Check(!l.TryEquip(PlayerSkillButton.Tactical, 30001));
            for (int id = 30003; id <= 30006; id++)
                Check(l.TryAddTacticalSkill(new Data.SkillData { DataId = id, Name = "t" + id }));
            Check(!l.HasFreeTacticalSlot);
            Check(!l.TryAddTacticalSkill(new Data.SkillData { DataId = 30007, Name = "수호 기도" }));
        });
        Console.WriteLine($"{tests - failures}/{tests} passed");
        return failures == 0 ? 0 : 1;
    }
}
