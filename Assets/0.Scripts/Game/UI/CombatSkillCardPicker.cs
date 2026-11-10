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
            "Play 중 전투가 진행될 때 항목을 누르면 그 스킬만 레벨업 카드로 열립니다. 카드를 고르면 보유에 등록됩니다.",
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
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || target == null))
            {
                if (GUILayout.Button($"{skill.Id}  {skill.Name}  ·  {rarity}{note}"))
                    target.ShowChosenSkillCard(skill.Id);
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
