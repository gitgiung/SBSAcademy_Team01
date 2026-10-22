#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyCollisionTests
{
    private readonly List<GameObject> owned = new List<GameObject>();
    private Transform target;

    [SetUp]
    public void SetUp()
    {
        var player = new GameObject("Collision Test Target");
        owned.Add(player);
        target = player.transform;
        target.position = new Vector3(4, 0, 0);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var item in owned)
            if (item != null) Object.DestroyImmediate(item);
        owned.Clear();
    }

    [UnityTest]
    public IEnumerator FollowersRemainSeparatedWhenCrowdingSameTarget()
    {
        var front = Spawn(new Vector2(0.8f, 0));
        var rear = Spawn(Vector2.zero);
        Assert.That(front.GetComponent<BoxCollider2D>(), Is.Not.Null, "Enemy needs a solid body collider.");
        Assert.That(front.GetComponent<BoxCollider2D>().isTrigger, Is.False);
        Assert.That(front.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
        for (int i = 0; i < 150; i++)
        {
            yield return new WaitForFixedUpdate();
            AssertSeparated(front, rear);
        }
        Assert.That(front.GetComponent<Rigidbody2D>().position.x, Is.GreaterThan(3));
        Assert.That(rear.GetComponent<Rigidbody2D>().position.x, Is.GreaterThan(2));
        Assert.That(front.GetComponent<CircleCollider2D>().isTrigger, Is.True, "Keep the damage trigger.");
    }

    [UnityTest]
    public IEnumerator EnemiesSpawnedAtSamePointSeparateInsteadOfRemainingStacked()
    {
        var first = Spawn(Vector2.zero);
        var second = Spawn(Vector2.zero);
        Assert.That(first.GetComponent<BoxCollider2D>(), Is.Not.Null);
        for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
        AssertSeparated(first, second);
    }

    [UnityTest]
    public IEnumerator RearEnemyContinuesAfterBlockingEnemyDies()
    {
        target.position = new Vector3(1.5f, 0, 0);
        var front = Spawn(Vector2.right);
        var rear = Spawn(Vector2.zero);
        Assert.That(front.GetComponent<BoxCollider2D>(), Is.Not.Null);
        // Pin the blocker so the test measures opening a passage, rather than pushing it forward.
        front.GetComponent<EnemyFollow>().enabled = false;
        front.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
        AssertSeparated(front, rear);
        float before = rear.GetComponent<Rigidbody2D>().position.x;
        front.GetComponent<EnemyHealth>().TakeDamage(3);
        Assert.That(front.GetComponent<BoxCollider2D>().enabled, Is.False);
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(rear.GetComponent<Rigidbody2D>().position.x, Is.GreaterThan(before + 0.2f));
    }

    private GameObject Spawn(Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/EnemyMvp.prefab");
        var enemy = Object.Instantiate(prefab, position, Quaternion.identity);
        owned.Add(enemy);
        enemy.GetComponent<EnemyFollow>().SetTarget(target);
        return enemy;
    }

    private static void AssertSeparated(GameObject first, GameObject second)
    {
        var distance = first.GetComponent<BoxCollider2D>().Distance(second.GetComponent<BoxCollider2D>());
        Assert.That(distance.distance, Is.GreaterThanOrEqualTo(-0.02f), "Body overlap exceeded physics contact tolerance.");
    }
}
#endif
