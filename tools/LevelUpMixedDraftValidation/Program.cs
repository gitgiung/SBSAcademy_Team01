using System;
using System.Collections.Generic;
using System.Linq;

internal static class Program
{
    private static int tests, failures;

    private static void Check(bool condition, string detail = null)
    {
        if (!condition) throw new Exception(detail ?? "assertion failed");
    }

    private static void Test(string name, Action action)
    {
        tests++;
        try
        {
            action();
            Console.WriteLine("PASS " + name);
        }
        catch (Exception e)
        {
            failures++;
            Console.WriteLine("FAIL " + name + ": " + e.Message);
        }
    }

    private static CombatSkillTableFile SampleTable()
    {
        return new CombatSkillTableFile
        {
            rarities = new List<SkillRarityRecord>
            {
                new SkillRarityRecord { Id = "Common", Weight = 60f },
                new SkillRarityRecord { Id = "Uncommon", Weight = 23f },
                new SkillRarityRecord { Id = "Rare", Weight = 11f },
                new SkillRarityRecord { Id = "Epic", Weight = 4f },
                new SkillRarityRecord { Id = "Legendary", Weight = 1.7f },
                new SkillRarityRecord { Id = "Mythic", Weight = 0.3f },
                new SkillRarityRecord { Id = "Fusion", Weight = 0f }
            },
            skills = new List<CombatSkillRecord>
            {
                new CombatSkillRecord { Id = 10001, Name = "A", Rarity = "Common" },
                new CombatSkillRecord { Id = 10002, Name = "B", Rarity = "Uncommon" },
                new CombatSkillRecord { Id = 10003, Name = "C", Rarity = "Rare" },
                new CombatSkillRecord { Id = 10004, Name = "D", Rarity = "Epic" }
            }
        };
    }

    private static Func<double> Sequence(params double[] values)
    {
        int index = 0;
        return () =>
        {
            if (index >= values.Length) throw new Exception("random sequence exhausted at " + index);
            return values[index++];
        };
    }

    private static int Main()
    {
        Test("temporary table has Uncommon~Mythic only and graded amounts", () =>
        {
            var table = LevelUpStatBonusTable.CreateTemporaryDamageDefaults();
            Check(!table.TryGetAmount(LevelUpStatKind.Damage, "Common", out _));
            Check(!table.TryGetAmount(LevelUpStatKind.Damage, "Fusion", out _));
            Check(table.TryGetAmount(LevelUpStatKind.Damage, "Uncommon", out float u) && Math.Abs(u - 0.05f) < 1e-6);
            Check(table.TryGetAmount(LevelUpStatKind.Damage, "Rare", out float r) && Math.Abs(r - 0.08f) < 1e-6);
            Check(table.TryGetAmount(LevelUpStatKind.Damage, "Epic", out float e) && Math.Abs(e - 0.12f) < 1e-6);
            Check(table.TryGetAmount(LevelUpStatKind.Damage, "Legendary", out float l) && Math.Abs(l - 0.18f) < 1e-6);
            Check(table.TryGetAmount(LevelUpStatKind.Damage, "Mythic", out float m) && Math.Abs(m - 0.25f) < 1e-6);
            Check(u < r && r < e && e < l && l < m);
        });

        Test("mixed draft is pure for the same random sequence", () =>
        {
            var table = SampleTable();
            var bonuses = LevelUpStatBonusTable.CreateTemporaryDamageDefaults();
            double[] rolls =
            {
                0.9, 0.0, 0.0, // skill share miss -> stat; pick first weighted uncommon
                0.1, 0.0, 0.0, // skill; pick from remaining
                0.1, 0.0, 0.0
            };
            var a = LevelUpMixedDraft.DrawChoices(table, bonuses, 3, 10000, 20000, Sequence(rolls));
            var b = LevelUpMixedDraft.DrawChoices(table, bonuses, 3, 10000, 20000, Sequence(rolls));
            Check(a.Count == b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Check(a[i].Kind == b[i].Kind);
                Check(a[i].Id == b[i].Id);
                Check(string.Equals(a[i].Rarity, b[i].Rarity, StringComparison.OrdinalIgnoreCase));
            }
        });

        Test("stat-only pack never emits Common or Fusion", () =>
        {
            var table = SampleTable();
            var bonuses = LevelUpStatBonusTable.CreateTemporaryDamageDefaults();
            // Always prefer stats (skillShare 0).
            var rolls = Enumerable.Repeat(0.0, 32).ToArray();
            var choices = LevelUpMixedDraft.DrawChoices(
                table, bonuses, 3, 10000, 20000, Sequence(rolls), null, 0, 1);
            Check(choices.Count == 3);
            Check(choices.All(c => c.Kind == LevelUpRewardKind.Stat));
            Check(choices.All(c =>
                !string.Equals(c.Rarity, "Common", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(c.Rarity, "Fusion", StringComparison.OrdinalIgnoreCase)));
            var rarities = choices.Select(c => c.Rarity).ToList();
            Check(rarities.Distinct(StringComparer.OrdinalIgnoreCase).Count() == rarities.Count);
            foreach (var choice in choices.Cast<LevelUpStatChoice>())
                Check(bonuses.TryGetAmount(choice.StatKind, choice.Rarity, out float amount)
                    && Math.Abs(amount - choice.Amount) < 1e-6);
        });

        Test("null applier refuses without side effects", () =>
        {
            var reward = new LevelUpStatChoice(LevelUpStatKind.Damage, "Uncommon", 0.05f, "피해량 증가");
            Check(!NullLevelUpRewardApplier.Instance.TryApply(reward, null));
            Check(!NullLevelUpRewardApplier.Instance.TryApply(null, null));
        });

        Test("skill choice wraps record identity", () =>
        {
            var skill = new CombatSkillRecord { Id = 10002, Name = "B", Rarity = "Uncommon" };
            var choice = new LevelUpSkillChoice(skill);
            Check(choice.Kind == LevelUpRewardKind.Skill);
            Check(choice.Id == 10002);
            Check(choice.Skill == skill);
        });

        Console.WriteLine($"{tests - failures}/{tests} passed");
        return failures == 0 ? 0 : 1;
    }
}
