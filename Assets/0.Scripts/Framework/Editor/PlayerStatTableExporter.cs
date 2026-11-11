#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using static CombatSkillWorkbook;

public static class PlayerStatTableExporter
{
    [MenuItem("Tools/Parse Player Stat Table")]
    public static void Export()
    {
        string workbook = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Document/PlayerDatatable.xlsx");
        PlayerStatTable table = Read(workbook);
        string path = Path.Combine(Application.dataPath, "Resources/Data/PlayerStatTable.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(table, true));
        AssetDatabase.Refresh();
        Debug.Log($"스탯 카드 {table.Cards.Length}개를 저장했습니다.");
    }

    public static PlayerStatTable Read(string workbook)
    {
        using var stream = new FileStream(workbook, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var sheets = ReadSheetPaths(zip);
        var strings = ReadSharedStrings(zip);
        var stats = ReadCells(zip, sheets["Player_Stats"], strings);
        var cardCells = ReadCells(zip, sheets["Player_Stats_Card"], strings);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int row = 3; row < 1000; row++)
        {
            string key = Cell(stats, "B", row);
            if (key.Length > 0) keys.Add(key);
        }

        var cards = new List<PlayerStatCard>();
        for (int row = 3; row < 1000; row++)
        {
            string key = Cell(cardCells, "B", row);
            if (key.Length == 0) continue;
            if (!keys.Remove(key) || !Enum.TryParse(key, out PlayerStat stat) || stat.ToString() != key)
                throw new InvalidDataException($"Player_Stats_Card!B{row}: 알 수 없거나 중복된 스탯 {key}");
            string name = Cell(cardCells, "C", row);
            var values = new List<float>(6);
            bool ended = false;
            for (char column = 'D'; column <= 'I'; column++)
            {
                string raw = Cell(cardCells, column.ToString(), row);
                if (raw.Length == 0 || raw == "-") { ended = true; continue; }
                if (ended) throw new InvalidDataException($"Player_Stats_Card!{column}{row}: 중간 레벨 값이 없습니다.");
                values.Add(Parse(raw, $"{column}{row}"));
            }
            if (values.Count < 2) throw new InvalidDataException($"Player_Stats_Card!D{row}: 레벨 값이 부족합니다.");
            string limit = Cell(cardCells, "J", row);
            cards.Add(new PlayerStatCard
            {
                Stat = stat, Name = name, Values = values.ToArray(),
                Limit = limit.Length == 0 ? 0f : Parse(limit, $"J{row}"),
                Weight = Parse(Cell(cardCells, "K", row), $"K{row}")
            });
        }
        return new PlayerStatTable { Cards = cards.ToArray() };
    }

    private static float Parse(string raw, string cell)
    {
        if (raw.EndsWith("/s", StringComparison.Ordinal)) raw = raw.Substring(0, raw.Length - 2);
        bool percent = raw.EndsWith("%", StringComparison.Ordinal);
        if (percent) raw = raw.Substring(0, raw.Length - 1);
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            || !(value >= 0f) || float.IsInfinity(value))
            throw new InvalidDataException($"Player_Stats_Card!{cell}: 잘못된 수치 {raw}");
        return percent ? value / 100f : value;
    }
}
#endif
