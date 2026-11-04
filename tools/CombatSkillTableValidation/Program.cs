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
        table = CombatSkillWorkbook.Read("SkillDatatable.xlsx");
        Test("existing light attack values", () =>
        {
            Equal(46, table.skills.Count); var s = Skill(10001);
            Equal(5f, s.CostAmount); Equal(7f, s.Range); Equal(.5f, s.Width);
            Equal(8f, s.BaseValue); Equal(1.2f, s.Factor); Equal("Projectile", s.AreaType);
        });
        Test("regeneration preserves self target and duration", () =>
        {
            var s = Skill(30001);
            Equal("HealOverTime", Field(s, "EffectType")); Equal("Self", Field(s, "TargetType"));
            Equal(5f, Field(s, "EffectDuration")); Equal(5, s.Hits); Equal(1f, s.HitDelay);
            Equal(.04f, s.Factor); Equal("HP", s.ScalingStat);
        });
        Test("heavy armor and knockback", () =>
        {
            Equal(false, Field(Skill(20001), "SuperArmor"));
            Equal(true, Field(Skill(20015), "SuperArmor"));
            Equal("Knockback", Field(Skill(20001), "CrowdControl"));
            Equal("None", Field(Skill(10001), "StatusEffectId"));
            Equal(0f, Field(Skill(10001), "EffectDuration"));
        });
        Test("empty resources remain empty", () =>
        {
            foreach (string field in new[] { "Icon", "AnimationName", "EffectPrefab", "SoundCue" })
                Equal("", Field(Skill(10001), field));
            Equal("Common", Field(Skill(10001), "Rarity"));
            Equal("5초 동안 1초마다 최대 체력의 4%를 회복한다.", Field(Skill(30001), "Description"));
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
            File.Copy("SkillDatatable.xlsx", path, true);
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
        Test("existing JSON fields remain unchanged for all skills", () =>
        {
            var previous = UnityEngine.JsonUtility.FromJson<CombatSkillTableFile>(
                File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
            Equal(previous.skills.Count, table.skills.Count);
            string[] fields = { "Id", "Name", "Element", "CostType", "CostAmount", "Cooldown", "CastType", "CastTime",
                "AreaType", "Range", "Angle", "Width", "Projectiles", "Hits", "HitDelay", "BaseValue", "ScalingStat", "Factor" };
            foreach (var s in previous.skills)
                foreach (string field in fields) Equal(Field(s, field), Field(Skill(s.Id), field));
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
}
