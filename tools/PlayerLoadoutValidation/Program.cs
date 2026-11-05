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
        Console.WriteLine($"{tests - failures}/{tests} passed");
        return failures == 0 ? 0 : 1;
    }
}
