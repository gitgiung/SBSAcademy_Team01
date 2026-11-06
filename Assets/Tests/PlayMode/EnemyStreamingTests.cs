using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class EnemyStreamingTests
{
    [UnityTest]
    public IEnumerator ScheduledSpawnsUseAllEdgesAndStayOutsideCamera()
    {
        yield return SceneManager.LoadSceneAsync("MainTestScene");
        var director = Object.FindAnyObjectByType<EnemySpawnDirector>();
        var camera = Camera.main;
        camera.aspect = 16f / 9f;
        var target = GameObject.Find("Player Target").transform;
        var host = new GameObject("Scheduled Spawn Test");
        host.SetActive(false);
        var spawner = host.AddComponent<EnemySpawner>();
        var pool = new RecordingPool();
        spawner.Configure(pool, target, new[] { GetPrefab(director) }, 1f, 100, 1, camera, 10f);
        host.SetActive(true);

        var oldState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(7542);
        var sides = new HashSet<int>();
        try
        {
            for (int i = 0; i < 64; i++)
            {
                Assert.That(spawner.TrySpawnScheduled(i, out _), Is.True);
                Vector3 viewport = camera.WorldToViewportPoint(pool.Position);
                int side = viewport.x < 0 ? 0 : viewport.x > 1 ? 1 : viewport.y < 0 ? 2 : viewport.y > 1 ? 3 : -1;
                Assert.That(side, Is.GreaterThanOrEqualTo(0));
                sides.Add(side);
            }
            Assert.That(sides.Count, Is.EqualTo(4));
        }
        finally { UnityEngine.Random.state = oldState; Object.Destroy(host); }
    }

    [UnityTest]
    public IEnumerator FarEnemiesFreeCapacityWithoutKillingAndNearEnemyStays()
    {
        yield return SceneManager.LoadSceneAsync("MainTestScene");
        var director = Object.FindAnyObjectByType<EnemySpawnDirector>();
        director.Initialize();
        director.enabled = false;
        var pool = director.SpawnPool;
        var diagnostics = (IEnemySpawnDiagnostics)pool;
        var scene = director.gameObject.scene;
        var target = GameObject.Find("Player Target").transform;
        var camera = Camera.main;
        camera.aspect = 16f / 9f;
        var prefab = GetPrefab(director);
        Vector3 near = camera.ViewportToWorldPoint(new Vector3(1.1f, .5f, 10));
        GameObject nearEnemy = null;
        int deaths = 0;
        var tracked = new List<EnemyHealth>();
        void RecordDeath() => deaths++;

        try
        {
            Assert.That(pool.TrySpawn(prefab, near, target, scene, enemy => nearEnemy = enemy), Is.True);
            int expectedNearby = diagnostics.ActiveEnemyCount;
            for (int index = expectedNearby; index < 130; index++)
            {
                var position = new Vector3(350 + index % 13 * 2, 350 + index / 13 * 2, target.position.z);
                Assert.That(pool.TrySpawn(prefab, position, target, scene, enemy =>
                {
                    var health = enemy.GetComponent<EnemyHealth>();
                    health.Died += RecordDeath;
                    tracked.Add(health);
                }), Is.True);
            }
            Assert.That(pool.TrySpawn(prefab, near, target, scene), Is.False);

            director.SetPaused(true);
            yield return new WaitForSeconds(.35f);
            Assert.That(diagnostics.ActiveEnemyCount, Is.EqualTo(130));

            director.SetPaused(false);
            yield return new WaitForSeconds(.45f);
            Assert.That(diagnostics.ActiveEnemyCount, Is.EqualTo(expectedNearby));
            Assert.That(nearEnemy.activeInHierarchy, Is.True);
            Assert.That(deaths, Is.Zero);
            var pickups = (IDictionary)pool.GetType()
                .GetField("activePickups", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pool);
            Assert.That(pickups.Count, Is.Zero);
            Assert.That(pool.TrySpawn(prefab, near, target, scene), Is.True);
        }
        finally
        {
            director.SetPaused(false);
            foreach (var health in tracked)
                if (health != null) health.Died -= RecordDeath;
        }
    }

    [UnityTearDown]
    public IEnumerator UnloadTestScene()
    {
        Scene scene = SceneManager.GetSceneByName("MainTestScene");
        if (!scene.isLoaded) yield break;

        SceneManager.SetActiveScene(SceneManager.CreateScene("EnemyStreamingTestCleanup"));
        yield return SceneManager.UnloadSceneAsync(scene);
    }

    private static GameObject GetPrefab(EnemySpawnDirector director)
    {
        var waves = (EnemySpawnWave[])typeof(EnemySpawnDirector)
            .GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
        return waves[0].Enemies[0].Prefab;
    }

    private sealed class RecordingPool : IEnemySpawnPool
    {
        public Vector3 Position { get; private set; }

        public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene)
        {
            Position = position;
            return true;
        }

        public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, Scene scene, Action<GameObject> spawned)
            => TrySpawn(prefab, position, target, scene);

        public void ReturnEnemy(GameObject enemy) { }
    }
}
