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
        table = CombatSkillWorkbook.Read("Document/SkillDatatable.xlsx");
        Test("acquired skill is owned once and not automatically equipped", () =>
        {
            var loadout = new PlayerLoadout(); int changes = 0; loadout.Changed += () => changes++;
            Equal(true, Acquire(loadout, Skill(10002)));
            Equal(10002, loadout.GetAttackSkill(0).DataId);
            Equal(Skill(10002).Name, loadout.GetAttackSkill(0).Name);
            Equal(Skill(10002).Icon, loadout.GetAttackSkill(0).IconLabel);
            Equal(0, loadout.GetEquippedSkillId(PlayerSkillButton.Light));
            Equal(false, Acquire(loadout, Skill(10002))); Equal(1, changes);
        });
        Test("acquisition refuses full slots and nonattack data", () =>
        {
            var loadout = new PlayerLoadout();
            for (int id = 10001; id <= 10006; id++) Equal(true, Acquire(loadout, Skill(id)));
            Equal(false, Acquire(loadout, Skill(10008)));
            Equal(false, Acquire(new PlayerLoadout(), Skill(30001)));
            Equal(10006, loadout.GetAttackSkill(5).DataId);
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
        Test("acquisition removes the selected record from the next draft", () =>
        {
            var loadout = new PlayerLoadout();
            var first = Draw(table, 3, 0)[0]; Equal(true, Acquire(loadout, first));
            var owned = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < loadout.UnlockedAttackSlots; i++)
                if (loadout.GetAttackSkill(i) != null) owned.Add(loadout.GetAttackSkill(i).DataId);
            var next = DrawWithExclusions(table, owned);
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
        Test("existing light attack values", () =>
        {
            Equal(46, table.skills.Count); var s = Skill(10001);
            Equal(5f, s.CostAmount); Equal(7f, s.Range); Equal(.5f, s.Width);
            Equal(8f, s.BaseValue); Equal(1.2f, s.Factor); Equal("Projectile", s.AreaType);
        });
        Test("utility workbook row keeps its name while effect cells stay blank", () =>
        {
            var s = Skill(30001);
            Equal("재생 치유", s.Name); Equal("", Field(s, "EffectType")); Equal("", s.TargetType);
        });
        Test("heavy armor and knockback", () =>
        {
            Equal(false, Field(Skill(20001), "SuperArmor"));
            Equal(true, Field(Skill(20015), "SuperArmor"));
            Equal("", Field(Skill(20001), "CrowdControl"));
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
        Test("owned passive columns stay blank and scale defaults to 20 percent", () =>
        {
            Equal(0f, Skill(10001).PassiveCooldown); Equal(0f, Skill(10001).PassiveScale);
            Equal(0f, Skill(20001).PassiveCooldown); Equal(0f, Field(Skill(30001), "PassiveCooldown"));
            Equal(false, Skill(10001).TryOwnedPassive(out _, out _));
            var ready = new CombatSkillRecord { Id = 10014, PassiveCooldown = 3f };
            Equal(true, ready.TryOwnedPassive(out float cooldown, out float scale));
            Equal(3f, cooldown); Equal(CombatSkillRecord.DefaultPassiveScale, scale);
            ready.PassiveScale = .5f; Equal(true, ready.TryOwnedPassive(out _, out scale)); Equal(.5f, scale);
            ready.Id = 30001; Equal(false, ready.TryOwnedPassive(out _, out _));
            ready.Id = 20001; ready.PassiveCooldown = 0f; Equal(false, ready.TryOwnedPassive(out _, out _));
        });
        Test("area radius and length remain independent", () =>
        {
            Equal(0f, Field(Skill(20001), "Radius"));
            Equal(7f, Field(Skill(10001), "Length"));
            Equal(0f, Field(Skill(10001), "Radius"));
        });
        Test("inline strings and absolute relationship targets", () =>
        {
            string path = Path.GetFullPath(".utmp/CombatSkillData/inline.xlsx");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy("Document/SkillDatatable.xlsx", path, true);
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
        Test("shipped JSON keeps combat values and blank passive columns", () =>
        {
            var shipped = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>(
                File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
            CombatSkillRecord Find(int id) => shipped.skills.Single(s => s.Id == id);
            Equal(46, shipped.skills.Count);
            var flame = Find(10001);
            Equal(8f, flame.BaseValue); Equal("", flame.Icon); Equal(0f, flame.PassiveCooldown); Equal(0f, flame.PassiveScale);
            var hammer = Find(20001);
            Equal("Knockback", hammer.CrowdControl); Equal(2.2f, hammer.Radius); Equal(0f, hammer.PassiveCooldown);
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
        return (bool)type.GetMethod("TryAcquire").Invoke(null, new object[] { loadout, record });
    }
    private static System.Collections.Generic.List<CombatSkillRecord> DrawWithExclusions(CombatSkillTableFile source, System.Collections.Generic.ISet<int> excluded)
    {
        var method = typeof(CombatSkillTable).Assembly.GetType("LevelUpSkillDraft").GetMethod("Draw");
        if (method.GetParameters().Length != 6) throw new Exception("owned exclusion parameter is missing");
        return (System.Collections.Generic.List<CombatSkillRecord>)method.Invoke(null,
            new object[] { source, 3, 10000, 20000, (Func<double>)(() => 0), excluded });
    }
}
