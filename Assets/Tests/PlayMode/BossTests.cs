#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class BossTests
{
    private GameObject boss;
    private GameObject player;
    private BossController controller;
    private FixedBossPatternSelector selector;

    [SetUp]
    public void SetUp()
    {
        player = new GameObject("Boss Test Player");
        boss = new GameObject("Boss Test");
        boss.SetActive(false);
        boss.transform.position = new Vector3(0, 4, 0);
        boss.AddComponent<SpriteRenderer>().color = Color.magenta;
        selector = boss.AddComponent<FixedBossPatternSelector>();
        controller = boss.AddComponent<BossController>();
        var settings = new SerializedObject(controller);
        settings.FindProperty("target").objectReferenceValue = player.transform;
        settings.FindProperty("patternSelector").objectReferenceValue = selector;
        settings.FindProperty("patternInterval").floatValue = 100f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        boss.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (boss != null) Object.DestroyImmediate(boss);
        Object.DestroyImmediate(player);
    }

    [UnityTest]
    public IEnumerator StopsAtAttackDistanceThenResumesFollowingMovingPlayer()
    {
        for (int i = 0; i < 65; i++) yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(boss.transform.position, player.transform.position), Is.EqualTo(2f).Within(0.02f));
        Assert.That(selector.Calls, Is.GreaterThan(0));
        float before = boss.transform.position.y;
        player.transform.position = new Vector3(0, -4, 0);
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(boss.transform.position.y, Is.LessThan(before));
    }

    [UnityTest]
    public IEnumerator SelectsReplacementStrategyOnlyInRangeAndHonorsCooldown()
    {
        yield return null;
        Assert.That(selector.Calls, Is.Zero);
        var logs = new System.Collections.Generic.List<string>();
        Application.LogCallback capture = (message, trace, type) => logs.Add(message);
        Application.logMessageReceived += capture;
        try
        {
            boss.transform.position = new Vector3(0, 1.5f, 0);
            Physics2D.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (selector.Calls == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(selector.Calls, Is.EqualTo(1));
            Assert.That(logs, Does.Contain("[Boss] 임시 패턴2: 중거리 견제 공격 (로그 전용)"));
            Assert.That(selector.LastDistance, Is.EqualTo(1.5f).Within(0.1f));
            Assert.That(selector.LastHealth, Is.EqualTo(3));
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(selector.Calls, Is.EqualTo(1));
        }
        finally { Application.logMessageReceived -= capture; }
    }

    [UnityTest]
    public IEnumerator MissingOrInactivePlayerStopsMovementAndPatternSelection()
    {
        player.SetActive(false);
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(boss.transform.position.y, Is.EqualTo(4f).Within(0.001f));
        Assert.That(selector.Calls, Is.Zero);
        Object.DestroyImmediate(player);
        yield return null;
        Assert.That(selector.Calls, Is.Zero);
    }

    [UnityTest]
    public IEnumerator DamageFlashesAndLethalHitStopsBossImmediately()
    {
        var health = boss.GetComponent<EnemyHealth>();
        IDamageable damageable = health;
        damageable.TakeDamage(1);
        Assert.That(health.CurrentHealth, Is.EqualTo(2));
        Assert.That(boss.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
        damageable.TakeDamage(2);
        Assert.That(controller.enabled, Is.False);
        Assert.That(boss.GetComponent<EnemyFollow>().enabled, Is.False);
        Assert.That(boss.GetComponent<BoxCollider2D>().enabled, Is.False);
        Assert.That(boss.GetComponent<Rigidbody2D>().simulated, Is.False);
        yield return null;
        Assert.That(boss == null, Is.True);
    }

    [UnityTest]
    public IEnumerator DisablingBossControllerStopsFollowAndReenableRestoresIt()
    {
        controller.enabled = false;
        Assert.That(boss.GetComponent<EnemyFollow>().enabled, Is.False);
        yield return new WaitForFixedUpdate();
        float before = boss.transform.position.y;
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(boss.transform.position.y, Is.EqualTo(before));
        controller.enabled = true;
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Assert.That(boss.transform.position.y, Is.LessThan(before));
    }

    [Test]
    public void RandomSelectorReturnsOnlyTemporaryCandidatesAndPreservesAllCandidates()
    {
        var random = boss.AddComponent<RandomBossPatternSelector>();
        var savedState = Random.state;
        try
        {
            Random.InitState(421);
            var seen = new System.Collections.Generic.HashSet<BossPattern>();
            for (int i = 0; i < 100; i++)
            {
                var pattern = random.SelectPattern(2, 3);
                Assert.That((int)pattern, Is.InRange(0, 2));
                seen.Add(pattern);
            }
            Assert.That(seen.Count, Is.EqualTo(3));
        }
        finally { Random.state = savedState; }
    }
}
#endif
