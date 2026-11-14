#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Play 중 CombatUI 테스트 메뉴에서 지정한 스킬만 레벨업 카드로 엽니다.</summary>
public sealed class CombatSkillCardPicker : EditorWindow
{
    private CombatPopupController target;
    private string filter = "";
    private Vector2 scroll;
    private readonly List<CombatSkillRecord> skills = new List<CombatSkillRecord>();

    public static void Open(CombatPopupController controller)
    {
        var window = GetWindow<CombatSkillCardPicker>(true, "원하는 카드", true);
        window.target = controller;
        window.minSize = new Vector2(420f, 480f);
        window.Reload();
        window.Show();
    }

    private void OnEnable() { Reload(); }

    private void Reload()
    {
        skills.Clear();
        CombatSkillTableFile table = CombatSkillTable.CreateSnapshot();
        if (table?.skills == null) return;
        foreach (CombatSkillRecord skill in table.skills)
            if (skill != null && skill.Id > 0) skills.Add(skill);
        skills.Sort((left, right) => left.Id.CompareTo(right.Id));
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "스킬·스탯을 누르면 해당 카드만 열립니다. 스탯은 다음 단계로 증가합니다. 삭제는 스킬의 보유·장착 또는 스탯의 모든 단계를 제거합니다.",
            MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            filter = EditorGUILayout.TextField("검색", filter);
            if (GUILayout.Button("새로고침", GUILayout.Width(72f))) Reload();
        }

        if (target == null && EditorApplication.isPlaying)
            target = FindActiveController();
        if (!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Play 중에만 카드를 열 수 있습니다.", MessageType.Warning);
        else if (target == null)
            EditorGUILayout.HelpBox("Hierarchy의 CombatUI를 선택하고 컴포넌트 메뉴에서 테스트/원하는 카드 고르기를 다시 실행하세요.", MessageType.Warning);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        string current = null;
        string query = (filter ?? "").Trim();
        PlayerData playerData = target != null && EditorApplication.isPlaying ? target.CardTestPlayerData : null;
        EditorGUILayout.LabelField("스탯 증가 카드", EditorStyles.boldLabel);
        foreach (var card in PlayerStatTable.LoadCards())
        {
            if (query.Length > 0 && (card.Name == null || card.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                && card.Stat.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
            int level = playerData != null ? playerData.StatCards.Level(card.Stat) : 0;
            bool canUpgrade = card.Values != null && level + 1 < card.Values.Length
                && card.ValueAt(level + 1) > card.ValueAt(level);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(playerData == null || !canUpgrade))
                    if (GUILayout.Button($"{card.Name}  Lv{level} → " + (canUpgrade ? $"Lv{level + 1} ({card.FormatValue(level + 1)})" : "최대")))
                        target.ShowChosenStatCard(card.Stat);
                using (new EditorGUI.DisabledScope(playerData == null || level == 0))
                    if (GUILayout.Button("전체 단계 삭제", GUILayout.Width(100f))) target.RemoveTestStatCard(card.Stat);
            }
        }
        foreach (CombatSkillRecord skill in skills)
        {
            if (!Matches(skill, query)) continue;
            string category = Category(skill.Id);
            if (category != current)
            {
                current = category;
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField(category, EditorStyles.boldLabel);
            }
            string rarity = string.IsNullOrEmpty(skill.Rarity) ? "등급 없음" : skill.Rarity;
            string note = skill.Id >= 30000 && skill.Id < 40000 && !TacticalSkillEffect.IsSupported(skill.EffectType)
                ? "  (시전 미연결)" : "";
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(playerData == null))
                    if (GUILayout.Button($"{skill.Id}  {skill.Name}  ·  {rarity}{note}"))
                        target.ShowChosenSkillCard(skill.Id);
                bool owned = false;
                if (playerData != null)
                {
                    for (int i = 0; i < playerData.Loadout.UnlockedAttackSlots; i++)
                        if (playerData.Loadout.GetAttackSkill(i)?.DataId == skill.Id) owned = true;
                    for (int i = 0; i < PlayerLoadout.TacticalCapacity; i++)
                        if (playerData.Loadout.GetTacticalSkill(i)?.DataId == skill.Id) owned = true;
                }
                using (new EditorGUI.DisabledScope(!owned))
                    if (GUILayout.Button("삭제", GUILayout.Width(52f))) target.RemoveTestSkillCard(skill.Id);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static CombatPopupController FindActiveController()
    {
        foreach (CombatPopupController candidate in FindObjectsByType<CombatPopupController>(FindObjectsInactive.Exclude))
            if (candidate != null && candidate.isActiveAndEnabled) return candidate;
        return null;
    }

    private static bool Matches(CombatSkillRecord skill, string query)
    {
        if (query.Length == 0) return true;
        if (skill.Id.ToString().Contains(query)) return true;
        return skill.Name != null && skill.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string Category(int id)
    {
        if (id >= 10000 && id < 20000) return "약공격";
        if (id >= 20000 && id < 30000) return "강공격";
        if (id >= 30000 && id < 40000) return "전략";
        return "기타";
    }
}
#endif
