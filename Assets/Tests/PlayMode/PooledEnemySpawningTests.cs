using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class PooledEnemySpawningTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private Scene loadedScene;
    private static Type RuntimeType(string name, string assembly = "EnemyMvp.Runtime")
    {
        Type type = Type.GetType(name + ", " + assembly);
        Assert.That(type, Is.Not.Null, name + " runtime feature must exist");
        return type;
    }

    private GameObject Create(string name)
    {
        GameObject go = new GameObject(name);
        objects.Add(go);
        return go;
    }

    private static object Call(object target, string name, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);
        Assert.That(method, Is.Not.Null, name + " must be available");
        return method.Invoke(target, args);
    }

    private static T Read<T>(object target, string name) =>
        (T)target.GetType().GetProperty(name).GetValue(target);

    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private Component NewPool() => Create("test pool bridge")
        .AddComponent(RuntimeType("SceneEnemyPool", "Assembly-CSharp"));

    private GameObject NewPrefab()
    {
        GameObject prefab = Create("test enemy prefab");
        prefab.SetActive(false);
        prefab.AddComponent<SpriteRenderer>().color = Color.red;
        prefab.AddComponent(RuntimeType("EnemyFollow"));
        prefab.AddComponent(RuntimeType("EnemyHealth"));
        return prefab;
    }

    private GameObject[] Spawned() => UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None)
        .Where(c => c.GetType().Name == "EnemyHealth" && c.gameObject.name == "test enemy prefab")
        .Select(c => c.gameObject).ToArray();

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        foreach (GameObject go in objects)
            if (go != null) UnityEngine.Object.Destroy(go);
        objects.Clear();
        yield return null;
        if (loadedScene.IsValid() && loadedScene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(loadedScene);
        loadedScene = default;
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == "@Managers") UnityEngine.Object.Destroy(go);
        yield return null;
    }

    [Test]
    public void DeathReturnsAndReusesTheSameEnemyWithFullHealthAndPhysics()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Transform target = Create("target").transform;
        Assert.That(Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene), Is.True);
        GameObject enemy = Spawned().Single();
        Component health = enemy.GetComponent(RuntimeType("EnemyHealth"));
        enemy.GetComponent<Rigidbody2D>().linearVelocity = Vector2.one;
        health.GetType().GetMethod("TakeDamage", new[] { typeof(float) }).Invoke(health, new object[] { 999f });
        Assert.That(enemy.activeSelf, Is.False, "death must return immediately, not schedule Destroy");
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.Zero);
        Assert.That(Call(pool, "TrySpawn", prefab, Vector3.right, target, prefab.scene), Is.True);
        Assert.That(Spawned().Single(), Is.SameAs(enemy));
        Assert.That(Read<float>(health, "CurrentHealth"), Is.EqualTo(3f));
        Assert.That(Read<bool>(health, "IsDead"), Is.False);
        Assert.That(enemy.GetComponent<Rigidbody2D>().simulated, Is.True);
        Assert.That(enemy.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(enemy.GetComponent<Collider2D>().enabled, Is.True);
        Assert.That(((Behaviour)enemy.GetComponent(RuntimeType("EnemyFollow"))).enabled, Is.True);
        Assert.That(enemy.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.red));
    }

    [Test]
    public void ActiveLimitCountsLiveEnemiesAndAllowsReplacementAfterReturn()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Transform target = Create("target").transform;
        for (int i = 0; i < 130; i++)
            Assert.That(Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene), Is.True);
        Assert.That(Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene), Is.False);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.EqualTo(130));
        Call(pool, "ReturnEnemy", Spawned().First());
        Assert.That(Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene), Is.True);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.EqualTo(130));
    }

    [UnityTest]
    public IEnumerator DestroyingSceneOwnerReturnsOnlyItsEnemies()
    {
        Component pool = NewPool();
        Component other = NewPool();
        GameObject prefab = NewPrefab();
        Transform target = Create("target").transform;
        Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene);
        Call(other, "TrySpawn", prefab, Vector3.one, target, prefab.scene);
        UnityEngine.Object.Destroy(pool.gameObject);
        yield return null;
        Assert.That(Spawned(), Has.Length.EqualTo(1));
        Assert.That(Read<int>(other, "ActiveEnemyCount"), Is.EqualTo(1));
    }

    private Camera NewCamera()
    {
        Camera camera = Create("test camera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        return camera;
    }

    private Component NewSpawner(Component pool, GameObject prefab, Camera camera,
        int total = 5, int batch = 2, float interval = 1f)
    {
        GameObject go = Create("test spawner");
        go.SetActive(false);
        Component spawner = go.AddComponent(RuntimeType("EnemySpawner"));
        Call(spawner, "Configure", pool, Create("target").transform, new[] { prefab },
            interval, total, batch, camera, 1f);
        go.SetActive(true);
        return spawner;
    }

    [Test]
    public void SpawnerWaitsForIntervalSpawnsBatchesAndDisablesAtItsOwnTotal()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Component spawner = NewSpawner(pool, prefab, NewCamera());
        Assert.That(Spawned(), Is.Empty);
        Call(spawner, "Tick", 0.5f);
        Assert.That(Spawned(), Is.Empty);
        Call(spawner, "Tick", 0.5f);
        Assert.That(Spawned(), Has.Length.EqualTo(2));
        Call(spawner, "Tick", 1f);
        Assert.That(Spawned(), Has.Length.EqualTo(4));
        Call(spawner, "Tick", 1f);
        Assert.That(Spawned(), Has.Length.EqualTo(5));
        Assert.That(spawner.gameObject.activeSelf, Is.False);
        Assert.That(Spawned().All(go => go.activeInHierarchy), Is.True);
        Assert.That(Read<int>(spawner, "SpawnedCount"), Is.EqualTo(5));
    }

    [Test]
    public void EachSpawnerHasItsOwnTotalAndZeroTotalCompletesImmediately()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Camera camera = NewCamera();
        Component first = NewSpawner(pool, prefab, camera, 3, 2);
        Component second = NewSpawner(pool, prefab, camera, 3, 2);
        Call(first, "Tick", 1f);
        Call(second, "Tick", 1f);
        Call(first, "Tick", 1f);
        Call(second, "Tick", 1f);
        Assert.That(Spawned(), Has.Length.EqualTo(6));
        Component empty = NewSpawner(pool, prefab, camera, 0);
        Assert.That(empty.gameObject.activeSelf, Is.False);
        Assert.That(Spawned(), Has.Length.EqualTo(6));
    }

    [Test]
    public void FullPoolDoesNotConsumeQuotaAndReturnAllowsTheNextBatch()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Transform target = Create("target").transform;
        for (int i = 0; i < 129; i++)
            Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene);
        Component spawner = NewSpawner(pool, prefab, NewCamera(), 3, 2);
        Call(spawner, "Tick", 1f);
        Assert.That(Read<int>(spawner, "SpawnedCount"), Is.EqualTo(1));
        Call(spawner, "Tick", 1f);
        Assert.That(Read<int>(spawner, "SpawnedCount"), Is.EqualTo(1));
        Call(pool, "ReturnEnemy", Spawned().First());
        Call(pool, "ReturnEnemy", Spawned().First());
        Call(spawner, "Tick", 1f);
        Assert.That(Read<int>(spawner, "SpawnedCount"), Is.EqualTo(3));
        Assert.That(spawner.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void MovingCameraRepositionsSpawnerBeforeAnotherBatch()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Camera camera = NewCamera();
        Component spawner = NewSpawner(pool, prefab, camera);
        Call(spawner, "Tick", 1f);
        Vector3 first = spawner.transform.position;
        camera.transform.position = first + Vector3.back * 10f;
        Call(spawner, "Tick", 1f);
        Vector3 viewport = camera.WorldToViewportPoint(spawner.transform.position);
        Assert.That(viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f, Is.True);
        Assert.That(viewport.z, Is.GreaterThan(0f));
    }

    [Test]
    public void StoppingSpawnerPreservesItsQuotaUntilExplicitResume()
    {
        Component pool = NewPool();
        Component spawner = NewSpawner(pool, NewPrefab(), NewCamera());
        Call(spawner, "Tick", 0.5f);
        Call(spawner, "StopSpawning");
        Call(spawner, "Tick", 10f);
        Assert.That(Spawned(), Is.Empty);
        Call(spawner, "ResumeSpawning");
        Call(spawner, "Tick", 0.5f);
        Assert.That(Spawned(), Has.Length.EqualTo(2));
    }

    private Component NewDirector(Component pool, GameObject[] prefabs, int count,
        int[] totals, float interval = 1f)
    {
        GameObject templateObject = Create("spawner template");
        templateObject.SetActive(false);
        Component template = templateObject.AddComponent(RuntimeType("EnemySpawner"));
        Component director = Create("test director").AddComponent(RuntimeType("EnemySpawnDirector"));
        Type minuteType = RuntimeType("EnemySpawnMinute");
        Array settings = Array.CreateInstance(minuteType, totals.Length);
        for (int i = 0; i < totals.Length; i++)
        {
            object setting = Activator.CreateInstance(minuteType);
            Set(setting, "enemyPrefabs", new[] { prefabs[Math.Min(i, prefabs.Length - 1)] });
            Set(setting, "spawnInterval", interval);
            Set(setting, "totalSpawnCount", totals[i]);
            Set(setting, "batchSpawnCount", 2);
            settings.SetValue(setting, i);
        }
        Set(director, "poolProvider", pool);
        Set(director, "player", Create("target").transform);
        Set(director, "playerCamera", NewCamera());
        Set(director, "spawnerTemplate", template);
        Set(director, "spawnerCount", count);
        Set(director, "minutes", settings);
        Call(director, "Initialize");
        return director;
    }

    [Test]
    public void DirectorSchedulesConfiguredNumberOfSpawnersOncePerMinute()
    {
        Component pool = NewPool();
        Component director = NewDirector(pool, new[] { NewPrefab() }, 3, new[] { 3, 1 });
        Call(director, "Tick", 1f);
        Assert.That(Spawned(), Has.Length.EqualTo(6));
        Call(director, "Tick", 1f);
        Assert.That(Spawned(), Has.Length.EqualTo(9));
        Call(director, "Tick", 1f);
        Assert.That(Spawned(), Has.Length.EqualTo(9), "completed slots must wait for the next minute");
        Call(director, "Tick", 57f);
        Call(director, "Tick", 1f);
        Assert.That(Spawned(), Has.Length.EqualTo(12));
    }

    [Test]
    public void MinuteBoundaryFinishesOldQuotaBeforeUsingNewPrefabAndTotal()
    {
        Component pool = NewPool();
        GameObject first = NewPrefab();
        GameObject second = NewPrefab();
        second.name = "second enemy prefab";
        Component director = NewDirector(pool, new[] { first, second }, 1, new[] { 4, 1 }, 40f);
        Call(director, "Tick", 40f);
        Assert.That(Spawned(), Has.Length.EqualTo(2));
        Call(director, "Tick", 40f);
        Assert.That(Spawned(), Has.Length.EqualTo(4), "minute 0 quota must not be replaced mid-job");
        Call(director, "Tick", 0.1f);
        Call(director, "Tick", 40f);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.EqualTo(5));
        Assert.That(UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Count(go => go.name == "second enemy prefab"), Is.EqualTo(1));
    }

    [Test]
    public void DirectorPauseFreezesBothMinuteClockAndPartialSpawnInterval()
    {
        Component pool = NewPool();
        Component director = NewDirector(pool, new[] { NewPrefab() }, 1, new[] { 5 });
        Call(director, "Tick", 0.5f);
        Call(director, "SetPaused", true);
        Call(director, "Tick", 120f);
        Assert.That(Read<float>(director, "ElapsedTime"), Is.EqualTo(0.5f));
        Assert.That(Spawned(), Is.Empty);
        Call(director, "SetPaused", false);
        Call(director, "Tick", 0.5f);
        Assert.That(Spawned(), Has.Length.EqualTo(2));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator ReusedEnemyFollowsItsNewTarget()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Transform target = Create("target").transform;
        target.position = Vector3.right * 5f;
        Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene);
        GameObject enemy = Spawned().Single();
        Call(pool, "ReturnEnemy", enemy);
        target.position = Vector3.left * 5f;
        Call(pool, "TrySpawn", prefab, Vector3.zero, target, prefab.scene);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(enemy.transform.position.x, Is.LessThan(0f));
    }

    [Test]
    public void ZeroSpawnerCountGeneratesNothing()
    {
        Component pool = NewPool();
        Component director = NewDirector(pool, new[] { NewPrefab() }, 0, new[] { 5 });
        Call(director, "Tick", 120f);
        Assert.That(Read<int>(director, "SpawnerCount"), Is.Zero);
        Assert.That(Spawned(), Is.Empty);
    }

    [Test]
    public void NewMinuteWaitsForItsOwnFullIntervalBeforeSpawning()
    {
        Component pool = NewPool();
        Component director = NewDirector(pool, new[] { NewPrefab() }, 1, new[] { 1 });
        Call(director, "Tick", 1f);
        Call(director, "Tick", 58.5f);
        Call(director, "Tick", 0.5f);
        Assert.That(Spawned(), Has.Length.EqualTo(1));
        Call(director, "Tick", 0.5f);
        Assert.That(Spawned(), Has.Length.EqualTo(1), "new minute must not inherit the previous frame interval");
        Call(director, "Tick", 0.5f);
        Assert.That(Spawned(), Has.Length.EqualTo(2));
    }

    [Test]
    public void DisablingPoolOwnerReturnsItsActiveEnemies()
    {
        Component pool = NewPool();
        GameObject prefab = NewPrefab();
        Call(pool, "TrySpawn", prefab, Vector3.zero, Create("target").transform, prefab.scene);
        pool.gameObject.SetActive(false);
        Assert.That(Spawned(), Is.Empty);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.Zero);
    }

    [Test]
    public void MultiplePrefabTypesShareTheSameActiveLimit()
    {
        Component pool = NewPool();
        GameObject first = NewPrefab();
        GameObject second = NewPrefab();
        second.name = "second enemy prefab";
        Transform target = Create("target").transform;
        for (int i = 0; i < 65; i++)
        {
            Assert.That(Call(pool, "TrySpawn", first, Vector3.zero, target, first.scene), Is.True);
            Assert.That(Call(pool, "TrySpawn", second, Vector3.zero, target, first.scene), Is.True);
        }
        Assert.That(Call(pool, "TrySpawn", second, Vector3.zero, target, first.scene), Is.False);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.EqualTo(130));
    }

    [UnityTest]
    public IEnumerator OldOwnerCannotClearTheTargetOfANewerLease()
    {
        Component firstOwner = NewPool();
        Component secondOwner = NewPool();
        GameObject prefab = NewPrefab();
        Transform target = Create("target").transform;
        target.position = Vector3.right * 5f;
        Call(firstOwner, "TrySpawn", prefab, Vector3.zero, target, prefab.scene);
        GameObject enemy = Spawned().Single();
        object sharedPool = RuntimeType("Managers", "Assembly-CSharp").GetProperty("Pool").GetValue(null);
        Assert.That(Call(sharedPool, "Push", enemy), Is.True);
        target.position = Vector3.left * 5f;
        Call(secondOwner, "TrySpawn", prefab, Vector3.zero, target, prefab.scene);
        Assert.That(Spawned().Single(), Is.SameAs(enemy));
        UnityEngine.Object.Destroy(firstOwner.gameObject);
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That(Read<int>(secondOwner, "ActiveEnemyCount"), Is.EqualTo(1));
        Assert.That(enemy.transform.position.x, Is.LessThan(0f), "old lease must not change the current target");
    }

    [Test]
    public void NextMinutePlacesCompletedSpawnerNearTheCurrentCameraBoundary()
    {
        Component pool = NewPool();
        Component director = NewDirector(pool, new[] { NewPrefab() }, 1, new[] { 1 });
        Call(director, "Tick", 1f);
        Camera camera = objects.Single(go => go.name == "test camera").GetComponent<Camera>();
        camera.transform.position += Vector3.right * 1000f;
        Call(director, "Tick", 59f);
        Transform spawner = objects.Single(go => go.name == "Enemy Spawner 1").transform;
        Assert.That(Mathf.Abs(spawner.position.x - camera.transform.position.x), Is.LessThan(100f),
            "new job must be placed at the current camera boundary, not a distant previous location");
        Vector3 viewport = camera.WorldToViewportPoint(spawner.position);
        Assert.That(viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f, Is.True);
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator GiungSceneUsesPoolAndBossPauseReturnsNormalEnemies()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/9.Scenes/GiungTestScene.unity",
            new LoadSceneParameters(LoadSceneMode.Additive));
        loadedScene = SceneManager.GetSceneByName("GiungTestScene");
        yield return null;
        MonoBehaviour[] components = loadedScene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
        Component director = components.SingleOrDefault(c => c != null && c.GetType().Name == "EnemySpawnDirector");
        Assert.That(director, Is.Not.Null, "GiungTestScene must have the configured director");
        Component pool = components.Single(c => c != null && c.GetType().Name == "SceneEnemyPool");
        // Respect the scene's authored interval rather than the old one-second test default.
        Array minutes = (Array)director.GetType().GetField("minutes", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(director);
        float spawnInterval = Read<float>(minutes.GetValue(0), "SpawnInterval");
        Call(director, "Tick", spawnInterval);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.GreaterThan(0));
        Component encounter = components.Single(c => c != null && c.GetType().Name == "BossEncounterController");
        encounter.GetType().GetMethod("BeginBossFight", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(encounter, null);
        Assert.That(Read<bool>(director, "IsPaused"), Is.True);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.Zero);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Component boss = components.Single(c => c != null && c.GetType().Name == "BossController");
        Component bossHealth = boss.GetComponent(RuntimeType("EnemyHealth"));
        bossHealth.GetType().GetMethod("TakeDamage", new[] { typeof(float) })
            .Invoke(bossHealth, new object[] { 99999f });
        Assert.That(Read<bool>(director, "IsPaused"), Is.False);
        Call(director, "Tick", spawnInterval);
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.GreaterThan(0));
        object sharedPool = RuntimeType("Managers", "Assembly-CSharp").GetProperty("Pool").GetValue(null);
        GameObject enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/EnemyMvp.prefab");
        yield return SceneManager.UnloadSceneAsync(loadedScene);
        loadedScene = default;
        Assert.That(Call(sharedPool, "GetActiveCount", enemyPrefab), Is.EqualTo(0));
        Assert.That(UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .Any(c => c != null && c.GetType().Name == "EnemyHealth"), Is.False,
            "scene-owned rented enemies must not survive the scene");
    }
#endif
}
