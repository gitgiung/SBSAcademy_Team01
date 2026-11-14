using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>편집 시에만 이름을 해석하고 실제 프리팹 참조를 직렬화합니다.</summary>
public static class EnemyStageRosterImporter
{
    public const string DefaultFolder = "Assets/1.Prefabs/Normal Enemy/Temp_enemy";
    private static readonly string[] TypeNames = { "Enemy_movespeed2", "Enemy_movespeed3", "Enemy_movespeed4", "ZombieHound" };

    public static bool TryLoad(string folder, out GameObject[][] prefabs, out string error)
    {
        prefabs = null;
        error = null;
        if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
        { error = "몬스터 프리팹 폴더를 선택하세요."; return false; }

        var result = new GameObject[TypeNames.Length][];
        for (int i = 0; i < result.Length; i++) result[i] = new GameObject[EnemySpawnProgression.StageCount];
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            for (int type = 0; type < TypeNames.Length; type++)
            {
                for (int stage = 0; stage < EnemySpawnProgression.StageCount; stage++)
                {
                    if (name != TypeNames[type] + "_" + (stage + 1) * 3 + "min") continue;
                    if (result[type][stage] != null)
                    { error = "같은 단계의 프리팹 이름이 중복됩니다: " + name; return false; }
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null || !prefab.TryGetComponent<EnemyHealth>(out _) || !prefab.TryGetComponent<EnemyFollow>(out _))
                    { error = "프리팹 루트의 EnemyHealth/EnemyFollow를 확인하세요: " + path; return false; }
                    result[type][stage] = prefab;
                }
            }
        }
        var missing = new List<string>();
        for (int type = 0; type < TypeNames.Length; type++)
            for (int stage = 0; stage < EnemySpawnProgression.StageCount; stage++)
                if (result[type][stage] == null) missing.Add(TypeNames[type] + "_" + (stage + 1) * 3 + "min");
        if (missing.Count > 0)
        { error = "프리팹 누락 (기존 설정은 유지됩니다):\n" + string.Join("\n", missing); return false; }
        prefabs = result;
        return true;
    }

    public static bool TryRegister(EnemySpawnDirector director, string folder, out string error)
    {
        if (director == null || Application.isPlaying)
        { error = "Play를 종료한 뒤 스폰 Director를 선택하세요."; return false; }
        if (!TryLoad(folder, out GameObject[][] prefabs, out error)) return false;

        var serialized = new SerializedObject(director);
        SerializedProperty types = serialized.FindProperty("stageMixing").FindPropertyRelative("types");
        var savedWeights = new Dictionary<string, Vector2Int>();
        for (int i = 0; i < types.arraySize; i++)
        {
            SerializedProperty type = types.GetArrayElementAtIndex(i);
            savedWeights[type.FindPropertyRelative("typeName").stringValue ?? ""] = new Vector2Int(
                type.FindPropertyRelative("earlyWeight").intValue, type.FindPropertyRelative("lateWeight").intValue);
        }
        Undo.RecordObject(director, "몬스터 6단계 자동 등록");
        types.arraySize = TypeNames.Length;
        for (int i = 0; i < TypeNames.Length; i++)
        {
            SerializedProperty type = types.GetArrayElementAtIndex(i);
            type.FindPropertyRelative("typeName").stringValue = TypeNames[i];
            Vector2Int weights = savedWeights.TryGetValue(TypeNames[i], out Vector2Int saved) ? saved : Vector2Int.one;
            type.FindPropertyRelative("earlyWeight").intValue = weights.x;
            type.FindPropertyRelative("lateWeight").intValue = weights.y;
            SerializedProperty stages = type.FindPropertyRelative("stagePrefabs");
            stages.arraySize = EnemySpawnProgression.StageCount;
            for (int s = 0; s < stages.arraySize; s++) stages.GetArrayElementAtIndex(s).objectReferenceValue = prefabs[i][s];
        }
        serialized.FindProperty("rosterMode").enumValueIndex = (int)EnemyRosterMode.StageMixing;
        serialized.ApplyModifiedProperties();
        return true;
    }
}

[CustomEditor(typeof(EnemySpawnDirector))]
public sealed class EnemySpawnDirectorEditor : Editor
{
    private DefaultAsset folder;
    private int previewWave = 1;
    private string importError;
    private void OnEnable() { folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EnemyStageRosterImporter.DefaultFolder); }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var director = (EnemySpawnDirector)target;
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            SerializedProperty mode = serializedObject.FindProperty("rosterMode");
            EditorGUILayout.PropertyField(mode, new GUIContent("몬스터 구성 모드"));
            if (mode.enumValueIndex == (int)EnemyRosterMode.StageMixing)
            {
                EditorGUILayout.HelpBox("3분 경계부터 새 단계가 섞입니다. 각 종류의 Stage Prefabs는 0–3분부터 15–18분까지 순서입니다. Early/Late Weight는 초반/후반 상대 비율입니다.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stageMixing"), new GUIContent("단계 혼합 설정"), true);
            }
            else EditorGUILayout.PropertyField(serializedObject.FindProperty("waves"), new GUIContent("수동 웨이브"), true);

            folder = (DefaultAsset)EditorGUILayout.ObjectField("자동 등록 폴더", folder, typeof(DefaultAsset), false);
            if (GUILayout.Button("폴더에서 24개 자동 등록 · 단계 혼합 사용"))
            {
                serializedObject.ApplyModifiedProperties();
                EnemyStageRosterImporter.TryRegister(director, AssetDatabase.GetAssetPath(folder), out importError);
                serializedObject.Update();
            }
            if (!string.IsNullOrEmpty(importError)) EditorGUILayout.HelpBox(importError, MessageType.Error);
            DrawPropertiesExcluding(serializedObject, "m_Script", "rosterMode", "stageMixing", "waves", "legacyMinutes");
            serializedObject.ApplyModifiedProperties();
        }
        if (Application.isPlaying) EditorGUILayout.HelpBox("실행 중에는 설정을 편집할 수 없습니다. 아래 비율은 현재 진행 시간의 웨이브 기준입니다.", MessageType.Info);
        DrawPreview(director);
    }

    private void DrawPreview(EnemySpawnDirector director)
    {
        if (director.RosterMode != EnemyRosterMode.StageMixing) return;
        if (director.StageMixing == null || !director.StageMixing.IsValid || !director.Settings.IsValid)
        { EditorGUILayout.HelpBox("단계 프리팹 6개씩, 종류별 가중치(끝점마다 합계 > 0), 전환 시간(0–180초), 생존 설정을 확인하세요.", MessageType.Warning); return; }
        int wave = Application.isPlaying ? director.Settings.WaveIndex(director.ElapsedTime) :
            (previewWave = EditorGUILayout.IntSlider("스폰 비율 미리보기 · 웨이브", previewWave, 1, director.Settings.waveCount)) - 1;
        double time = wave * (double)director.Settings.waveSeconds;
        director.StageMixing.BuildRoster(time, director.Settings.Duration, out GameObject[] prefabs, out int[] weights);
        long total = 0;
        foreach (int weight in weights) total += weight;
        EditorGUILayout.LabelField($"웨이브 {wave + 1} · {time / 60:0.0}분", EditorStyles.boldLabel);
        for (int i = 0; i < weights.Length; i++)
            if (weights[i] > 0) EditorGUILayout.LabelField(prefabs[i].name, (100d * weights[i] / total).ToString("0.0") + "%");
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
