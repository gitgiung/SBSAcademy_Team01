using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BossEncounterScheduleTests
{
    private GameObject root;
    private EnemySpawnDirector director;
    private Transform player;
    private readonly List<Object> assets = new List<Object>();

    private static void Set(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, "필요한 설정: " + name);
        field.SetValue(target, value);
    }

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        root = new GameObject("Boss schedule tests");
        player = new GameObject("Player").transform;
        player.SetParent(root.transform);
        player.gameObject.AddComponent<EncounterTestPlayer>();
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.SetParent(root.transform);
        var template = new GameObject("Spawner template");
        template.SetActive(false);
        template.transform.SetParent(root.transform);
        var spawner = template.AddComponent<EnemySpawner>();
        var normal = Prefab("Normal", false);
        var minute = new EnemySpawnMinute();
        Set(minute, "enemyPrefabs", new[] { normal });
        director = root.AddComponent<EnemySpawnDirector>();
        Set(director, "poolProvider", root.AddComponent<EncounterTestPool>());
        Set(director, "player", player);
        Set(director, "playerCamera", camera);
        Set(director, "spawnerTemplate", spawner);
        Set(director, "spawnerCount", 0);
        Set(director, "legacyMinutes", new[] { minute });
        director.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        foreach (var asset in assets) if (asset != null) Object.DestroyImmediate(asset);
        assets.Clear();
        Time.timeScale = 1f;
    }

    private GameObject Prefab(string name, bool boss = true)
    {
        var prefab = new GameObject(name);
        prefab.SetActive(false);
        prefab.AddComponent<Rigidbody2D>();
        prefab.AddComponent<EnemyFollow>();
        prefab.AddComponent<EnemyHealth>();
        if (boss) prefab.AddComponent<ZombieSlimeBoss>();
        assets.Add(prefab);
        return prefab;
    }

    private ScriptableObject Schedule(float[] times, GameObject[] prefabs, Vector3? offset = null)
    {
        var type = typeof(BossEncounterController).Assembly.GetType("BossEncounterSchedule");
        Assert.That(type, Is.Not.Null, "다회차 보스 일정 기능 필요");
        var schedule = ScriptableObject.CreateInstance(type);
        assets.Add(schedule);
        var entryType = type.GetNestedType("Entry");
        Assert.That(entryType, Is.Not.Null);
        var entries = Array.CreateInstance(entryType, times.Length);
        for (int i = 0; i < times.Length; i++)
        {
            var entry = Activator.CreateInstance(entryType);
            Set(entry, "appearTime", times[i]);
            Set(entry, "bossPrefab", prefabs[i]);
            Set(entry, "spawnOffset", offset ?? Vector3.zero);
            entries.SetValue(entry, i);
        }
        Set(schedule, "entries", entries);
        return schedule;
    }

    private BossEncounterController Encounter(ScriptableObject schedule, bool useDirector = true,
        GameObject healthBar = null, BossController legacyBoss = null)
    {
        var go = new GameObject("Encounter");
        go.SetActive(false);
        go.transform.SetParent(root.transform);
        var encounter = go.AddComponent<BossEncounterController>();
        if (schedule != null) Set(encounter, "schedule", schedule);
        Set(encounter, "spawnDirector", useDirector ? director : null);
        Set(encounter, "boss", legacyBoss);
        Set(encounter, "bossTarget", player);
        Set(encounter, "bossSpawnAnchor", player);
        Set(encounter, "bossHealthSlider", healthBar);
        go.SetActive(true);
        return encounter;
    }

    private static EnemyHealth ActiveBoss(BossEncounterController encounter)
    {
        var bosses = encounter.GetComponentsInChildren<EnemyHealth>();
        Assert.That(bosses.Length, Is.EqualTo(1), "현재 회차 보스가 하나만 활성화되어야 함");
        return bosses[0];
    }

    [Test]
    public void BoundaryStopsOnlyProgressClockAndUsesAnchorOffset()
    {
        player.position = new Vector3(12, 20, 0);
        var encounter = Encounter(Schedule(new[] { 180f }, new[] { Prefab("First") }, new Vector3(0, 9.26f, 0)));
        director.Tick(179.99f);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.TimedCombat));
        director.Tick(500f);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(180f));
        Assert.That(director.IsPaused, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(ActiveBoss(encounter).transform.position, Is.EqualTo(new Vector3(12, 29.26f, 0)));
        director.Tick(20f);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(180f));
    }

    [Test]
    public void SixDifferentPrefabsAppearInOrderIncluding1080SecondBoundary()
    {
        float[] times = { 180, 360, 540, 720, 900, 1080 };
        var prefabs = new GameObject[6];
        for (int i = 0; i < 6; i++) prefabs[i] = Prefab("Boss " + (i + 1));
        var encounter = Encounter(Schedule(times, prefabs));
        for (int i = 0; i < 6; i++)
        {
            director.Tick(10000);
            Assert.That(encounter.ElapsedTime, Is.EqualTo(times[i]));
            Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
            var health = ActiveBoss(encounter);
            Assert.That(health.name, Does.StartWith("Boss " + (i + 1)));
            health.TakeDamage(health.MaxHealth);
            Assert.That(director.IsPaused, Is.False);
        }
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        director.Tick(60);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(1080));
        Assert.That(encounter.GetComponentsInChildren<EnemyHealth>().Length, Is.Zero);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void RepeatedPrefabCreatesFreshHealthAndIgnoresOldDeath()
    {
        var prefab = Prefab("Repeated");
        var encounter = Encounter(Schedule(new[] { 1f, 2f }, new[] { prefab, prefab }));
        director.Tick(10);
        var first = ActiveBoss(encounter);
        first.TakeDamage(3);
        director.Tick(10);
        var second = ActiveBoss(encounter);
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.IsDead, Is.False);
        Assert.That(second.CurrentHealth, Is.EqualTo(3));
        first.TakeDamage(3);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
        Assert.That(director.IsPaused, Is.True);
    }

    [Test]
    public void MissingRoundWarnsAndDoesNotArmAnUnreachableStop()
    {
        var schedule = Schedule(new[] { 180f, 360f }, new[] { null, Prefab("Second") });
        LogAssert.Expect(LogType.Warning, new Regex(".*1.*180.*"));
        var encounter = Encounter(schedule);
        director.Tick(200);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(200));
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.TimedCombat));
        director.Tick(200);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(360));
        Assert.That(ActiveBoss(encounter).name, Does.StartWith("Second"));
    }

    [Test]
    public void InvalidBossPrefabReportsErrorAndSkipsOnlyThatRound()
    {
        var schedule = Schedule(new[] { 1f, 2f }, new[] { Prefab("Not boss", false), Prefab("Valid") });
        LogAssert.Expect(LogType.Error, new Regex(".*1.*"));
        var encounter = Encounter(schedule);
        director.Tick(4);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(2));
        Assert.That(ActiveBoss(encounter).name, Does.StartWith("Valid"));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void InvalidTimeDoesNotFreezeClock(float time)
    {
        var schedule = Schedule(new[] { time }, new[] { Prefab("Invalid time") });
        LogAssert.Expect(LogType.Error, new Regex(".*"));
        var encounter = Encounter(schedule);
        director.Tick(4);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(director.ElapsedTime, Is.EqualTo(4));
    }

    [TestCase(1f)]
    [TestCase(.5f)]
    public void DuplicateOrDescendingTimeRejectsSchedule(float second)
    {
        var prefab = Prefab("Invalid order");
        var schedule = Schedule(new[] { 1f, second }, new[] { prefab, prefab });
        LogAssert.Expect(LogType.Error, new Regex(".*"));
        Encounter(schedule);
        director.Tick(4);
        Assert.That(director.ElapsedTime, Is.EqualTo(4));
    }

    [Test]
    public void DisableWhileWaitingClearsBoundaryAndEnableCatchesUpOnce()
    {
        var encounter = Encounter(Schedule(new[] { 1f, 5f }, new[] { Prefab("First"), Prefab("Next") }));
        encounter.enabled = false;
        director.Tick(2);
        Assert.That(director.ElapsedTime, Is.EqualTo(2));
        encounter.enabled = true;
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
        ActiveBoss(encounter).TakeDamage(3);
        director.Tick(10);
        Assert.That(director.ElapsedTime, Is.EqualTo(5));
        Assert.That(ActiveBoss(encounter).name, Does.StartWith("Next"));
    }

    [Test]
    public void DeathWhileDisabledIsHandledOnReenable()
    {
        var encounter = Encounter(Schedule(new[] { 1f, 2f }, new[] { Prefab("First"), Prefab("Next") }));
        director.Tick(10);
        var health = ActiveBoss(encounter);
        encounter.enabled = false;
        health.TakeDamage(3);
        encounter.enabled = true;
        Assert.That(director.IsPaused, Is.False);
        director.Tick(10);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(2));
        Assert.That(ActiveBoss(encounter).name, Does.StartWith("Next"));
    }

    [TestCase(400f)]
    [TestCase(1080f)]
    public void ReenableAfterSeveralBoundariesProcessesOverdueRoundsWithoutDeadlock(float elapsed)
    {
        var encounter = Encounter(Schedule(new[] { 180f, 360f, 1080f },
            new[] { Prefab("First overdue"), Prefab("Second overdue"), Prefab("Final") }));
        encounter.enabled = false;
        director.Tick(elapsed);
        encounter.enabled = true;
        ActiveBoss(encounter).TakeDamage(3);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
        Assert.That(ActiveBoss(encounter).name, Does.StartWith("Second overdue"));
        ActiveBoss(encounter).TakeDamage(3);
        if (elapsed < 1080) director.Tick(10000);
        Assert.That(ActiveBoss(encounter).name, Does.StartWith("Final"));
        ActiveBoss(encounter).TakeDamage(3);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(director.IsPaused, Is.False);
        Assert.That(director.ElapsedTime, Is.EqualTo(1080));
    }

    [Test]
    public void RuntimeInvalidPrefabsSkipAllOverdueRoundsAndReleaseBoundary()
    {
        var first = Prefab("Destroyed first");
        var second = Prefab("Destroyed second");
        var encounter = Encounter(Schedule(new[] { 180f, 360f }, new[] { first, second }));
        encounter.enabled = false;
        director.Tick(400);
        Object.DestroyImmediate(first);
        Object.DestroyImmediate(second);
        LogAssert.Expect(LogType.Error, new Regex(".*180.*"));
        LogAssert.Expect(LogType.Error, new Regex(".*360.*"));
        encounter.enabled = true;
        director.Tick(1);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(director.ElapsedTime, Is.EqualTo(401));
        Assert.That(director.IsPaused, Is.False);
    }

    [Test]
    public void SharedHealthBarTracksOnlyCurrentBoss()
    {
        var bar = new GameObject("Health bar", typeof(RectTransform), typeof(Slider));
        bar.transform.SetParent(root.transform);
        var slider = bar.GetComponent<Slider>();
        var secondPrefab = Prefab("Second");
        Set(secondPrefab.GetComponent<EnemyHealth>(), "maxHealth", 10f);
        var encounter = Encounter(Schedule(new[] { 1f, 2f }, new[] { Prefab("First"), secondPrefab }), true, bar);
        director.Tick(10);
        var first = ActiveBoss(encounter);
        first.TakeDamage(1);
        Assert.That(slider.value, Is.EqualTo(2f / 3f).Within(.001f));
        first.TakeDamage(2);
        Assert.That(bar.activeSelf, Is.False);
        director.Tick(10);
        Assert.That(bar.activeSelf, Is.True);
        Assert.That(slider.value, Is.EqualTo(1f));
        ActiveBoss(encounter).TakeDamage(2);
        Assert.That(slider.value, Is.EqualTo(.8f).Within(.001f));
        first.gameObject.SetActive(true);
        first.EnsureInitialized();
        Assert.That(slider.value, Is.EqualTo(.8f).Within(.001f));
    }

    [Test]
    public void StandaloneClockAlsoStartsScheduledRound()
    {
        var encounter = Encounter(Schedule(new[] { 1f }, new[] { Prefab("Standalone") }), false);
        Set(encounter, "standaloneElapsedTime", 1f);
        typeof(BossEncounterController).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(encounter, null);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
        Assert.That(encounter.ElapsedTime, Is.EqualTo(1f));
    }

    [Test]
    public void BeastReceivesTargetAndEntranceBeforeActivation()
    {
        var prefab = Prefab("Beast", false);
        var templates = new GameObject("Inactive prefab storage");
        templates.SetActive(false);
        assets.Add(templates);
        prefab.transform.SetParent(templates.transform);
        prefab.AddComponent<RandomBossPatternSelector>();
        prefab.AddComponent<BossController>();
        prefab.SetActive(true);
        var encounter = Encounter(Schedule(new[] { 1f }, new[] { prefab }));
        director.Tick(10);
        var beast = ActiveBoss(encounter).GetComponent<BossController>();
        Assert.That(beast.Target, Is.SameAs(player));
        Assert.That(beast.IsEntering, Is.True);
        Assert.That(beast.enabled, Is.True);
    }

    [Test]
    public void HordeAndSlimeUseExplicitTarget()
    {
        var otherPlayer = new GameObject("Selected player").transform;
        otherPlayer.SetParent(root.transform);
        var hordePrefab = Prefab("Horde", false);
        hordePrefab.AddComponent<HordeLordBoss>();
        var encounter = Encounter(Schedule(new[] { 1f, 2f }, new[] { hordePrefab, Prefab("Slime") }));
        Set(encounter, "bossTarget", otherPlayer);
        director.Tick(10);
        var horde = ActiveBoss(encounter);
        Assert.That(horde.GetComponent<HordeLordBoss>().Target, Is.SameAs(otherPlayer));
        horde.TakeDamage(3);
        director.Tick(10);
        Assert.That(ActiveBoss(encounter).GetComponent<EnemyFollow>().Target, Is.SameAs(otherPlayer));
    }

    [Test]
    public void AllMissingRoundsDoNotFallBackToLegacyBoss()
    {
        var prefab = Prefab("Legacy must stay hidden", false);
        prefab.AddComponent<RandomBossPatternSelector>();
        var legacy = prefab.AddComponent<BossController>();
        var schedule = Schedule(new[] { 1f, 2f }, new GameObject[2]);
        LogAssert.Expect(LogType.Warning, new Regex(".*1.*1.*"));
        LogAssert.Expect(LogType.Warning, new Regex(".*2.*2.*"));
        var encounter = Encounter(schedule, legacyBoss: legacy);
        director.Tick(20);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(director.ElapsedTime, Is.EqualTo(20));
        Assert.That(legacy.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void DestroyingEncounterDuringFightReleasesClockAndGeneratedBoss()
    {
        var encounter = Encounter(Schedule(new[] { 1f }, new[] { Prefab("Generated") }));
        director.Tick(10);
        var generated = ActiveBoss(encounter);
        Object.DestroyImmediate(encounter);
        Assert.That(generated.gameObject.activeSelf, Is.False);
        director.Tick(.5f);
        Assert.That(director.ElapsedTime, Is.EqualTo(1.5f));
    }

    [Test]
    public void PauseDoesNotConsumeUpcomingRound()
    {
        var encounter = Encounter(Schedule(new[] { 1f }, new[] { Prefab("After pause") }));
        Time.timeScale = 0;
        director.Tick(10);
        Assert.That(director.ElapsedTime, Is.Zero);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.TimedCombat));
        Time.timeScale = 1;
        director.Tick(10);
        Assert.That(director.ElapsedTime, Is.EqualTo(1));
        Assert.That(ActiveBoss(encounter).name, Does.StartWith("After pause"));
    }

    [Test]
    public void RoundBeyondDirectorDurationReportsErrorAndDoesNotBlockClock()
    {
        var schedule = Schedule(new[] { 1081f }, new[] { Prefab("Too late") });
        LogAssert.Expect(LogType.Error, new Regex(".*1081.*"));
        var encounter = Encounter(schedule);
        director.Tick(2000);
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
        Assert.That(director.ElapsedTime, Is.EqualTo(1080));
    }

    [Test]
    public void LegacySingleBossStillStopsAtConfiguredTime()
    {
        var prefab = Prefab("Legacy", false);
        prefab.AddComponent<RandomBossPatternSelector>();
        var boss = prefab.AddComponent<BossController>();
        var go = new GameObject("Legacy encounter");
        go.SetActive(false);
        go.transform.SetParent(root.transform);
        var encounter = go.AddComponent<BossEncounterController>();
        Set(encounter, "boss", boss);
        Set(encounter, "spawnDirector", director);
        Set(encounter, "bossAppearTime", 1f);
        go.SetActive(true);
        director.Tick(2);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(1));
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.BossFight));
        boss.GetComponent<EnemyHealth>().TakeDamage(3);
        director.Tick(.5f);
        Assert.That(encounter.ElapsedTime, Is.EqualTo(1.5f));
        Assert.That(encounter.Phase, Is.EqualTo(EncounterPhase.AfterBoss));
    }
}

public sealed class EncounterTestPlayer : MonoBehaviour, IContactDamageReceiver
{
    public void TakeContactDamage(float damage, GameObject source) { }
}

public sealed class EncounterTestPool : MonoBehaviour, IEnemySpawnPool
{
    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, UnityEngine.SceneManagement.Scene scene) => false;
    public bool TrySpawn(GameObject prefab, Vector3 position, Transform target, UnityEngine.SceneManagement.Scene scene, Action<GameObject> spawned) => false;
    public void ReturnEnemy(GameObject enemy) => enemy.SetActive(false);
}
