#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class EnemyMvpSceneTests
{
    private Scene scene;

    [UnityTest]
    public IEnumerator ExampleSceneSpawnsVisibleEnemiesThatFollowTheMarker()
    {
        scene = EditorSceneManager.LoadSceneInPlayMode("Assets/9.Scenes/EnemyMvpTestScene.unity",
            new LoadSceneParameters(LoadSceneMode.Additive));
        yield return null;
        var roots = scene.GetRootGameObjects();
        var target = roots.Single(o => o.name.StartsWith("Player Target"));
        var camera = roots.Select(o => o.GetComponent<Camera>()).First(c => c != null);
        var clone = Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None)
            .First(e => e.name == "EnemyMvp(Clone)");
        Assert.That(clone.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
        Assert.That(target.GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
        Vector3 viewport = camera.WorldToViewportPoint(clone.transform.position);
        Assert.That(viewport.x, Is.InRange(0f, 1f));
        Assert.That(viewport.y, Is.InRange(0f, 1f));
        Assert.That(viewport.z, Is.GreaterThan(0f));

        float before = Vector2.Distance(clone.transform.position, target.transform.position);
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Assert.That(Vector2.Distance(clone.transform.position, target.transform.position), Is.LessThan(before));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (scene.IsValid() && scene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(scene);
        foreach (var enemy in Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None))
            if (enemy.name == "EnemyMvp(Clone)") Object.Destroy(enemy.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ExampleSceneBossTakesMeleeDamageAndStopsOnDeath()
    {
        scene = EditorSceneManager.LoadSceneInPlayMode("Assets/9.Scenes/EnemyMvpTestScene.unity",
            new LoadSceneParameters(LoadSceneMode.Additive));
        yield return null;
        yield return WaitForScheduledBoss(scene);
        var roots = scene.GetRootGameObjects();
        var player = roots.Single(o => o.name.StartsWith("Player Target"));
        var boss = roots.Single(o => o.GetComponent<BossController>() != null);
        var health = boss.GetComponent<EnemyHealth>();
        Assert.That(health.CurrentHealth, Is.EqualTo(10));
        Assert.That(boss.GetComponent<RandomBossPatternSelector>(), Is.Not.Null);
        boss.GetComponent<Rigidbody2D>().position = (Vector2)player.transform.position + Vector2.right * 0.8f;
        Physics2D.SyncTransforms();
        var attack = player.GetComponent<PlayerMeleeAttack>();
        Assert.That(attack.TryAttack(Vector2.right), Is.True);
        Assert.That(health.CurrentHealth, Is.EqualTo(9), "Body and trigger must not duplicate damage.");
        Assert.That(boss.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
        yield return new WaitForSeconds(0.35f);
        var attackSettings = new UnityEditor.SerializedObject(attack);
        attackSettings.FindProperty("damage").intValue = 9;
        attackSettings.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(attack.TryAttack(Vector2.right), Is.True);
        Assert.That(boss.GetComponent<BossController>().enabled, Is.False);
        Assert.That(boss.GetComponent<EnemyFollow>().enabled, Is.False);
        Assert.That(boss.GetComponent<Rigidbody2D>().simulated, Is.False);
        yield return null;
        Assert.That(boss == null, Is.True);
    }

    [UnityTest]
    public IEnumerator TimerSceneFreezesAtTenAndResumesAfterBossDeath()
    {
        scene = EditorSceneManager.LoadSceneInPlayMode("Assets/9.Scenes/EnemyMvpTestScene.unity",
            new LoadSceneParameters(LoadSceneMode.Additive));
        yield return null;
        var roots = scene.GetRootGameObjects();
        var encounter = roots.Select(o => o.GetComponent<BossEncounterController>()).First(c => c != null);
        var label = roots.SelectMany(o => o.GetComponentsInChildren<UnityEngine.UI.Text>()).Single();
        var boss = roots.Single(o => o.GetComponent<BossController>() != null);
        Assert.That(label.text, Is.EqualTo("00:00"));
        Assert.That(boss.activeSelf, Is.False);
        yield return WaitForScheduledBoss(scene);
        yield return null;
        Assert.That(encounter.ElapsedTime, Is.EqualTo(10f));
        Assert.That(label.text, Is.EqualTo("보스를 처치하세요."));
        Assert.That(Object.FindObjectsByType<EnemyFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(e => e.gameObject.scene == scene && e.GetComponent<BossController>() == null), Is.Empty);
        boss.GetComponent<EnemyHealth>().TakeDamage(10);
        Assert.That(label.text, Is.EqualTo("00:10"));
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(Object.FindObjectsByType<EnemyFollow>(FindObjectsSortMode.None)
            .Any(e => e.gameObject.scene == scene && e.GetComponent<BossController>() == null), Is.True);
        yield return new WaitForSeconds(0.1f);
        Assert.That(encounter.ElapsedTime, Is.GreaterThan(10f));
    }

    private static IEnumerator WaitForScheduledBoss(Scene loadedScene)
    {
        var encounter = loadedScene.GetRootGameObjects().Select(o => o.GetComponent<BossEncounterController>()).First(c => c != null);
        float previousScale = Time.timeScale;
        try
        {
            Time.timeScale = 20;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (encounter.Phase != EncounterPhase.BossFight && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
        }
        finally { Time.timeScale = previousScale; }
    }

    [UnityTest]
    public IEnumerator ExampleScenePlayerCanKillSpawnedEnemyWithMelee()
    {
        scene = EditorSceneManager.LoadSceneInPlayMode("Assets/9.Scenes/EnemyMvpTestScene.unity",
            new LoadSceneParameters(LoadSceneMode.Additive));
        yield return null;
        var player = scene.GetRootGameObjects().Single(o => o.name.StartsWith("Player Target"));
        Assert.That(player.GetComponent<PlayerMvpMovement>(), Is.Not.Null);
        var attack = player.GetComponent<PlayerMeleeAttack>();
        Assert.That(attack, Is.Not.Null);
        var enemy = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)
            .First(e => e.name == "EnemyMvp(Clone)");
        Assert.That(enemy.GetComponent<Collider2D>().enabled, Is.True);
        enemy.GetComponent<Rigidbody2D>().position = (Vector2)player.transform.position + Vector2.right * 0.8f;
        Physics2D.SyncTransforms();
        Assert.That(attack.TryAttack(Vector2.right), Is.True);
        Assert.That(enemy.CurrentHealth, Is.EqualTo(2));
        yield return new WaitForSeconds(0.35f);
        Assert.That(attack.TryAttack(Vector2.right), Is.True);
        yield return new WaitForSeconds(0.35f);
        Assert.That(attack.TryAttack(Vector2.right), Is.True);
        Assert.That(enemy.IsDead, Is.True);
        yield return null;
        Assert.That(enemy == null, Is.True);
    }
}
#endif
