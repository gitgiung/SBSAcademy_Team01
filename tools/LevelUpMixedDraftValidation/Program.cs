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

    private static int Main(string[] args)
    {
        var statCards = PlayerStatTableExporter.Read("Document/PlayerDatatable.xlsx");
        if (args.Length == 1 && args[0] == "--export")
        {
            System.IO.File.WriteAllText("Assets/Resources/Data/PlayerStatTable.json", UnityEngine.JsonUtility.ToJson(statCards, true));
            return 0;
        }
        Test("workbook exports every card and keeps key, unit, weight and cap", () =>
        {
            Check(statCards.Cards.Length == 13);
            Check(statCards.Cards.Select(card => card.Stat).Distinct().Count() == 13);
            Check(statCards.Cards.All(card => card.Weight == 1));
            var regen = statCards.Cards.Single(card => card.Stat == PlayerStat.HP_Regen);
            Check(Math.Abs(regen.Values[1] - .001f) < 1e-6);
            var speed = statCards.Cards.Single(card => card.Stat == PlayerStat.Move_Speed);
            Check(speed.Values[0] == 0 && speed.Values[5] == .25f);
            var xp = statCards.Cards.Single(card => card.Stat == PlayerStat.XP_Gain);
            Check(xp.Values[2] == 1.14f && xp.Limit == 1.5f);
            Check(statCards.Cards.Single(card => card.Stat == PlayerStat.Projectile_Count).Values.Length == 4);
            var json = System.IO.File.ReadAllText("Assets/Resources/Data/PlayerStatTable.json");
            var shipped = UnityEngine.JsonUtility.FromJson<PlayerStatTable>(json);
            Check(UnityEngine.JsonUtility.ToJson(shipped, false) == UnityEngine.JsonUtility.ToJson(statCards, false));
        });
        Test("weighted choices do not duplicate or offer capped stats and reject stale upgrades", () =>
        {
            var progress = new PlayerStatProgress();
            var hp = statCards.Cards.Single(card => card.Stat == PlayerStat.HP);
            var projectiles = statCards.Cards.Single(card => card.Stat == PlayerStat.Projectile_Count);
            Check(progress.Draw(new[] { projectiles, hp }, 3, () => 0).Count == 2);
            Check(progress.TryUpgrade(projectiles, 1));
            Check(!progress.TryUpgrade(projectiles, 1));
            Check(progress.TryUpgrade(projectiles, 2));
            Check(progress.TryUpgrade(projectiles, 3));
            Check(!progress.TryUpgrade(projectiles, 4));
            Check(progress.Value(PlayerStat.Projectile_Count) == 3);
            Check(progress.Draw(new[] { projectiles, hp }, 3, () => 0).Single() == hp);
            Check(new PlayerStatProgress().Value(PlayerStat.Projectile_Count) == 0);
            var capped = new PlayerStatCard { Stat = PlayerStat.DEF, Values = new[] { 0f, 2f, 4f }, Limit = 1f, Weight = 9f };
            Check(progress.Draw(new[] { hp, capped }, 1, () => .5).Single() == capped);
            Check(progress.TryUpgrade(capped, 1) && progress.Value(PlayerStat.DEF) == 1f);
            Check(!progress.TryUpgrade(capped, 2));
        });
        PlayerStatCard Card(PlayerStat stat) => statCards.Cards.Single(card => card.Stat == stat);
        Test("removing a stat resets its level and permits acquiring it again", () =>
        {
            var player = new PlayerData();
            Check(!player.RemoveStatCard(PlayerStat.Projectile_Size));
            Check(player.TryUpgradeStat(Card(PlayerStat.Projectile_Size), 1));
            Check(player.TryUpgradeStat(Card(PlayerStat.Skill_Range), 1));
            Check(player.RemoveStatCard(PlayerStat.Projectile_Size));
            Check(player.StatCards.Level(PlayerStat.Projectile_Size) == 0);
            Check(player.StatCards.Value(PlayerStat.Projectile_Size) == 0f);
            Check(player.StatCards.Level(PlayerStat.Skill_Range) == 1);
            Check(player.TryUpgradeStat(Card(PlayerStat.Projectile_Size), 1));
        });
        Test("removing HP cards restores the original maximum and clamps health", () =>
        {
            var player = new PlayerData { maxHealth = 73f, currentHealth = 60f };
            Check(player.TryUpgradeStat(Card(PlayerStat.HP), 1));
            Check(player.TryUpgradeStat(Card(PlayerStat.HP), 2));
            Check(player.RemoveStatCard(PlayerStat.HP));
            Check(player.maxHealth == 73f && player.currentHealth <= 73f);
            Check(player.StatCards.Level(PlayerStat.HP) == 0);
            Check(player.TryUpgradeStat(Card(PlayerStat.HP), 1));
            Check(player.RemoveStatCard(PlayerStat.HP) && player.maxHealth == 73f);
        });
        Test("PlayerData applies table recovery, health, mitigation and movement", () =>
        {
            var player = new PlayerData { speed = 5f, maxHealth = 100f, currentHealth = 80f, maxMP = 100f };
            var notifications = new List<(float current, float maximum)>();
            player.HealthChanged += (current, maximum) => notifications.Add((current, maximum));
            Check(player.TryUpgradeStat(Card(PlayerStat.HP), 1));
            Check(player.maxHealth == 150f && player.currentHealth == 130f);
            Check(notifications.SequenceEqual(new[] { (130f, 150f) }));
            Check(player.TryUpgradeStat(Card(PlayerStat.HP_Regen), 1));
            Check(player.TryUpgradeStat(Card(PlayerStat.MP_Regen), 1));
            player.TickMPRecovery(2f);
            player.TickHealthRecovery(2f);
            Check(Math.Abs(player.currentHealth - 130.3f) < 1e-5 && Math.Abs(player.CurrentMP - 2.4f) < 1e-5);
            Check(player.TryUpgradeStat(Card(PlayerStat.DEF), 1));
            player.ApplyDamage(10f);
            Check(Math.Abs(player.currentHealth - (130.3f - 10f * (1f - 2f / 35.34f))) < 1e-5);
            Check(player.TryUpgradeStat(Card(PlayerStat.Move_Speed), 1));
            Check(Math.Abs(player.MoveSpeed - 5.25f) < 1e-5);
            Check(player.speed == 5f);
        });
        Test("PlayerData composes damage and applies XP before fractional carry", () =>
        {
            var player = new PlayerData { experiencePickupDistance = 3f };
            Check(player.TryUpgradeStat(Card(PlayerStat.ATK), 1));
            Check(player.TryUpgradeStat(Card(PlayerStat.Elm_All_ATK), 1));
            Check(Math.Abs(player.ScaleAttackDamage(100f) - 110f) < 1e-4);
            Check(Math.Abs(player.ScaleAttackDamage(100f, "ELEM_FIRE") - 121f) < 1e-4);
            Check(player.TryUpgradeStat(Card(PlayerStat.XP_Gain), 1));
            Check(player.TryCollectExperience(10f, out float first) && first == 10f);
            Check(player.TryCollectExperience(10f, out float second) && second == 11f);
            Check(player.TryUpgradeStat(Card(PlayerStat.Pickup_Radius), 1));
            Check(Math.Abs(player.PickupDistance - 3.3f) < 1e-5);
        });

        Test("bonus table uses supplied values and does not fill missing rows", () =>
        {
            var table = new LevelUpStatBonusTable(new[] { (LevelUpStatKind.Damage, "Rare", 2f) });
            Check(!table.TryGetAmount(LevelUpStatKind.Damage, "Common", out _));
            Check(!table.TryGetAmount(LevelUpStatKind.Damage, "Uncommon", out _));
            Check(table.TryGetAmount(LevelUpStatKind.Damage, "Rare", out float amount) && amount == 2f);
        });

        Test("mixed draft is pure for the same random sequence", () =>
        {
            var table = SampleTable();
            var bonuses = SampleBonuses();
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
            var bonuses = SampleBonuses();
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
            var reward = new LevelUpStatChoice(LevelUpStatKind.Damage, "Uncommon", 1f, "Test stat");
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

    private static LevelUpStatBonusTable SampleBonuses() => new LevelUpStatBonusTable(new[]
    {
        (LevelUpStatKind.Damage, "Uncommon", 1f),
        (LevelUpStatKind.Damage, "Rare", 2f),
        (LevelUpStatKind.Damage, "Epic", 3f)
    });
}
