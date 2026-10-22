#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemySpawnerTests
{
    private readonly List<GameObject> owned = new List<GameObject>();
    private GameObject prefab;
    private GameObject target;
    private GameObject host;
    private EnemySpawner spawner;

    [SetUp]
    public void SetUp()
    {
        prefab = Make("SpawnTestEnemy");
        prefab.SetActive(false);
        prefab.AddComponent<EnemyFollow>();
        target = Make("Target");
        target.transform.position = new Vector3(8, 0, 0);
        host = Make("Spawner");
        host.SetActive(false);
        spawner = host.AddComponent<EnemySpawner>();
        Configure(3, 0.15f);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var clone in Clones())
            Object.DestroyImmediate(clone.gameObject);
        foreach (var item in owned)
            if (item != null) Object.DestroyImmediate(item);
        owned.Clear();
    }

    [UnityTest]
    public IEnumerator SpawnsImmediatelyAtIntervalsAndStopsAtLimit()
    {
        float startedAt = Time.time;
        host.SetActive(true);
        Assert.That(Clones().Length, Is.EqualTo(1));
        yield return WaitForSpawnCount(2);
        Assert.That(Time.time - startedAt, Is.GreaterThanOrEqualTo(0.149f));
        yield return WaitForSpawnCount(3);
        yield return new WaitForSeconds(0.2f);
        Assert.That(Clones().Length, Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator InactivePrefabClonesActivateAndFollowAssignedPlayer()
    {
        Configure(1, 0.1f);
        host.SetActive(true);
        var clone = Clones().Single();
        Assert.That(clone.gameObject.activeSelf, Is.True);
        float startX = clone.transform.position.x;
        for (int i = 0; i < 4; i++) yield return new WaitForFixedUpdate();
        Assert.That(clone.transform.position.x, Is.GreaterThan(startX));
    }

    [UnityTest]
    public IEnumerator CyclesSpawnPointsAndUsesHostForNullSlot()
    {
        var first = Make("Point");
        first.transform.position = new Vector3(-4, 2, 0);
        host.transform.position = new Vector3(1, 3, 0);
        var settings = new SerializedObject(prefab.GetComponent<EnemyFollow>());
        settings.FindProperty("moveSpeed").floatValue = 0;
        settings.ApplyModifiedPropertiesWithoutUndo();
        // Test placement independently: coincident live bodies now separate through physics.
        prefab.GetComponent<Rigidbody2D>().simulated = false;
        Configure(3, 0.05f, first.transform, null);
        host.SetActive(true);
        yield return WaitForSpawnCount(3);
        var positions = Clones().Select(c => c.transform.position).ToArray();
        Assert.That(positions.Count(p => p == first.transform.position), Is.EqualTo(2));
        Assert.That(positions.Count(p => p == host.transform.position), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator DisablePausesSpawningAndReenablePreservesTotal()
    {
        host.SetActive(true);
        spawner.enabled = false;
        yield return new WaitForSeconds(0.35f);
        Assert.That(Clones().Length, Is.EqualTo(1));
        spawner.enabled = true;
        yield return WaitForSpawnCount(3);
        spawner.enabled = false;
        spawner.enabled = true;
        yield return new WaitForSeconds(0.2f);
        Assert.That(Clones().Length, Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator ZeroCountDoesNotSpawn()
    {
        Configure(0, 0.1f);
        host.SetActive(true);
        yield return new WaitForSeconds(0.15f);
        Assert.That(Clones(), Is.Empty);
    }

    [UnityTest]
    public IEnumerator MissingPlayerWarnsOnceAndDoesNotSpawn()
    {
        Object.DestroyImmediate(target);
        LogAssert.Expect(LogType.Warning, "EnemySpawner requires a player and an enemy prefab with EnemyFollow on its root.");
        host.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        Assert.That(Clones(), Is.Empty);
    }

    [UnityTest]
    public IEnumerator MissingFollowComponentWarnsAndDoesNotSpawn()
    {
        Object.DestroyImmediate(prefab.GetComponent<EnemyFollow>());
        LogAssert.Expect(LogType.Warning, "EnemySpawner requires a player and an enemy prefab with EnemyFollow on its root.");
        host.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        Assert.That(Clones(), Is.Empty);
    }

    private GameObject Make(string name)
    {
        var item = new GameObject(name);
        owned.Add(item);
        return item;
    }

    private void Configure(int count, float interval, params Transform[] points)
    {
        var settings = new SerializedObject(spawner);
        settings.FindProperty("enemyPrefab").objectReferenceValue = prefab;
        settings.FindProperty("player").objectReferenceValue = target.transform;
        settings.FindProperty("totalSpawnCount").intValue = count;
        settings.FindProperty("spawnInterval").floatValue = interval;
        var slots = settings.FindProperty("spawnPoints");
        slots.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
            slots.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static EnemyFollow[] Clones()
    {
        return Object.FindObjectsByType<EnemyFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(e => e.name == "SpawnTestEnemy(Clone)").ToArray();
    }

    private static IEnumerator WaitForSpawnCount(int expected)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (Clones().Length < expected && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(Clones().Length, Is.GreaterThanOrEqualTo(expected));
    }
}
#endif
