using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

internal static class Program
{
    private static int failures;
    private static int tests;
    private static CombatSkillTableFile table;
    private static string workbookPath;
    private static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual)) throw new Exception($"expected {expected}, actual {actual}");
    }
    private static object Field(CombatSkillRecord skill, string name)
    {
        var field = typeof(CombatSkillRecord).GetField(name);
        if (field == null) throw new Exception($"missing exported field: {name}");
        return field.GetValue(skill);
    }
    private static CombatSkillRecord Skill(int id) => table.skills.Single(s => s.Id == id);
    private static void Test(string name, Action action)
    {
        tests++;
        try { action(); Console.WriteLine($"PASS {name}"); }
        catch (Exception e) { failures++; Console.WriteLine($"FAIL {name}: {e.Message}"); }
    }
    private static int Main(string[] args)
    {
        // 엑셀이 열려 있어 원본을 바꾸지 못할 때 SKILL_WORKBOOK으로 사본을 검증할 수 있습니다.
        workbookPath = Environment.GetEnvironmentVariable("SKILL_WORKBOOK");
        if (string.IsNullOrEmpty(workbookPath)) workbookPath = "Document/SkillDatatable.xlsx";
        table = CombatSkillWorkbook.Read(workbookPath);
        var shipped = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>(
            File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
        Test("workbook reimport preserves every runtime field except authored icon paths", () =>
        {
            Equal(shipped.skills.Count, table.skills.Count);
            foreach (var expected in shipped.skills)
            {
                var actual = Skill(expected.Id);
                foreach (var field in typeof(CombatSkillRecord).GetFields())
                {
                    if (field.IsStatic || field.Name == "Icon") continue;
                    if (!Equals(field.GetValue(expected), field.GetValue(actual)))
                        throw new Exception($"{expected.Id} {field.Name}: JSON={field.GetValue(expected)}, workbook={field.GetValue(actual)}");
                }
            }
        });
        foreach (int id in new[] { 10011, 20007, 20009 })
        {
            Test($"skill {id} scales range at every range card level", () =>
            {
                var record = shipped.skills.Single(s => s.Id == id);
                Equal(true, LightSkillCast.TryFrom(record, out var skill));
                for (int level = 1; level <= 5; level++)
                {
                    float multiplier = 1f + level * 0.1f;
                    var upgraded = skill.Presented().WithStatUpgrades(0, multiplier);
                    Equal(record.Range * multiplier, upgraded.Range);
                    Equal(record.Width * multiplier, upgraded.Width);
                    Equal(record.Angle, upgraded.Angle);
                }
            });
        }
        foreach (int id in new[] { 10006, 10009 })
        {
            Test($"projectile {id} keeps range independent of range cards", () =>
            {
                var record = shipped.skills.Single(s => s.Id == id);
                Equal(true, LightSkillCast.TryFrom(record, out var skill));
                var upgraded = skill.Presented().WithStatUpgrades(2, 1.5f);
                Equal(LightSkillShape.Projectile, upgraded.Shape);
                Equal(record.Range, upgraded.Range);
                Equal(record.Projectiles + 2, upgraded.Projectiles);
            });
        }
        Test("acquired skill is owned once and not automatically equipped", () =>
        {
            var loadout = new PlayerLoadout(); int changes = 0; loadout.Changed += () => changes++;
            Equal(true, Acquire(loadout, Skill(10002)));
            Equal(10002, loadout.GetAttackSkill(0).DataId);
            Equal(Skill(10002).Name, loadout.GetAttackSkill(0).Name);
            Equal(Skill(10002).Icon, loadout.GetAttackSkill(0).IconLabel);
            Equal(0, loadout.GetEquippedSkillId(PlayerSkillButton.Light));
            Equal(1, loadout.GetAttackSkillStack(10002)); Equal(1, changes);
        });
        Test("duplicate acquisition upgrades stack until max without a new slot", () =>
        {
            var loadout = new PlayerLoadout();
            Equal(true, Acquire(loadout, Skill(10002)));
            Equal(true, Acquire(loadout, Skill(10002)));
            Equal(2, loadout.GetAttackSkillStack(10002));
            Equal(1.2f, loadout.GetAttackSkillDamageMultiplier(10002));
            Equal(null, loadout.GetAttackSkill(1));
            for (int level = 3; level <= PlayerLoadout.MaxAttackSkillStack; level++)
            {
                Equal(true, Acquire(loadout, Skill(10002)));
                Equal(level, loadout.GetAttackSkillStack(10002));
                Equal(1.5f, loadout.GetAttackSkillDamageMultiplier(10002));
            }
            Equal(false, Acquire(loadout, Skill(10002)));
            Equal(null, loadout.GetAttackSkill(1));
        });
        Test("special upgrades roll by shape and stack independent of level", () =>
        {
            LightSkillCast Presented(int id)
            {
                var record = shipped.skills.Single(skill => skill.Id == id);
                Equal(true, LightSkillCast.TryFrom(record, out LightSkillCast skill));
                return skill.Presented();
            }
            var bullet = Presented(10001);
            Equal(LightSkillShape.Projectile, bullet.Shape);
            Equal(SkillUpgradeKind.Projectiles, bullet.RollUpgrade(() => 0));
            Equal(SkillUpgradeKind.Range, bullet.RollUpgrade(() => 0.999));
            var twoShots = bullet.WithUpgrade(SkillUpgradeKind.Projectiles).WithUpgrade(SkillUpgradeKind.Projectiles);
            Equal(bullet.Projectiles + 2, twoShots.Projectiles);
            Equal(bullet.Range, twoShots.Range);
            Equal(bullet.Width, twoShots.Width);
            var ranged = twoShots.WithUpgrade(SkillUpgradeKind.Range);
            Equal(bullet.Range * LightSkillCast.UpgradeStep, ranged.Range);
            Equal(true, bullet.DescribeNext(1, SkillUpgradeKind.Projectiles).Contains("피해"));
            Equal(true, bullet.DescribeNext(3, SkillUpgradeKind.Projectiles).Contains("투사체 1→2"));
            Equal(true, bullet.DescribeNext(6, SkillUpgradeKind.Projectiles).Contains("투사체 1→2"));
            Equal(true, ranged.DescribeNext(5, SkillUpgradeKind.Range).Contains("사거리"));
            Equal(1.5f, PlayerLoadout.DamageMultiplierForStack(7));

            var smash = Presented(10003);
            Equal(LightSkillShape.Circle, smash.Shape);
            Equal(SkillUpgradeKind.Range, smash.RollUpgrade(() => 0));
            Equal(SkillUpgradeKind.Hits, smash.RollUpgrade(() => 0.999));
            var wider = smash.WithUpgrade(SkillUpgradeKind.Range);
            Equal(smash.Range * LightSkillCast.UpgradeStep, wider.Range);
            Equal(smash.Range * LightSkillCast.UpgradeStep * LightSkillCast.UpgradeStep, wider.WithUpgrade(SkillUpgradeKind.Range).Range);
            Equal(smash.Hits + 1, smash.WithUpgrade(SkillUpgradeKind.Hits).Hits);
            Equal(smash.Projectiles, smash.WithUpgrade(SkillUpgradeKind.Projectiles).Projectiles);
            Equal(true, smash.DescribeNext(3, SkillUpgradeKind.Range).Contains("범위"));
            Equal(true, smash.WithUpgrade(SkillUpgradeKind.Hits).DescribeNext(4, SkillUpgradeKind.Hits).Contains("타격 2→3"));

            var spear = Presented(10005);
            Equal(LightSkillShape.Line, spear.Shape);
            Equal(spear.Width * LightSkillCast.UpgradeStep, spear.WithUpgrade(SkillUpgradeKind.Range).Width);
            Equal(true, spear.DescribeNext(6, SkillUpgradeKind.Range).Contains("길이·두께"));

            var fan = Presented(10011);
            Equal(LightSkillShape.Cone, fan.Shape);
            Equal(SkillUpgradeKind.Range, fan.RollUpgrade(() => 0));
            Equal(SkillUpgradeKind.Angle, fan.RollUpgrade(() => 0.5));
            Equal(fan.Angle * LightSkillCast.UpgradeStep, fan.WithUpgrade(SkillUpgradeKind.Angle).Angle);
            Equal(fan.Hits, fan.WithUpgrade(SkillUpgradeKind.Hits).Hits);
            Equal(fan.Angle, fan.WithUpgrade(SkillUpgradeKind.Hits).Angle);
            Equal(true, fan.DescribeNext(3, SkillUpgradeKind.Angle).Contains("각도"));

            var rapid = Presented(10013);
            Equal(LightSkillShape.Projectile, rapid.Shape);
            Equal(SkillUpgradeKind.Projectiles, rapid.RollUpgrade(() => 0));
            Equal(rapid.Projectiles + 1, rapid.WithUpgrade(SkillUpgradeKind.Projectiles).Projectiles);

            var withStats = twoShots.WithStatUpgrades(2, 1.5f);
            Equal(bullet.Projectiles + 4, withStats.Projectiles);
            Equal(bullet.Range, withStats.Range);
            var areaStats = wider.WithUpgrade(SkillUpgradeKind.Hits).WithStatUpgrades(2, 1.2f);
            Equal(smash.Range * LightSkillCast.UpgradeStep * 1.2f, areaStats.Range);
            Equal(smash.Hits + 1, areaStats.Hits);

            var loadout = new PlayerLoadout();
            var owned = new Data.SkillData { DataId = 10001, Name = "불꽃 탄환" };
            Equal(true, loadout.TryAddOrUpgradeAttackSkill(owned, SkillUpgradeKind.Projectiles));
            Equal(true, loadout.TryAddOrUpgradeAttackSkill(owned, SkillUpgradeKind.Projectiles));
            Equal(true, loadout.TryAddOrUpgradeAttackSkill(owned));
            Equal(0, loadout.GetAttackSkillUpgrades(10001).Length);
            Equal(true, loadout.TryAddOrUpgradeAttackSkill(owned, SkillUpgradeKind.Range));
            Equal(true, loadout.TryAddOrUpgradeAttackSkill(owned, SkillUpgradeKind.Projectiles));
            var stored = loadout.GetAttackSkillUpgrades(10001);
            Equal(2, stored.Length);
            Equal(SkillUpgradeKind.Range, stored[0]);
            Equal(SkillUpgradeKind.Projectiles, stored[1]);
            var applied = bullet.WithUpgrades(stored);
            Equal(bullet.Range * LightSkillCast.UpgradeStep, applied.Range);
            Equal(bullet.Projectiles + 1, applied.Projectiles);
            Equal(true, loadout.RemoveSkill(10001));
            Equal(0, loadout.GetAttackSkillUpgrades(10001).Length);
        });
        Test("level-up offers supported heavy attacks without automatically equipping them", () =>
        {
            var loadout = new PlayerLoadout();
            var choices = LevelUpSkillReward.DrawChoices(shipped, loadout, 46, () => 0);
            Equal(true, choices.Any(skill => skill.Id == 20001));
            Equal(false, choices.Any(skill => skill.Id == 20002 || skill.Id == 20012));
            Equal(19, choices.Count(skill => skill.Id >= 20000));
            Equal(true, Acquire(loadout, shipped.skills.Single(skill => skill.Id == 20001)));
            Equal(1, loadout.GetAttackSkillStack(20001));
            Equal(0, loadout.GetEquippedSkillId(PlayerSkillButton.Heavy));
            Equal(true, loadout.TryEquip(PlayerSkillButton.Heavy, 20001));
            Equal(20001, loadout.GetEquippedSkillId(PlayerSkillButton.Heavy));
            Equal(false, Acquire(loadout, shipped.skills.Single(skill => skill.Id == 20002)));
            Equal(20001, loadout.GetEquippedSkillId(PlayerSkillButton.Heavy));
            var otherHeavy = shipped.skills.Single(skill => skill.Id == 20006);
            Equal(true, Acquire(loadout, otherHeavy));
            Equal(1, loadout.GetAttackSkillStack(20006));
            Equal(20001, loadout.GetEquippedSkillId(PlayerSkillButton.Heavy));
            Equal(true, Acquire(loadout, otherHeavy));
            Equal(2, loadout.GetAttackSkillStack(20006));
            Equal(20001, loadout.GetEquippedSkillId(PlayerSkillButton.Heavy));
        });
        Test("full slots offer owned heavy upgrades and exclude max stacks", () =>
        {
            var loadout = new PlayerLoadout();
            var hammer = shipped.skills.Single(skill => skill.Id == 20001);
            Equal(true, Acquire(loadout, hammer));
            for (int id = 10001; id <= 10005; id++) Equal(true, Acquire(loadout, Skill(id)));
            var choices = LevelUpSkillReward.DrawChoices(shipped, loadout, 46, () => 0);
            Equal(6, choices.Count);
            Equal(true, choices.Any(skill => skill.Id == 20001));
            Equal(false, choices.Any(skill => skill.Id == 20006));
            Equal(true, Acquire(loadout, hammer));
            Equal(true, Acquire(loadout, hammer));
            choices = LevelUpSkillReward.DrawChoices(shipped, loadout, 46, () => 0);
            Equal(true, choices.Any(skill => skill.Id == 20001));
            while (!loadout.IsAttackSkillAtMaxStack(hammer.Id))
                Equal(true, Acquire(loadout, hammer));
            Equal(PlayerLoadout.MaxAttackSkillStack, loadout.GetAttackSkillStack(hammer.Id));
            choices = LevelUpSkillReward.DrawChoices(shipped, loadout, 46, () => 0);
            Equal(false, choices.Any(skill => skill.Id == 20001));
        });
        Test("heavy charge uses the selected skill cost and duration once per release", () =>
        {
            var skill = shipped.skills.Single(value => value.Id == 20004);
            var player = new PlayerData();
            var charge = new PlayerHeavyCharge(player, () => true);
            var settings = new PlayerHeavyChargeSettings
            {
                Configured = true, MPCost = 10f,
                StageTimes = new[] { 0f, .4f, .8f }, StageBonuses = new[] { .1f, .1f, .5f }
            };
            var aim = new UnityEngine.Vector2(3f, 0f);
            Equal(true, charge.TryBegin(settings, aim, skill));
            Equal(4f, player.MP);
            Equal(true, charge.SuperArmor);
            int id = charge.CurrentId;
            Equal(false, charge.TryBegin(settings, aim, skill));
            charge.Tick(.375f, aim);
            Equal(2, charge.Stage);
            charge.Tick(.375f, aim);
            Equal(3, charge.Stage);
            Equal(true, charge.TryRelease(id, out HeavyChargeRelease result));
            Equal(.75f, result.Elapsed);
            Equal(1.7, Math.Round(result.Multiplier, 2));
            Equal(1f, result.Direction.x);
            Equal(false, charge.TryRelease(id, out _));
            Equal(false, charge.TryBegin(settings, aim, skill));
            Equal(4f, player.MP);
        });
        Test("acquisition refuses full slots and nonattack data", () =>
        {
            var loadout = new PlayerLoadout();
            for (int id = 10001; id <= 10006; id++) Equal(true, Acquire(loadout, Skill(id)));
            Equal(false, Acquire(loadout, Skill(10008)));
            Equal(10006, loadout.GetAttackSkill(5).DataId);
            // 슬롯이 가득 차도 미최대 스택 보유 스킬은 강화할 수 있습니다.
            Equal(true, Acquire(loadout, Skill(10001)));
            Equal(2, loadout.GetAttackSkillStack(10001));
        });
        Test("tactical acquisition registers and auto-equips once", () =>
        {
            var loadout = new PlayerLoadout();
            Equal(true, Acquire(loadout, Skill(30001)));
            Equal(30001, loadout.GetTacticalSkill(0).DataId);
            Equal(30001, loadout.GetEquippedSkillId(PlayerSkillButton.Tactical));
            Equal(false, Acquire(loadout, Skill(30001)));
            Equal(true, Acquire(loadout, Skill(30002)));
            Equal(30002, loadout.GetTacticalSkill(1).DataId);
            Equal(30001, loadout.GetEquippedSkillId(PlayerSkillButton.Tactical));
        });
        Test("owned exclusions leave only unowned candidates and can exhaust", () =>
        {
            var fixture = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>("""
                {"skills":[{"Id":10001,"Rarity":"Common"},{"Id":10002,"Rarity":"Rare"}],
                 "rarities":[{"Id":"Common","Weight":60},{"Id":"Rare","Weight":11}]}
                """);
            var owned = new System.Collections.Generic.HashSet<int> { 10001 };
            var result = DrawWithExclusions(fixture, owned);
            Equal(1, result.Count); Equal(10002, result[0].Id);
            owned.Add(10002); Equal(0, DrawWithExclusions(fixture, owned).Count);
        });
        Test("max-stack owned skills are excluded from the next draft", () =>
        {
            var loadout = new PlayerLoadout();
            var first = Draw(table, 3, 0)[0];
            Equal(true, Acquire(loadout, first));
            while (!loadout.IsAttackSkillAtMaxStack(first.Id))
                Equal(true, Acquire(loadout, first));
            var excluded = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < loadout.UnlockedAttackSlots; i++)
            {
                var owned = loadout.GetAttackSkill(i);
                if (owned != null && loadout.IsAttackSkillAtMaxStack(owned.DataId))
                    excluded.Add(owned.DataId);
            }
            var next = DrawWithExclusions(table, excluded);
            Equal(3, next.Count); Equal(false, next.Any(skill => skill.Id == first.Id));
            Equal(true, loadout.TryEquip(PlayerSkillButton.Light, first.Id));
            Equal(first.Id, loadout.GetEquippedSkillId(PlayerSkillButton.Light));
        });
        Test("rarity sheet preserves fractional weights and colors", () =>
        {
            var field = typeof(CombatSkillTableFile).GetField("rarities");
            if (field == null) throw new Exception("missing exported rarities");
            var rarities = (System.Collections.IList)field.GetValue(table);
            Equal(7, rarities.Count);
            var mythic = rarities[5];
            Equal("Mythic", mythic.GetType().GetField("Id").GetValue(mythic));
            Equal(.3f, mythic.GetType().GetField("Weight").GetValue(mythic));
            Equal("#FF5438", mythic.GetType().GetField("ColorHex").GetValue(mythic));
            Equal(0f, rarities[6].GetType().GetField("Weight").GetValue(rarities[6]));
        });
        Test("draft rolls rarity before uniform skill and respects boundaries", () =>
        {
            var fixture = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>("""
                {"skills":[{"Id":10001,"Rarity":"Common"},{"Id":10002,"Rarity":"Common"},{"Id":10003,"Rarity":"Rare"}],
                 "rarities":[{"Id":"Common","Weight":60},{"Id":"Rare","Weight":40}]}
                """);
            Equal(10002, Draw(fixture, 1, .599999, .999)[0].Id);
            Equal(10003, Draw(fixture, 1, .6, 0)[0].Id);
        });
        Test("draft excludes zero unknown and heavy and avoids duplicate IDs", () =>
        {
            var fixture = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>("""
                {"skills":[{"Id":10001,"Rarity":"Common"},{"Id":10001,"Rarity":"Common"},{"Id":10002,"Rarity":"Fusion"},
                 {"Id":10003,"Rarity":"Unknown"},{"Id":10004,"Rarity":""},{"Id":20001,"Rarity":"Common"}],
                 "rarities":[{"Id":"Common","Weight":60},{"Id":"Fusion","Weight":0}]}
                """);
            var result = Draw(fixture, 3, 0);
            Equal(1, result.Count); Equal(10001, result[0].Id);
        });
        Test("draft handles exhausted rarities and small candidate lists", () =>
        {
            var fixture = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>("""
                {"skills":[{"Id":10001,"Rarity":"Common"},{"Id":10002,"Rarity":"Mythic"}],
                 "rarities":[{"Id":"Common","Weight":60},{"Id":"Mythic","Weight":0.3}]}
                """);
            var result = Draw(fixture, 3, 0);
            Equal(2, result.Count); Equal(10001, result[0].Id); Equal(10002, result[1].Id);
            Equal(0, Draw(new CombatSkillTableFile(), 3, 0).Count);
        });
        Test("legacy Mystic resolves to Mythic and can be drafted", () =>
        {
            var fixture = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>("""
                {"skills":[{"Id":10006,"Rarity":"Mystic"}],"rarities":[{"Id":"Mythic","Weight":0.3}]}
                """);
            Equal(10006, Draw(fixture, 1, .999999)[0].Id);
        });
        Test("tooltip tokens use table values and preserve blank passive data", () =>
        {
            var type = typeof(CombatSkillTable).Assembly.GetType("LevelUpSkillDraft");
            string text = (string)type.GetMethod("FormatDescription").Invoke(null, new object[] { Skill(10001) });
            Equal("불꽃으로 이루어진 탄환 1발(8*1.2)을 적에게 쏜다.", text);
        });
        Test("passive description repeats the active action on the owned interval", () =>
        {
            var type = typeof(CombatSkillTable).Assembly.GetType("LevelUpSkillDraft");
            var format = type.GetMethod("FormatPassiveDescription");
            string flame = (string)format.Invoke(null, new object[] { Skill(10001) });
            Equal("불꽃 탄환 1발을 적에게 3초에 한 번 쏜다.", flame);
            Equal("불기둥을 {Passive_Cooldown}초에 한 번 세워 남아 있는 동안 들어온 적을 한 번 태운다.", Field(Skill(20009), "PassiveDescription"));
            Equal("불기둥을 7초에 한 번 세워 남아 있는 동안 들어온 적을 한 번 태운다.", format.Invoke(null, new object[] { Skill(20009) }));
            Equal("불꽃 탄환 3발을 4.5초에 한 번 연속으로 쏜다.", format.Invoke(null, new object[] { Skill(10002) }));
            foreach (var skill in table.skills)
            {
                string passive = (string)Field(skill, "PassiveDescription");
                bool attack = skill.Id >= 10000 && skill.Id < 30000;
                if (attack && (string.IsNullOrEmpty(passive) || !passive.Contains("{Passive_Cooldown}")))
                    throw new Exception(skill.Id + " 패시브 설명이 없습니다.");
                if (!attack && !string.IsNullOrEmpty(passive))
                    throw new Exception(skill.Id + " 전략 스킬에 패시브 설명이 있습니다.");
            }
        });
        Test("existing light attack values", () =>
        {
            Equal(76, table.skills.Count); var s = Skill(10001);
            Equal(5f, s.CostAmount); Equal(7f, s.Range); Equal(.5f, s.Width);
            Equal(8f, s.BaseValue); Equal(1.2f, s.Factor); Equal("Projectile", s.AreaType);
        });
        Test("utility workbook restores the runtime healing definition", () =>
        {
            var s = Skill(30001);
            Equal("재생 치유", s.Name); Equal("HealOverTime", Field(s, "EffectType")); Equal("Self", s.TargetType);
            Equal(5f, s.EffectDuration); Equal(5, s.Hits); Equal(.04f, s.Factor);
        });
        Test("heavy armor and knockback", () =>
        {
            Equal(false, Field(Skill(20001), "SuperArmor"));
            Equal(true, Field(Skill(20015), "SuperArmor"));
            Equal("Knockback", Field(Skill(20001), "CrowdControl"));
            Equal("None", Field(Skill(10001), "StatusEffectId"));
            Equal(0f, Field(Skill(10001), "EffectDuration"));
        });
        Test("light workbook keeps its icon path and blank cues", () =>
        {
            Equal("SkillIcons/SkillIcon_10001", Field(Skill(10001), "Icon"));
            foreach (string field in new[] { "AnimationName", "EffectPrefab", "SoundCue" })
                Equal("", Field(Skill(10001), field));
            Equal("Common", Field(Skill(10001), "Rarity"));
        });
        Test("passive cooldown follows rarity and the blank scale means 20 percent", () =>
        {
            string[] order = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            // 같은 계열·같은 등급에서 쿨타임이 가장 짧은 값이 기준이며, 등급이 오를수록 같거나 길어야 합니다.
            foreach (var group in new[] { (10000, 20000), (20000, 30000) })
            {
                float previous = 0f;
                foreach (string rarity in order)
                {
                    var cooldowns = table.skills.Where(s => s.Id >= group.Item1 && s.Id < group.Item2 && s.Rarity == rarity)
                        .Select(s => s.PassiveCooldown).ToList();
                    if (cooldowns.Count == 0) continue;
                    if (cooldowns.Any(c => !(c > 0f))) throw new Exception($"{group.Item1}대 {rarity}에 쿨타임이 빈 스킬이 있습니다.");
                    if (cooldowns.Min() < previous) throw new Exception($"{group.Item1}대 {rarity} 쿨타임이 낮은 등급보다 짧습니다.");
                    previous = cooldowns.Min();
                }
            }
            // 강공격은 같은 등급의 약공격보다 짧지 않고, 시너지 등급은 규칙이 없어 비어 있습니다.
            foreach (string rarity in order)
            {
                var light = table.skills.Where(s => s.Id < 20000 && s.Rarity == rarity).Select(s => s.PassiveCooldown).DefaultIfEmpty(0f).Min();
                var heavy = table.skills.Where(s => s.Id >= 20000 && s.Id < 30000 && s.Rarity == rarity).Select(s => s.PassiveCooldown).DefaultIfEmpty(float.MaxValue).Min();
                if (light > 0f && heavy != float.MaxValue && heavy <= light) throw new Exception($"{rarity} 강공격 쿨타임이 약공격보다 짧거나 같습니다.");
            }
            Equal(3f, Skill(10001).PassiveCooldown); Equal(7f, Skill(10017).PassiveCooldown);
            Equal(8f, Skill(20005).PassiveCooldown); Equal(2f, Skill(10007).PassiveCooldown);
            Equal(0f, Skill(10001).PassiveScale); Equal(0f, Field(Skill(30001), "PassiveCooldown"));
            Equal(true, Skill(10001).TryOwnedPassive(out float baseCooldown, out float baseScale));
            Equal(3f, baseCooldown); Equal(.2f, baseScale);
            Equal(true, Skill(10007).TryOwnedPassive(out _, out _));
            var ready = new CombatSkillRecord { Id = 10014, PassiveCooldown = 3f };
            Equal(true, ready.TryOwnedPassive(out float cooldown, out float scale));
            Equal(3f, cooldown); Equal(CombatSkillRecord.DefaultPassiveScale, scale);
            ready.PassiveScale = .5f; Equal(true, ready.TryOwnedPassive(out _, out scale)); Equal(.5f, scale);
            ready.Id = 30001; Equal(false, ready.TryOwnedPassive(out _, out _));
            ready.Id = 20001; ready.PassiveCooldown = 0f; Equal(false, ready.TryOwnedPassive(out _, out _));
        });
        Test("water skills use shots, bursts, fans and rings as well as lines", () =>
        {
            var water = table.skills.Where(s => (s.Id >= 10022 && s.Id <= 10036) || (s.Id >= 20016 && s.Id <= 20030)).ToList();
            Equal(30, water.Count);
            foreach (string shape in new[] { "Projectile", "Circle", "Line", "Cone", "Arc", "Ring" })
                if (!water.Any(s => s.AreaType == shape)) throw new Exception("물 스킬에 없는 모양: " + shape);
            int lines = water.Count(s => s.AreaType == "Line");
            if (lines >= 15) throw new Exception("물 스킬 관통이 절반 이상입니다: " + lines);
            Equal("Line", Skill(10022).AreaType);
            Equal("Circle", Skill(10023).AreaType);
            Equal("Projectile", Skill(10025).AreaType);
            Equal(3, Skill(10026).Projectiles);
            Equal("Ring", Skill(10032).AreaType);
            Equal("Arc", Skill(10035).AreaType);
            Equal("Cone", Skill(20018).AreaType);
            Equal("Circle", Skill(20030).AreaType);
        });
        Test("slashes reach farther and the flame pillar hits once", () =>
        {
            Equal(5.5f, Skill(10007).Range); Equal(5.5f, (float)Field(Skill(10007), "Radius")); Equal(140f, Skill(10007).Angle);
            Equal(5f, Skill(10011).Range); Equal(5f, (float)Field(Skill(10011), "Radius")); Equal(80f, Skill(10011).Angle);
            Equal(5.2f, Skill(10028).Range); Equal(0f, (float)Field(Skill(10028), "Radius"));
            Equal(5.5f, Skill(10035).Range);
            Equal(4.5f, Skill(20006).Range); Equal(4.5f, (float)Field(Skill(20006), "Radius"));
            Equal(5f, Skill(20010).Range); Equal(80f, Skill(20010).Angle);
            Equal(5.2f, Skill(20011).Range);
            Equal(5.2f, Skill(20017).Range);
            Equal(5.5f, Skill(20018).Range);
            Equal(5.5f, Skill(20025).Range);
            Equal(6f, Skill(20029).Range);
            Equal(3.5f, Skill(10013).Range);
            var pillar = Skill(20009);
            Equal(1, pillar.Hits); Equal(4f, pillar.Range); Equal(4f, (float)Field(pillar, "Radius"));
            Equal(7f, pillar.PassiveCooldown); Equal("Circle", pillar.AreaType); Equal(0.18f, pillar.HitDelay);
            Equal("불기둥을 세워 남아 있는 동안 들어온 적을 한 번 태운다.", Field(pillar, "Description"));
            Equal("대상 지점에 불기둥({Base_Value}*{Scaling_Factor})을 세워 들어온 적을 한 번 태운다.", Field(pillar, "SimpleDescription"));
        });
        Test("area radius and length remain independent", () =>
        {
            Equal(2.2f, Field(Skill(20001), "Radius"));
            Equal(7f, Field(Skill(10001), "Length"));
            Equal(0f, Field(Skill(10001), "Radius"));
        });
        Test("inline strings and absolute relationship targets", () =>
        {
            string path = Path.GetFullPath(".utmp/CombatSkillData/inline.xlsx");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(workbookPath, path, true);
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                Rewrite(zip, "xl/worksheets/sheet1.xml", doc =>
                {
                    var cell = doc.Descendants(main + "c").Single(c => (string)c.Attribute("r") == "D3");
                    cell.SetAttributeValue("t", "inlineStr");
                    cell.ReplaceNodes(new XElement(main + "is", new XElement(main + "t", "inline skill")));
                });
                Rewrite(zip, "xl/_rels/workbook.xml.rels", doc =>
                {
                    foreach (var rel in doc.Root.Elements())
                    {
                        string target = (string)rel.Attribute("Target");
                        if (!target.StartsWith("/")) rel.SetAttributeValue("Target", "/xl/" + target);
                    }
                });
            }
            Equal("inline skill", CombatSkillWorkbook.Read(path).skills.Single(s => s.Id == 10001).Name);
        });
        Test("fusion is the active at max level plus that skill's passive card", () =>
        {
            var field = typeof(CombatSkillTableFile).GetField("fusions");
            if (field == null) throw new Exception("missing exported fusions");
            var fusions = (System.Collections.IList)field.GetValue(table);
            Equal(1, fusions.Count);
            object fusion = fusions[0];
            object Value(string name) => fusion.GetType().GetField(name).GetValue(fusion);
            Equal(10007, Value("Id"));
            Equal("태양 검", Value("Name"));
            Equal(10003, Value("ActiveSkillId"));
            Equal("화염 강타", Value("ActiveSkillName"));
            Equal(10003, Value("PassiveCardId"));
            Equal("화염 강타 패시브", Value("PassiveCardName"));
            Equal("MaxLevel", Value("ActiveCondition"));
            Equal("Acquire", Value("PassiveCondition"));
            var shipped = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>(
                File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
            var shippedFusions = (System.Collections.IList)field.GetValue(shipped);
            Equal(1, shippedFusions.Count);
            object shippedFusion = shippedFusions[0];
            foreach (var fusionField in fusion.GetType().GetFields())
            {
                if (!Equals(fusionField.GetValue(fusion), fusionField.GetValue(shippedFusion)))
                    throw new Exception($"fusion {fusionField.Name}: workbook={fusionField.GetValue(fusion)}, JSON={fusionField.GetValue(shippedFusion)}");
            }
        });
        Test("shipped JSON keeps combat values and blank passive columns", () =>
        {
            var shipped = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>(
                File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
            CombatSkillRecord Find(int id) => shipped.skills.Single(s => s.Id == id);
            Equal(76, shipped.skills.Count);
            var flame = Find(10001);
            Equal(8f, flame.BaseValue); Equal("", flame.Icon); Equal(3f, flame.PassiveCooldown); Equal(0f, flame.PassiveScale);
            var hammer = Find(20001);
            Equal("Knockback", hammer.CrowdControl); Equal(2.2f, hammer.Radius); Equal(4f, hammer.PassiveCooldown);
            // 엑셀과 배포 JSON의 패시브 쿨타임이 서로 다르면 어느 한쪽이 덜 반영된 것입니다.
            foreach (var s in shipped.skills)
                Equal(Skill(s.Id).PassiveCooldown, s.PassiveCooldown);
            var heal = Find(30001);
            Equal("HealOverTime", heal.EffectType); Equal("Self", heal.TargetType); Equal(5f, heal.EffectDuration);
            Equal(5, heal.Hits); Equal(1f, heal.HitDelay); Equal(.04f, heal.Factor); Equal("HP", heal.ScalingStat);
            Equal("5초 동안 1초마다 최대 체력의 4%를 회복한다.", heal.Description);
        });
        Console.WriteLine($"Result: {tests - failures}/{tests} passed");
        if (failures == 0 && args.Contains("--export")) CombatSkillTableExporter.Export();
        return failures == 0 ? 0 : 1;
    }
    private static void Rewrite(ZipArchive zip, string path, Action<XDocument> change)
    {
        var entry = zip.GetEntry(path); XDocument doc;
        using (var stream = entry.Open()) doc = XDocument.Load(stream);
        change(doc); entry.Delete();
        using (var stream = zip.CreateEntry(path).Open()) doc.Save(stream);
    }
    private static System.Collections.Generic.List<CombatSkillRecord> Draw(CombatSkillTableFile source, int count, params double[] rolls)
    {
        var type = typeof(CombatSkillTable).Assembly.GetType("LevelUpSkillDraft");
        if (type == null) throw new Exception("LevelUpSkillDraft is missing");
        int next = 0;
        Func<double> random = () => rolls[next++ % rolls.Length];
        var method = type.GetMethod("Draw");
        object[] arguments = method.GetParameters().Length == 5
            ? new object[] { source, count, 10000, 20000, random }
            : new object[] { source, count, 10000, 20000, random, null };
        return (System.Collections.Generic.List<CombatSkillRecord>)method.Invoke(null, arguments);
    }
    private static bool Acquire(PlayerLoadout loadout, CombatSkillRecord record)
    {
        var type = typeof(CombatSkillTable).Assembly.GetType("LevelUpSkillReward");
        if (type == null) throw new Exception("LevelUpSkillReward is missing");
        var method = type.GetMethod("TryAcquire");
        if (method == null) throw new Exception("TryAcquire is missing");
        object[] args = method.GetParameters().Length >= 3
            ? new object[] { loadout, record, SkillUpgradeKind.None }
            : new object[] { loadout, record };
        return (bool)method.Invoke(null, args);
    }
    private static System.Collections.Generic.List<CombatSkillRecord> DrawWithExclusions(CombatSkillTableFile source, System.Collections.Generic.ISet<int> excluded)
    {
        var method = typeof(CombatSkillTable).Assembly.GetType("LevelUpSkillDraft").GetMethod("Draw");
        if (method.GetParameters().Length != 6) throw new Exception("owned exclusion parameter is missing");
        return (System.Collections.Generic.List<CombatSkillRecord>)method.Invoke(null,
            new object[] { source, 3, 10000, 20000, (Func<double>)(() => 0), excluded });
    }
}
