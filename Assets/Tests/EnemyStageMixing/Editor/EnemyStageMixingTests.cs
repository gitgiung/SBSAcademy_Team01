using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EnemyStageMixingTests
{
    private const string Folder = "Assets/1.Prefabs/Normal Enemy/Temp_enemy";
    private string fixtureFolder;
    private bool fixtureCreated;
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject host;
    private EnemySpawnDirector director;
    private static Type Importer => Type.GetType("EnemyStageRosterImporter, Assembly-CSharp-Editor", true);

    [SetUp]
    public void SetUp()
    {
        fixtureFolder = "Assets/StageMixingTestFixtures_" + Guid.NewGuid().ToString("N");
        fixtureCreated = false;
        host = new GameObject("Stage mixing test");
        director = host.AddComponent<EnemySpawnDirector>();
        Assert.That(Register(Folder, out string error), Is.True, error);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(host);
        if (fixtureCreated && AssetDatabase.IsValidFolder(fixtureFolder)) AssetDatabase.DeleteAsset(fixtureFolder);
        Time.timeScale = 1;
    }

    private bool Register(string folder, out string error)
    {
        object[] args = { director, folder, null };
        bool result = (bool)Importer.GetMethod("TryRegister").Invoke(null, args);
        error = (string)args[2];
        return result;
    }

    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private void CreateFixtureFolder()
    {
        Assert.That(AssetDatabase.IsValidFolder(fixtureFolder), Is.False);
        AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(fixtureFolder));
        fixtureCreated = true;
    }
    private double Share(double time, int stage)
    {
        director.StageMixing.BuildRoster(time, 1080, out GameObject[] prefabs, out int[] weights);
        string suffix = "_" + (stage + 1) * 3 + "min";
        return weights.Where((w, i) => prefabs[i].name.EndsWith(suffix, StringComparison.Ordinal)).Sum() / (double)weights.Sum();
    }

    [Test]
    public void RegistersRealPrefabsAndRoundTripsSerialization()
    {
        Assert.That(director.RosterMode, Is.EqualTo(EnemyRosterMode.StageMixing));
        Assert.That(director.StageMixing.IsValid, Is.True);
        var restored = JsonUtility.FromJson<EnemySpawnProgression>(JsonUtility.ToJson(director.StageMixing));
        Assert.That(restored.IsValid, Is.True);
        restored.BuildRoster(240, 1080, out GameObject[] prefabs, out int[] weights);
        Assert.That(prefabs.Length, Is.EqualTo(24));
        Assert.That(prefabs.Distinct().Count(), Is.EqualTo(24));
        Assert.That(weights.Count(w => w > 0), Is.EqualTo(8));
    }

    [TestCase(0d, 0, 1d)]
    [TestCase(179.999d, 0, 1d)]
    [TestCase(180d, 1, 0d)]
    [TestCase(210d, 1, 1d / 3)]
    [TestCase(240d, 1, 2d / 3)]
    [TestCase(270d, 1, 1d)]
    [TestCase(360d, 2, 0d)]
    [TestCase(390d, 2, 1d / 3)]
    [TestCase(900d, 5, 0d)]
    [TestCase(990d, 5, 1d)]
    [TestCase(1080d, 5, 1d)]
    public void MixesOnlyAfterStageBoundary(double time, int stage, double expected)
        => Assert.That(Share(time, stage), Is.EqualTo(expected).Within(.002));

    [Test]
    public void RegistrationPreservesEditedWeightsAndTransitionTime()
    {
        var serialized = new SerializedObject(director);
        var profile = serialized.FindProperty("stageMixing");
        profile.FindPropertyRelative("transitionSeconds").floatValue = 60;
        profile.FindPropertyRelative("types").GetArrayElementAtIndex(0).FindPropertyRelative("lateWeight").intValue = 7;
        serialized.ApplyModifiedProperties();
        Assert.That(Register(Folder, out string error), Is.True, error);
        serialized.Update();
        Assert.That(profile.FindPropertyRelative("transitionSeconds").floatValue, Is.EqualTo(60));
        Assert.That(profile.FindPropertyRelative("types").GetArrayElementAtIndex(0).FindPropertyRelative("lateWeight").intValue, Is.EqualTo(7));
    }

    [Test]
    public void MissingFolderLeavesExistingConfigurationUnchanged()
    {
        string before = EditorJsonUtility.ToJson(director);
        Assert.That(Register("Assets/DoesNotExist", out _), Is.False);
        Assert.That(EditorJsonUtility.ToJson(director), Is.EqualTo(before));
    }

    [Test]
    public void MissingSlotsLeaveExistingConfigurationUnchanged()
    {
        CreateFixtureFolder();
        string before = EditorJsonUtility.ToJson(director);
        Assert.That(Register(fixtureFolder, out string error), Is.False);
        Assert.That(error, Does.Contain("누락"));
        Assert.That(EditorJsonUtility.ToJson(director), Is.EqualTo(before));
    }

    [Test]
    public void DuplicateNamesLeaveExistingConfigurationUnchanged()
    {
        CreateFixtureFolder();
        AssetDatabase.CreateFolder(fixtureFolder, "Duplicate");
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Assert.That(AssetDatabase.CopyAsset(path, fixtureFolder + "/" + System.IO.Path.GetFileName(path)), Is.True);
        }
        Assert.That(AssetDatabase.CopyAsset(Folder + "/Enemy_movespeed2_3min.prefab", fixtureFolder + "/Duplicate/Enemy_movespeed2_3min.prefab"), Is.True);
        string before = EditorJsonUtility.ToJson(director);
        Assert.That(Register(fixtureFolder, out string error), Is.False);
        Assert.That(error, Does.Contain("중복"));
        Assert.That(EditorJsonUtility.ToJson(director), Is.EqualTo(before));
    }

    [Test]
    public void RegistrationCanBeUndone()
    {
        Set(director, "rosterMode", EnemyRosterMode.ManualWaves);
        Undo.IncrementCurrentGroup();
        Assert.That(Register(Folder, out string error), Is.True, error);
        Undo.FlushUndoRecordObjects();
        Undo.PerformUndo();
        Assert.That(director.RosterMode, Is.EqualTo(EnemyRosterMode.ManualWaves));
    }

    [Test]
    public void CleanupPreservesAnExistingSimilarlyNamedFolder()
    {
        const string existingFolder = "Assets/StageMixingTestFixtures";
        bool createdHere = !AssetDatabase.IsValidFolder(existingFolder);
        if (createdHere) AssetDatabase.CreateFolder("Assets", "StageMixingTestFixtures");
        try
        {
            TearDown();
            Assert.That(AssetDatabase.IsValidFolder(existingFolder), Is.True, "테스트가 소유하지 않은 기존 폴더를 삭제하면 안 됩니다.");
        }
        finally
        {
            if (createdHere && AssetDatabase.IsValidFolder(existingFolder)) AssetDatabase.DeleteAsset(existingFolder);
        }
    }

    [Test]
    public void ManualWeightedWavesRetainTheirOriginalBehavior()
    {
        GameObject first = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Enemy_movespeed2_3min.prefab");
        GameObject second = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Enemy_movespeed3_3min.prefab");
        GameObject next = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Enemy_movespeed4_6min.prefab");
        var a = new EnemySpawnWave.Entry(); Set(a, "prefab", first); Set(a, "weight", 7);
        var b = new EnemySpawnWave.Entry(); Set(b, "prefab", second); Set(b, "weight", 3);
        var c = new EnemySpawnWave.Entry(); Set(c, "prefab", next); Set(c, "weight", 1);
        var firstWave = new EnemySpawnWave(); Set(firstWave, "enemies", new[] { a, b });
        var nextWave = new EnemySpawnWave(); Set(nextWave, "enemies", new[] { c });
        Set(director, "rosterMode", EnemyRosterMode.ManualWaves);
        Set(director, "waves", new[] { firstWave, nextWave });
        var pool = InitializeDirector(); pool.RejectSecondAttempt = true;
        Advance(30);
        Assert.That(pool.Spawns.Count(p => p == first), Is.EqualTo(34));
        Assert.That(pool.Spawns.Count(p => p == second), Is.EqualTo(15));
        pool.Spawns.Clear(); Advance(60);
        Assert.That(pool.Spawns.Count, Is.EqualTo(130));
        Assert.That(pool.Spawns, Has.All.EqualTo(next));
    }

    private EnemyStageMixingTestPool InitializeDirector()
    {
        var pool = host.AddComponent<EnemyStageMixingTestPool>();
        var cameraHost = new GameObject("Test camera");
        cameraHost.transform.SetParent(host.transform);
        cameraHost.transform.position = new Vector3(0, 0, -10);
        var camera = cameraHost.AddComponent<Camera>(); camera.orthographic = true;
        var spawnerHost = new GameObject("Test spawner"); spawnerHost.transform.SetParent(host.transform);
        Set(director, "poolProvider", pool); Set(director, "player", host.transform);
        Set(director, "playerCamera", camera); Set(director, "spawnerTemplate", spawnerHost.AddComponent<EnemySpawner>());
        director.Initialize();
        return pool;
    }
    private void Advance(float seconds) { for (int i = 0; i < (int)(seconds * 4); i++) director.Tick(.25f); }

    [Test]
    public void RealSpawnerUsesMixedRosterAndFreezesDuringPause()
    {
        var pool = InitializeDirector();
        Advance(210);
        Assert.That(pool.Spawns.All(p => p.name.EndsWith("_3min", StringComparison.Ordinal)), Is.True);
        int count = pool.Spawns.Count;
        director.SetPaused(true); director.Tick(90);
        Assert.That(director.ElapsedTime, Is.EqualTo(210)); Assert.That(pool.Spawns.Count, Is.EqualTo(count));
        director.SetPaused(false); pool.Spawns.Clear(); Advance(30);
        Assert.That(pool.Spawns.Any(p => p.name.EndsWith("_6min", StringComparison.Ordinal)), Is.True);
        Assert.That(pool.Spawns.Any(p => p.name.EndsWith("_3min", StringComparison.Ordinal)), Is.True);
        Advance(30); pool.Spawns.Clear(); Advance(30);
        Assert.That(pool.Spawns.All(p => p.name.EndsWith("_6min", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void RealSpawnerHandlesLargeTickPauseBoundaryAndCapacity()
    {
        var pool = InitializeDirector();
        director.Settings.maxAttemptsPerFrame = 10000;
        director.SetPauseBoundary(180); director.Tick(300);
        Assert.That(director.ElapsedTime, Is.EqualTo(180));
        Assert.That(pool.Spawns.All(p => p.name.EndsWith("_3min", StringComparison.Ordinal)), Is.True);
        director.ClearPauseBoundary(); director.Tick(120);
        Assert.That(pool.Spawns.Any(p => p.name.EndsWith("_6min", StringComparison.Ordinal)), Is.True);
        pool.BlockCapacity = true; int before = director.CapSkippedAttempts;
        director.Tick(30); Assert.That(director.CapSkippedAttempts, Is.GreaterThan(before));
    }
}

public sealed class EnemyStageMixingTestPool : MonoBehaviour, IEnemySpawnPool, IEnemySpawnDiagnostics
{
    public readonly List<GameObject> Spawns = new List<GameObject>();
    public bool BlockCapacity;
    public bool RejectSecondAttempt;
    private int attempts;
    public int ActiveEnemyCount => 0;
    public EnemySpawnFailure LastSpawnFailure { get; private set; }
    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene)
    {
        bool blocked = BlockCapacity || (++attempts == 2 && RejectSecondAttempt);
        LastSpawnFailure = blocked ? EnemySpawnFailure.Capacity : EnemySpawnFailure.None;
        if (blocked) return false;
        Spawns.Add(prefab); return true;
    }
    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene, Action<GameObject> spawned) => TrySpawn(prefab, position, target, scene);
    public void ReturnEnemy(GameObject enemy) { }
}
