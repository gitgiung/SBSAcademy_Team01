#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

public static class CombatSkillTableExporter
{
    private const string WorkbookPath = "SkillDatatable.xlsx";

    [MenuItem("Tools/Parse Combat Skill Table")]
    public static void Export()
    {
        string workbook = Path.Combine(Directory.GetParent(Application.dataPath).FullName, WorkbookPath);
        if (!File.Exists(workbook))
        {
            Debug.LogError($"전투 수치 엑셀이 없습니다: {workbook}");
            return;
        }

        CombatSkillTableFile file = CombatSkillWorkbook.Read(workbook);
        string directory = Path.Combine(Application.dataPath, "Resources", "Data");
        Directory.CreateDirectory(directory);
        string jsonPath = Path.Combine(directory, "CombatSkillTable.json");
        File.WriteAllText(jsonPath, JsonUtility.ToJson(file, true), new UTF8Encoding(false));
        AssetDatabase.Refresh();
        Debug.Log($"전투 스킬 {file.skills.Count}개를 저장했습니다: {jsonPath}");
    }
}

/// <summary>SkillDatatable.xlsx의 약·강·전략 시트와 범위 시트를 ID로 합칩니다.</summary>
public static class CombatSkillWorkbook
{
    public static CombatSkillTableFile Read(string path)
    {
        using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Read);
        List<string> shared = ReadSharedStrings(zip);
        Dictionary<string, string> sheets = ReadSheetPaths(zip);
        Dictionary<int, CombatSkillRecord> records = new Dictionary<int, CombatSkillRecord>();
        ReadSkills(zip, sheets, "LightSkilltable", shared, records);
        ReadSkills(zip, sheets, "HeavySkilltable", shared, records);
        ReadSkills(zip, sheets, "UtilitySkilltable", shared, records);
        ApplyAreas(zip, sheets, shared, records);

