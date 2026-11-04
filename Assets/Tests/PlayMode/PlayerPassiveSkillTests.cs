using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public sealed class PlayerPassiveSkillTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private Component data;
    private object combat;
    private object loadout;
    private GameObject prefab;
    private Camera camera;
    private InputActionAsset inputAsset;
    private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object target, string name, params object[] args) => target.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static void Set(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, $"Missing configuration: {name}");
        field.SetValue(target, value);
    }
    private static T Read<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
    private GameObject Create(string name)
    {
        var go = new GameObject(name); objects.Add(go); return go;
    }
    [SetUp]
    public void Setup()
    {
        Time.timeScale = 1;
        var owner = Create("passive player"); data = owner.AddComponent(Runtime("PlayerData"));
        Set(data, "maxMP", 100f); Call(data, "InitMP");
        loadout = Read<object>(data, "Loadout");
        prefab = Create("passive prefab"); prefab.SetActive(false); prefab.AddComponent(Runtime("Bullet"));
        combat = Activator.CreateInstance(Runtime("PlayerCombat"), owner.transform, data, "Enemy");
        camera = Create("passive camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.orthographic = true; camera.orthographicSize = 10; camera.transform.position = new Vector3(0, 0, -10);
        Enemy(Vector2.right * 4);
    }
    private GameObject Enemy(Vector2 position)
    {
        var go = Create("passive enemy"); go.tag = "Enemy"; go.transform.position = position;
        go.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms(); return go;
    }
    private object Add(int id = 10001, float interval = 3, string key = "flame")
    {
        var skill = Activator.CreateInstance(Runtime("Data.SkillData")); Set(skill, "DataId", id);
        Set(skill, "PassiveConfigured", true); Set(skill, "PassivePrefabKey", key);
        Set(skill, "PassiveDamage", 6f); Set(skill, "PassiveInterval", interval);
        Set(skill, "PassiveSpeed", 3f); Set(skill, "PassiveRange", 6f);
        Assert.That(Call(loadout, "TryAddAttackSkill", skill), Is.True);
        Call(combat, "BindProjectile", key, prefab); return skill;
    }
    private int Count => Object.FindObjectsByType(Runtime("Bullet")).Length;
    private Component LastBullet => (Component)Object.FindObjectsByType(Runtime("Bullet")).Last();
    private void Tick(float delta) => Call(combat, "Tick", delta);
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        foreach (var go in objects) if (go != null) Object.Destroy(go);
        Object.Destroy(GameObject.Find("@Managers"));
        if (inputAsset != null) Object.Destroy(inputAsset);
        yield return null; objects.Clear();
    }
    [Test]
    public void EmptyLoadoutDoesNotFireLegacyBullet() { Tick(.1f); Assert.That(Count, Is.Zero); }
    [Test]
    public void OwnedUnequippedSkillFiresWithoutMPAndUsesIndependentPassiveValues()
    {
        Add(); Tick(.1f); Assert.That(Count, Is.EqualTo(1));
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100));
        Tick(2.99f); Assert.That(Count, Is.EqualTo(1)); Tick(.02f); Assert.That(Count, Is.EqualTo(2));
    }
    [Test]
    public void MultipleSkillsHaveIndependentTimersAndRemovalStopsOnlyRemovedSkill()
    {
        Add(); Add(10002, 1); Tick(.01f); Assert.That(Count, Is.EqualTo(2));
        Tick(1.01f); Assert.That(Count, Is.EqualTo(3));
        Call(loadout, "RemoveSkill", 10002); Tick(1f); Assert.That(Count, Is.EqualTo(3));
        Tick(1f); Assert.That(Count, Is.EqualTo(4));
    }
    [Test]
    public void MetadataOnlyAndMissingProjectileDoNotFire()
    {
        var skill = Add(); Set(skill, "PassiveConfigured", false); Tick(.1f); Assert.That(Count, Is.Zero);
        Set(skill, "PassiveConfigured", true); Set(skill, "PassivePrefabKey", "not preloaded"); Tick(.1f); Assert.That(Count, Is.Zero);
    }
    [TestCase("PassiveInterval", 0f)]
    [TestCase("PassiveInterval", float.NaN)]
    [TestCase("PassiveDamage", -1f)]
    [TestCase("PassiveSpeed", float.PositiveInfinity)]
    [TestCase("PassiveRange", -1f)]
    public void InvalidConfigurationDoesNotSpawn(string field, float value)
    { var skill = Add(); Set(skill, field, value); Tick(.1f); Assert.That(Count, Is.Zero); }
    [Test]
    public void PauseAndZeroDeltaDoNotFireOrAdvanceCooldown()
    {
        Add(); Tick(0); Assert.That(Count, Is.Zero); Tick(.1f);
        Time.timeScale = 0; Tick(10); Assert.That(Count, Is.EqualTo(1));
        Time.timeScale = 1; Tick(2); Assert.That(Count, Is.EqualTo(1)); Tick(1.01f); Assert.That(Count, Is.EqualTo(2));
    }
    [Test]
    public void OutsideRangeOrViewportDoesNotConsumeReadyShot()
    {
        Add(); objects.First(o => o.name == "passive enemy").transform.position = Vector2.right * 7;
        Physics2D.SyncTransforms(); Tick(.1f); Assert.That(Count, Is.Zero);
        camera.orthographicSize = 1; objects.First(o => o.name == "passive enemy").transform.position = Vector2.up * 4;
        Physics2D.SyncTransforms(); Tick(.1f); Assert.That(Count, Is.Zero);
        camera.orthographicSize = 10; Tick(.01f); Assert.That(Count, Is.EqualTo(1));
    }
    [UnityTest]
    public IEnumerator NextShotRetargetsButPreviousShotKeepsFlyingStraight()
    {
        Add(); Tick(.01f); Component first = LastBullet;
        objects.First(o => o.name == "passive enemy").transform.position = Vector2.right * 5;
        Enemy(Vector2.up * 2); Tick(3.01f);
        var bullets = Object.FindObjectsByType(Runtime("Bullet")).Cast<Component>().ToArray();
        Component second = bullets.Single(b => b != first);
        yield return null; yield return null;
        Assert.That(first.transform.position.x, Is.GreaterThan(0)); Assert.That(first.transform.position.y, Is.EqualTo(0).Within(.001f));
        Assert.That(second.transform.position.y, Is.GreaterThan(0)); Assert.That(second.transform.position.x, Is.EqualTo(0).Within(.001f));
    }
    [Test]
    public void ResetClearsCooldownAndDeadPlayerDoesNotFire()
    {
        Add(); Tick(.1f); Call(combat, "Reset"); Tick(.1f); Assert.That(Count, Is.EqualTo(2));
        Set(data, "currentHealth", 0f); Tick(4); Assert.That(Count, Is.EqualTo(2));
    }

    private Component ConnectRealPlayer(bool prolongedDefense = false)
    {
        var owner = data.gameObject; owner.SetActive(false);
        owner.AddComponent<SpriteRenderer>();
        owner.AddComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("PlayerAnimator/Player");
        inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value); map.AddAction("Dash", InputActionType.Button); map.AddAction("Parry", InputActionType.Button);
        inputAsset.AddActionMap(map);
        var player = owner.AddComponent(Runtime("Player")); Set(player, "inputActions", inputAsset);
        if (prolongedDefense) Set(player, "parryStartup", 10f);
        var binding = Activator.CreateInstance(Runtime("PlayerProjectileBinding"));
        Set(binding, "Key", "flame"); Set(binding, "Prefab", prefab);
        var bindings = Array.CreateInstance(binding.GetType(), 1); bindings.SetValue(binding, 0);
        Set(player, "passiveProjectiles", bindings);
        owner.SetActive(true); return player;
    }
    [UnityTest]
    public IEnumerator RealPlayerUsesRedPrefabAndDealsSixDamageWithoutMPOrEquippedActive()
    {
        objects.First(o => o.name == "passive enemy").SetActive(false);
        prefab = Resources.Load<GameObject>("SkillProjectiles/FlameBullet"); Assert.That(prefab, Is.Not.Null);
        Add(); ConnectRealPlayer();
        var enemy = Create("passive damage target"); enemy.SetActive(false); enemy.tag = "Enemy";
        enemy.transform.position = Vector3.right; enemy.AddComponent<CircleCollider2D>().radius = .2f;
        var health = enemy.AddComponent(Runtime("EnemyHealth")); Set(health, "maxHealth", 20f);
        enemy.SetActive(true); Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.6f);
        Assert.That(Read<float>(health, "CurrentHealth"), Is.EqualTo(14f));
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100f));
        var pool = Runtime("Managers").GetProperty("Pool").GetValue(null);
        Assert.That(Call(pool, "GetInactiveCount", prefab), Is.EqualTo(1));
        Assert.That(prefab.GetComponent<SpriteRenderer>().color.r, Is.GreaterThan(.9f));
        Assert.That(prefab.GetComponent<SpriteRenderer>().color.g, Is.LessThan(.3f));
    }
    [UnityTest]
    public IEnumerator RealPlayerDisableResetsTimerWhilePausePreservesIt()
    {
        Add(); var player = ConnectRealPlayer(); yield return null; yield return null;
        Assert.That(Count, Is.EqualTo(1));
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(.2f); Assert.That(Count, Is.EqualTo(1));
        Time.timeScale = 1; yield return null; Assert.That(Count, Is.EqualTo(1));
        player.gameObject.SetActive(false); player.gameObject.SetActive(true);
        yield return null; yield return null; Assert.That(Count, Is.EqualTo(2));
        Call(loadout, "RemoveSkill", 10001); yield return null;
        Assert.That(Count, Is.EqualTo(2));
    }
    [UnityTest]
    public IEnumerator DefenseFreezesPassiveWhileLightInputLockStillAllowsIt()
    {
        Add(interval: .2f); var player = ConnectRealPlayer(prolongedDefense: true);
        yield return null; yield return null; Assert.That(Count, Is.EqualTo(1));
        Assert.That(Call(player, "TryParry"), Is.True);
        yield return new WaitForSeconds(.35f); Assert.That(Count, Is.EqualTo(1));
        Assert.That(Call(player, "TryDash", Vector2.right), Is.True);
        yield return new WaitForSeconds(.25f); Assert.That(Count, Is.GreaterThan(1));
        object[] args = { 0f, 0 }; Assert.That(Call(player, "TryBeginLightAttack", args), Is.True);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
        int before = Count; yield return new WaitForSeconds(.25f);
        Assert.That(Count, Is.GreaterThan(before));
    }
    [Test]
    public void MainSceneHasApprovedPassiveAndPreservesActiveConfiguration()
    {
        string scene = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "9.Scenes/MainTestScene.unity"));
        StringAssert.Contains("- Key: FlameBullet", scene);
        StringAssert.Contains("PassivePrefabKey: FlameBullet", scene);
        StringAssert.Contains("PassiveConfigured: 1", scene);
        StringAssert.Contains("PassiveDamage: 6", scene); StringAssert.Contains("PassiveInterval: 3", scene);
        StringAssert.Contains("PassiveSpeed: 3", scene); StringAssert.Contains("PassiveRange: 6", scene);
        StringAssert.Contains("MPCost: 10", scene); StringAssert.Contains("BaseDamage: 10", scene);
    }
}
