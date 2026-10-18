#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyHealthTests
{
    private GameObject enemy;
    private EnemyHealth health;
    private SpriteRenderer visual;

    [SetUp]
    public void SetUp()
    {
        enemy = new GameObject("Damage Test Enemy");
        visual = enemy.AddComponent<SpriteRenderer>();
        visual.color = Color.red;
        enemy.AddComponent<EnemyFollow>();
        enemy.AddComponent<CircleCollider2D>();
        health = enemy.AddComponent<EnemyHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        if (enemy != null) Object.DestroyImmediate(enemy);
    }

    [UnityTest]
    public IEnumerator DamageThroughInterfaceReducesHealthAndRestoresHitColor()
    {
        IDamageable target = health;
        target.TakeDamage(1);
        Assert.That(health.CurrentHealth, Is.EqualTo(2));
        Assert.That(visual.color, Is.Not.EqualTo(Color.red));
        float deadline = Time.realtimeSinceStartup + 2f;
        while (visual.color != Color.red && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(visual.color, Is.EqualTo(Color.red));
    }

    [Test]
    public void NonPositiveDamageDoesNotHealOrHurt()
    {
        health.TakeDamage(0);
        health.TakeDamage(-2);
        Assert.That(health.CurrentHealth, Is.EqualTo(3));
        Assert.That(visual.color, Is.EqualTo(Color.red));
    }

    [UnityTest]
    public IEnumerator LethalDamageStopsMovementAndCollisionsImmediatelyThenDestroys()
    {
        health.TakeDamage(int.MaxValue);
        Assert.That(health.CurrentHealth, Is.Zero);
        Assert.That(health.IsDead, Is.True);
        Assert.That(enemy.GetComponent<EnemyFollow>().enabled, Is.False);
        Assert.That(enemy.GetComponent<Collider2D>().enabled, Is.False);
        Assert.That(enemy.GetComponent<Rigidbody2D>().simulated, Is.False);
        health.TakeDamage(1);
        Assert.That(health.CurrentHealth, Is.Zero);
        yield return null;
        Assert.That(enemy == null, Is.True);
    }

    [Test]
    public void DisableDuringHitRestoresOriginalColor()
    {
        health.TakeDamage(1);
        health.enabled = false;
        Assert.That(visual.color, Is.EqualTo(Color.red));
    }
}
#endif