        CombatSkillTableFile file = new CombatSkillTableFile();
        List<int> ids = new List<int>(records.Keys);
        ids.Sort();
        foreach (int id in ids) file.skills.Add(records[id]);
        return file;
    }

    private static void ReadSkills(ZipArchive zip, Dictionary<string, string> sheets, string sheetName,
        List<string> shared, Dictionary<int, CombatSkillRecord> records)
    {
        if (!sheets.TryGetValue(sheetName, out string sheetPath))
            throw new InvalidOperationException($"{sheetName} 시트가 없습니다.");
        Dictionary<string, string> cells = ReadCells(zip, sheetPath, shared);
        for (int row = 3; row < 1000; row++)
        {
            if (!int.TryParse(Cell(cells, "C", row), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                continue;
            string name = Cell(cells, "D", row);
            if (string.IsNullOrEmpty(name)) continue;
            records[id] = new CombatSkillRecord
            {
                Id = id,
                Name = name,
                Element = Cell(cells, "H", row),
                CostType = Cell(cells, "I", row),
                CostAmount = Number(cells, "J", row),
                Cooldown = Number(cells, "K", row),
                CastType = Cell(cells, "L", row),
                CastTime = Number(cells, "M", row),
                AreaType = NormalizeArea(Cell(cells, "P", row)),
                Range = Number(cells, "O", row),
                Projectiles = Count(cells, "Q", row, 1),
                Hits = Count(cells, "R", row, 1),
                HitDelay = Number(cells, "S", row),
                BaseValue = Number(cells, "X", row),
                ScalingStat = Cell(cells, "Y", row),
                Factor = Number(cells, "Z", row)
            };
        }
    }

    private static void ApplyAreas(ZipArchive zip, Dictionary<string, string> sheets, List<string> shared,
        Dictionary<int, CombatSkillRecord> records)
    {
        if (!sheets.TryGetValue("Area_Typetable", out string sheetPath)) return;
        Dictionary<string, string> cells = ReadCells(zip, sheetPath, shared);
        for (int row = 3; row < 1000; row++)
        {
            if (!int.TryParse(Cell(cells, "C", row), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                continue;
            if (!records.TryGetValue(id, out CombatSkillRecord skill)) continue;
            string areaType = NormalizeArea(Cell(cells, "E", row));
            if (!string.IsNullOrEmpty(areaType)) skill.AreaType = areaType;
            float radius = Number(cells, "F", row);
            float angle = Number(cells, "G", row);
            float width = Number(cells, "H", row);
            float length = Number(cells, "I", row);
            skill.Angle = angle;
            skill.Width = width;
            if (skill.Range <= 0f)
            {
                if (skill.AreaType == "Projectile" || skill.AreaType == "Line") skill.Range = length;
                else if (radius > 0f) skill.Range = radius;
            }
        }
    }

    public static string NormalizeArea(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value == "Projecttile" || value == "Projectile") return "Projectile";
        return value;
    }

    private static int Count(Dictionary<string, string> cells, string column, int row, int fallback)
    {
        float value = Number(cells, column, row);
        return value > 0f ? Mathf.RoundToInt(value) : fallback;
    }

    private static float Number(Dictionary<string, string> cells, string column, int row)
    {
        string text = Cell(cells, column, row);
        if (string.IsNullOrEmpty(text) || text == "Reach" || text == "None") return 0f;
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
    }

    private static string Cell(Dictionary<string, string> cells, string column, int row)
    {
        return cells.TryGetValue(column + row, out string value) ? value : "";
    }

    private static Dictionary<string, string> ReadSheetPaths(ZipArchive zip)
    {
        XmlDocument workbook = LoadXml(zip, "xl/workbook.xml");
        XmlDocument rels = LoadXml(zip, "xl/_rels/workbook.xml.rels");
        XmlNamespaceManager workbookNs = Namespace(workbook, "m", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        XmlNamespaceManager relNs = Namespace(rels, "r", "http://schemas.openxmlformats.org/package/2006/relationships");
        var relationshipTargets = new Dictionary<string, string>();
        foreach (XmlNode node in rels.SelectNodes("//r:Relationship", relNs))
        {
            string id = node.Attributes?["Id"]?.Value;
            string target = node.Attributes?["Target"]?.Value;
            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(target))
                relationshipTargets[id] = "xl/" + target.TrimStart('/');
        }

        var sheets = new Dictionary<string, string>();
        foreach (XmlNode node in workbook.SelectNodes("//m:sheet", workbookNs))
        {
            string name = node.Attributes?["name"]?.Value;
            string id = node.Attributes?["id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"]?.Value;
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id)) continue;
            if (relationshipTargets.TryGetValue(id, out string target)) sheets[name] = target;
        }
        return sheets;
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var strings = new List<string>();
        ZipArchiveEntry entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return strings;
        XmlDocument document = LoadXml(entry);
        XmlNamespaceManager ns = Namespace(document, "m", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        foreach (XmlNode node in document.SelectNodes("//m:si", ns))
        {
            string value = "";
            foreach (XmlNode text in node.SelectNodes(".//m:t", ns)) value += text.InnerText;
            strings.Add(value);
        }
        return strings;
    }

    private static Dictionary<string, string> ReadCells(ZipArchive zip, string sheetPath, List<string> shared)
    {
        XmlDocument document = LoadXml(zip, sheetPath);
        XmlNamespaceManager ns = Namespace(document, "m", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        var cells = new Dictionary<string, string>();
        foreach (XmlNode node in document.SelectNodes("//m:c", ns))
        {
            string reference = node.Attributes?["r"]?.Value;
            if (string.IsNullOrEmpty(reference)) continue;
            XmlNode value = node.SelectSingleNode("m:v", ns);
            if (value == null) continue;
            string text = value.InnerText;
            if (node.Attributes?["t"]?.Value == "s"
                && int.TryParse(text, out int index) && index >= 0 && index < shared.Count)
                text = shared[index];
            cells[reference] = text;
        }
        return cells;
    }

    private static XmlDocument LoadXml(ZipArchive zip, string path)
    {
        ZipArchiveEntry entry = zip.GetEntry(path.Replace('\\', '/'));
        if (entry == null) throw new FileNotFoundException(path);
        return LoadXml(entry);
    }

    private static XmlDocument LoadXml(ZipArchiveEntry entry)
    {
        using Stream stream = entry.Open();
        XmlDocument document = new XmlDocument();
        document.Load(stream);
        return document;
    }

    private static XmlNamespaceManager Namespace(XmlDocument document, string prefix, string uri)
    {
        XmlNamespaceManager ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace(prefix, uri);
        return ns;
    }
}
#endif
