using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class EnemyWaveTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    [UnityTest]
    public IEnumerator WaveEntriesChangeAtThirtySecondsAndMixSuccessfulSpawnsByWeight()
    {
        yield return SceneManager.LoadSceneAsync("MainTestScene");

        var (director, pool, prefabs) = CreateDirector(new[] { new[] { 7, 3, 0 }, new[] { 0, 0, 1 } });
        pool.RejectSecondAttempt = true;

        for (int i = 0; i < 120; i++) director.Tick(.25f);
        Assert.That(director.ElapsedTime, Is.EqualTo(30f));
        Assert.That(director.AttemptedSpawns, Is.EqualTo(50));
        Assert.That(pool.Spawns.Count, Is.EqualTo(49));
        Assert.That(pool.Spawns.FindAll(prefab => prefab == prefabs[0]).Count, Is.EqualTo(34));
        Assert.That(pool.Spawns.FindAll(prefab => prefab == prefabs[1]).Count, Is.EqualTo(15));

        pool.Spawns.Clear();
        for (int i = 0; i < 240; i++) director.Tick(.25f);
        Assert.That(director.ElapsedTime, Is.EqualTo(90f));
        Assert.That(pool.Spawns.Count, Is.EqualTo(130));
        Assert.That(pool.Spawns, Has.All.EqualTo(prefabs[2]));
    }

    [UnityTest]
    public IEnumerator FailedPrefabDoesNotBlockOtherWeightedEnemies()
    {
        yield return SceneManager.LoadSceneAsync("MainTestScene");
        var (director, pool, prefabs) = CreateDirector(new[] { new[] { 1, 1, 0 } });
        pool.UnavailablePrefab = prefabs[0];

        for (int i = 0; i < 120; i++) director.Tick(.25f);
        Assert.That(director.AttemptedSpawns, Is.EqualTo(50));
        Assert.That(pool.Spawns.Count, Is.EqualTo(25));
        Assert.That(pool.Spawns, Has.All.EqualTo(prefabs[1]));
    }

    private static (EnemySpawnDirector, WaveTestPool, GameObject[]) CreateDirector(int[][] waveWeights)
    {
        var wavesField = typeof(EnemySpawnDirector).GetField("waves", Fields);
        Assert.That(wavesField, Is.Not.Null, "The director needs an Inspector wave list.");


        var host = new GameObject("Weighted Spawn Test");
        var director = host.AddComponent<EnemySpawnDirector>();
        var pool = host.AddComponent<WaveTestPool>();
        var spawnerHost = new GameObject("Spawner Template");
        spawnerHost.transform.SetParent(host.transform);
        var template = spawnerHost.AddComponent<EnemySpawner>();
        typeof(EnemySpawnDirector).GetField("spawnerTemplate", Fields).SetValue(director, template);
        typeof(EnemySpawnDirector).GetField("playerCamera", Fields).SetValue(director, Camera.main);
        typeof(EnemySpawnDirector).GetField("player", Fields).SetValue(director, GameObject.Find("Player Target").transform);
        var prefabHost = new GameObject("Inactive Test Enemies");
        prefabHost.transform.SetParent(host.transform);
        prefabHost.SetActive(false);
        GameObject[] prefabs = { MakeEnemy(prefabHost.transform), MakeEnemy(prefabHost.transform), MakeEnemy(prefabHost.transform) };

        Type waveType = wavesField.FieldType.GetElementType();
        var waves = Array.CreateInstance(waveType, waveWeights.Length);
        for (int i = 0; i < waveWeights.Length; i++)
            waves.SetValue(MakeWave(waveType, prefabs, waveWeights[i]), i);
        wavesField.SetValue(director, waves);
        typeof(EnemySpawnDirector).GetField("poolProvider", Fields).SetValue(director, pool);
        director.Initialize();
        return (director, pool, prefabs);
    }

    [UnityTearDown]
    public IEnumerator UnloadTestScene()
    {
        Scene scene = SceneManager.GetSceneByName("MainTestScene");
        if (!scene.isLoaded) yield break;

        SceneManager.SetActiveScene(SceneManager.CreateScene("EnemyWaveTestCleanup"));
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    private static GameObject MakeEnemy(Transform parent)
    {
        var enemy = new GameObject("Enemy");
        enemy.transform.SetParent(parent);
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<EnemyFollow>();
        return enemy;
    }

    private static object MakeWave(Type waveType, GameObject[] prefabs, int[] weights)
    {
        object wave = Activator.CreateInstance(waveType);
        var entriesField = waveType.GetField("enemies", Fields);
        Type entryType = entriesField.FieldType.GetElementType();
        var entries = Array.CreateInstance(entryType, prefabs.Length);
        for (int i = 0; i < prefabs.Length; i++)
        {
            object entry = Activator.CreateInstance(entryType);
            entryType.GetField("prefab", Fields).SetValue(entry, prefabs[i]);
            entryType.GetField("weight", Fields).SetValue(entry, weights[i]);
            entries.SetValue(entry, i);
        }
        entriesField.SetValue(wave, entries);
        return wave;
    }
}

public sealed class WaveTestPool : MonoBehaviour, IEnemySpawnPool, IEnemySpawnDiagnostics
{
    public readonly List<GameObject> Spawns = new List<GameObject>();
    private int attempts;
    public bool RejectSecondAttempt { get; set; }
    public GameObject UnavailablePrefab { get; set; }
    public int ActiveEnemyCount => 0;
    public EnemySpawnFailure LastSpawnFailure { get; private set; }
    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene)
    {
        if (++attempts == 2 && RejectSecondAttempt)
        {
            LastSpawnFailure = EnemySpawnFailure.Capacity;
            return false;
        }
        if (prefab == UnavailablePrefab)
        {
            LastSpawnFailure = EnemySpawnFailure.PoolUnavailable;
            return false;
        }
        LastSpawnFailure = EnemySpawnFailure.None;
        Spawns.Add(prefab);
        return true;
    }

    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene, Action<GameObject> spawned)
        => TrySpawn(prefab, position, target, scene);

    public void ReturnEnemy(GameObject enemy) { }
}
