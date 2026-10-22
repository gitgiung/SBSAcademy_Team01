using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class EncounterMvpSceneBuilder
{
    [MenuItem("Tools/Enemy MVP/Add Timer And Boss Encounter")]
    public static void AddTimerAndBossEncounter()
    {
        if (!BossMvpSceneBuilder.EnsureBossInTestScene()) return;
        var scene = EditorSceneManager.GetActiveScene();
        BossController boss = null;
        BossEncounterController encounter = null;
        var spawners = new System.Collections.Generic.List<EnemySpawner>();
        Canvas canvas = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.TryGetComponent<BossController>(out var foundBoss)) boss = foundBoss;
            if (root.TryGetComponent<BossEncounterController>(out var foundEncounter)) encounter = foundEncounter;
            spawners.AddRange(root.GetComponentsInChildren<EnemySpawner>(true));
            if (root.name == "Encounter HUD") canvas = root.GetComponent<Canvas>();
        }
        if (boss == null) throw new System.InvalidOperationException("The test scene needs a boss.");
        boss.gameObject.SetActive(false);
        if (encounter == null) encounter = new GameObject("Boss Encounter").AddComponent<BossEncounterController>();
        var settings = new SerializedObject(encounter);
        settings.FindProperty("boss").objectReferenceValue = boss;
        settings.FindProperty("bossAppearTime").floatValue = 10;
        var slots = settings.FindProperty("spawners");
        slots.arraySize = spawners.Count;
        for (int i = 0; i < spawners.Count; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = spawners[i];
        settings.ApplyModifiedPropertiesWithoutUndo();

        if (canvas == null)
        {
            var hud = new GameObject("Encounter HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas = hud.GetComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var view = canvas.GetComponentInChildren<EncounterTimerView>(true);
        if (view == null)
        {
            var timer = new GameObject("Timer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timer.transform.SetParent(canvas.transform, false);
            view = timer.AddComponent<EncounterTimerView>();
        }
        var rect = view.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -24);
        rect.sizeDelta = new Vector2(600, 70);
        var label = view.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 36;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = "00:00";
        var outline = view.GetComponent<Outline>();
        if (outline == null) outline = view.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
        var viewSettings = new SerializedObject(view);
        viewSettings.FindProperty("encounter").objectReferenceValue = encounter;
        viewSettings.FindProperty("label").objectReferenceValue = label;
        viewSettings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("ENCOUNTER_MVP_SCENE_READY");
    }
}
