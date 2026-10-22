#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BossEncounterTests
{
    private readonly List<GameObject> owned = new List<GameObject>();
    private BossEncounterController encounter;
    private BossController boss;
    private Text label;
    private EnemySpawner spawner;

    [SetUp]
    public void SetUp()
    {
        var player = Make("Encounter Test Player");
        var bossObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/BossMvp.prefab"));
        owned.Add(bossObject);
        boss = bossObject.GetComponent<BossController>();
        boss.SetTarget(player.transform);
        bossObject.SetActive(false);
        var spawnerHost = Make("Encounter Test Spawner");
        spawnerHost.SetActive(false);
        spawner = spawnerHost.AddComponent<EnemySpawner>();
        var spawnSettings = new SerializedObject(spawner);
        spawnSettings.FindProperty("enemyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/EnemyMvp.prefab");
        spawnSettings.FindProperty("player").objectReferenceValue = player.transform;
        spawnSettings.FindProperty("totalSpawnCount").intValue = 1;
        spawnSettings.ApplyModifiedPropertiesWithoutUndo();
        spawnerHost.SetActive(true);

        var host = Make("Encounter");
        host.SetActive(false);
        encounter = host.AddComponent<BossEncounterController>();
        var settings = new SerializedObject(encounter);
        settings.FindProperty("boss").objectReferenceValue = boss;
        settings.FindProperty("bossAppearTime").floatValue = 0.2f;
        var spawners = settings.FindProperty("spawners");
        spawners.arraySize = 1;
        spawners.GetArrayElementAtIndex(0).objectReferenceValue = spawner;
        settings.ApplyModifiedPropertiesWithoutUndo();
        var labelObject = new GameObject("Timer Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        owned.Add(labelObject);
        label = labelObject.GetComponent<Text>();
        var view = labelObject.AddComponent<EncounterTimerView>();
        labelObject.SetActive(false);
        var viewSettings = new SerializedObject(view);
        viewSettings.FindProperty("encounter").objectReferenceValue = encounter;
        viewSettings.FindProperty("label").objectReferenceValue = label;
        viewSettings.ApplyModifiedPropertiesWithoutUndo();
        host.SetActive(true);
        labelObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var item in owned)
            if (item != null) Object.DestroyImmediate(item);
        owned.Clear();
        foreach (var normal in Normals()) Object.DestroyImmediate(normal.gameObject);
    }

    [TestCase(0f, "00:00")]
    [TestCase(10f, "00:10")]
    [TestCase(65.9f, "01:05")]
    [TestCase(3600f, "60:00")]
    public void TimerUsesMinutesAndSeconds(float elapsed, string expected)
    {
        Assert.That(EncounterTimerView.FormatTime(elapsed), Is.EqualTo(expected));
    }

    [UnityTest]
    public IEnumerator BossTransitionFreezesTimeClearsActiveAndInactiveNormalsAndShowsMessage()
    {
        Assert.That(label.text, Is.EqualTo("00:00"));
        Assert.That(boss.gameObject.activeSelf, Is.False);
        var inactive = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/EnemyMvp.prefab"));
        inactive.SetActive(false);
        yield return WaitForBoss();
        Assert.That(encounter.ElapsedTime, Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(label.text, Is.EqualTo("보스를 처치하세요."));
        Assert.That(boss.gameObject.activeInHierarchy, Is.True);
        yield return null;
        Assert.That(inactive == null, Is.True);
        Assert.That(Normals(), Is.Empty);
        yield return new WaitForSeconds(0.25f);
        Assert.That(Normals(), Is.Empty);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(label.font.HasCharacter('보'), Is.True, "HUD font must render Korean.");
    }

    [UnityTest]
    public IEnumerator BossDeathResumesClockAndExhaustedSpawnerStartsNewWaveOnlyOnce()
    {
        yield return WaitForBoss();
        boss.GetComponent<EnemyHealth>().TakeDamage(10);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(label.text, Is.EqualTo("00:00"));
        Assert.That(Normals().Length, Is.EqualTo(1));
        yield return new WaitForSeconds(0.3f);
        Assert.That(encounter.ElapsedTime, Is.GreaterThan(0.2f));
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(boss == null, Is.True);
        Assert.That(Normals().Length, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator StopSpawningSurvivesComponentToggleAndRestartResetsLimit()
    {
        spawner.StopSpawning();
        spawner.enabled = false;
        spawner.enabled = true;
        encounter.enabled = false;
        foreach (var normal in Normals()) Object.DestroyImmediate(normal.gameObject);
        yield return new WaitForSeconds(0.1f);
        Assert.That(Normals(), Is.Empty);
        spawner.RestartSpawning();
        Assert.That(Normals().Length, Is.EqualTo(1));
    }

    private IEnumerator WaitForBoss()
    {
        float deadline = Time.realtimeSinceStartup + 3f;
        while (encounter.Phase != EncounterPhase.BossFight && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
    }

    private GameObject Make(string name)
    {
        var item = new GameObject(name);
        owned.Add(item);
        return item;
    }

    private static EnemyFollow[] Normals()
    {
        return Object.FindObjectsByType<EnemyFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(e => e.GetComponent<BossController>() == null && e.name == "EnemyMvp(Clone)").ToArray();
    }
}
#endif
