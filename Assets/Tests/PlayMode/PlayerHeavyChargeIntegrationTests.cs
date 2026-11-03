using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PlayerHeavyChargeIntegrationTests
{
    private readonly List<Object> objects = new List<Object>();
    private Component player, data, bridge;
    private object settings;
    private Keyboard keyboard;
    private InputSettings original;
    private readonly List<Camera> suspendedCameras = new List<Camera>();
    private static Type Runtime(string name)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
        Assert.That(type, Is.Not.Null, $"Missing charge integration: {name}"); return type;
    }
    private static object Call(object target, string name, params object[] args) => target.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static T Read<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
    private GameObject Create(string name) { var go = new GameObject(name); objects.Add(go); return go; }
    private Component mobile, joystick;
    private void CreatePlayer(bool configured = true, bool useMobile = false)
    {
        var go = Create("heavy integration player"); go.SetActive(false);
        data = go.AddComponent(Runtime("PlayerData")); Set(data, "maxMP", 100f);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("PlayerAnimator/Player");
        if (useMobile)
        {
            var joystickObject = Create("raw aim joystick");
            joystickObject.SetActive(false);
            joystick = joystickObject.AddComponent(Runtime("VariableJoystick"));
            go.AddComponent<Rigidbody2D>().gravityScale = 0;
            mobile = go.AddComponent(Runtime("PlayerController2D"));
            Set(mobile, "variableJoystick", joystick);
        }
        var asset = ScriptableObject.CreateInstance<InputActionAsset>(); objects.Add(asset);
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value).AddCompositeBinding("2DVector").With("Right", "<Keyboard>/d").With("Up", "<Keyboard>/w");
        map.AddAction("Dash", InputActionType.Button, "<Keyboard>/space"); map.AddAction("Parry", InputActionType.Button, "<Keyboard>/e");
        asset.AddActionMap(map);
        var button = Create("C_Attack").AddComponent<Button>();
        settings = Activator.CreateInstance(Runtime("PlayerHeavyChargeSettings"));
        Set(settings, "Configured", configured); Set(settings, "MPCost", 10f);
        Set(settings, "StageTimes", new[] { 0f, .4f, .8f }); Set(settings, "StageBonuses", new[] { .1f, .1f, .5f });
        player = go.AddComponent(Runtime("Player")); Set(player, "inputActions", asset);
        Set(player, "heavyChargeSettings", settings); Set(player, "heavyAttackButton", button);
        go.SetActive(true); bridge = button.GetComponent(Runtime("PlayerHeavyAttackButton"));
    }
    [SetUp]
    public void Setup()
    {
        Time.timeScale = 1; original = InputSystem.settings;
        // 앞선 씬 테스트의 카메라가 남아도 이 픽스처의 조준/패시브 판정을 사용합니다.
        foreach (var camera in Object.FindObjectsByType<Camera>())
            if (camera.enabled) { suspendedCameras.Add(camera); camera.enabled = false; }
        var copy = Object.Instantiate(original); objects.Add(copy); InputSystem.settings = copy;
        copy.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        copy.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1; InputSystem.settings = original;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        foreach (var go in objects.OfType<GameObject>()) if (go != null) Object.Destroy(go);
        Object.Destroy(GameObject.Find("@Managers")); yield return null;
        foreach (var obj in objects.Where(o => !(o is GameObject))) if (obj != null) Object.Destroy(obj);
        objects.Clear(); yield return null;
        foreach (var camera in suspendedCameras) if (camera != null) camera.enabled = true;
        suspendedCameras.Clear();
    }
    private void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update(); Call(player, "Update"); }
    private bool Begin() => (bool)Call(player, "TryBeginHeavyCharge");
    private bool Charging => Read<bool>(player, "IsHeavyCharging");
    private int Id => Read<int>(player, "HeavyChargeId");
    private PointerEventData Pointer(int id = 7) => new PointerEventData(EventSystem.current)
        { pointerId = id, button = PointerEventData.InputButton.Left };
    [Test]
    public void CAttackStartsOnceAndMatchingPointerReleaseProducesResultOnlyOnce()
    {
        CreatePlayer(); var pointer = Pointer(); Call(bridge, "OnPointerDown", pointer); int id = Id;
        Assert.That(Charging, Is.True); Assert.That(Read<bool>(player, "CanMove"), Is.False);
        Call(bridge, "OnPointerDown", pointer); Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90));
        Call(bridge, "OnPointerUp", Pointer(8)); Assert.That(Charging, Is.True);
        Call(bridge, "OnPointerUp", pointer); Assert.That(Charging, Is.False);
        Assert.That(Read<int>(Read<object>(player, "LastHeavyCharge"), "ChargeId"), Is.EqualTo(id));
        Assert.That(Call(player, "TryReleaseHeavyCharge", id), Is.False);
    }
    [UnityTest]
    public IEnumerator KeyboardQChargesWhileMovementBecomesAimOnly()
    {
        CreatePlayer(); yield return null; Keys(Key.Q, Key.D); Vector3 origin = player.transform.position;
        Assert.That(Charging, Is.True); yield return new WaitForSeconds(.5f);
        Assert.That(player.transform.position, Is.EqualTo(origin)); Assert.That(Read<int>(player, "HeavyChargeStage"), Is.EqualTo(2));
        var arrow = player.GetComponentInChildren<LineRenderer>(true); Assert.That(arrow != null && arrow.enabled, Is.True);
        Keys(); Assert.That(Charging, Is.False); Assert.That(arrow.enabled, Is.False);
        Assert.That(Read<Vector2>(Read<object>(player, "LastHeavyCharge"), "Direction"), Is.EqualTo(Vector2.right));
    }
    [UnityTest]
    public IEnumerator PauseFreezesProgressAndReleaseWhilePausedCancelsWithoutQueuedAttack()
    {
        CreatePlayer(); Call(bridge, "OnPointerDown", Pointer()); yield return new WaitForSeconds(.15f);
        float progress = Read<float>(player, "HeavyChargeProgress"); Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.25f); Assert.That(Read<float>(player, "HeavyChargeProgress"), Is.EqualTo(progress));
        Call(bridge, "OnPointerUp", Pointer()); Assert.That(Charging, Is.False); Time.timeScale = 1;
        yield return null; Assert.That(Read<int>(Read<object>(player, "LastHeavyCharge"), "ChargeId"), Is.Zero);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90));
    }
    [Test]
    public void SuccessfulDefenseCancelsButFailedDashKeepsCharge()
    {
        CreatePlayer(); Begin(); Set(data, "dashMPCost", 95f);
        Assert.That(Call(player, "TryDash", Vector2.right), Is.False); Assert.That(Charging, Is.True);
        Assert.That(Call(player, "TryParry"), Is.True); Assert.That(Charging, Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90));
    }
    [Test]
    public void SuccessfulDashCancelsWithoutRefundingChargeMP()
    {
        CreatePlayer(); Begin(); Set(data, "dashMPCost", 5f);
        Assert.That(Call(player, "TryDash", Vector2.right), Is.True); Assert.That(Charging, Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(85));
    }
    [TestCase(false)]
    [TestCase(true)]
    public void ActualHitCancelsUnlessSuperArmorButAlwaysDealsDamage(bool armor)
    {
        CreatePlayer(); Set(settings, "SuperArmor", armor); Begin(); Call(player, "TakeDamage", 1f, null);
        Assert.That(Charging, Is.EqualTo(armor)); Assert.That(data.GetType().GetField("currentHealth").GetValue(data), Is.EqualTo(99f));
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90));
    }
    [Test]
    public void DisableAndFocusLossClearChargeAndDoNotRefundStartedCost()
    {
        CreatePlayer(); Begin(); Call(player, "OnApplicationFocus", false); Assert.That(Charging, Is.False);
        Begin(); player.gameObject.SetActive(false); Assert.That(Charging, Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(80));
    }
    [Test]
    public void DisableDuringMPNotificationCancelsUnstartedChargeAndRefunds()
    {
        CreatePlayer(); data.GetType().GetEvent("MPChanged").AddEventHandler(data,
            (Action<float, float>)((current, _) => { if (current == 90) player.gameObject.SetActive(false); }));
        Assert.That(Begin(), Is.False); Assert.That(Charging, Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100));
    }
    [UnityTest]
    public IEnumerator LateOldPointerReleaseCannotEndNewCharge()
    {
        CreatePlayer(); Call(bridge, "OnPointerDown", Pointer()); int old = Id;
        Call(player, "TryParry"); yield return new WaitForSeconds(.6f);
        Assert.That(Begin(), Is.True); int next = Id; Assert.That(next, Is.Not.EqualTo(old));
        Call(bridge, "OnPointerUp", Pointer()); Assert.That(Charging, Is.True); Assert.That(Id, Is.EqualTo(next));
    }
    [Test]
    public void LightInputLockAndMissingConfigurationRejectCharge()
    {
        CreatePlayer(configured: false); Assert.That(Begin(), Is.False);
        Set(settings, "Configured", true); object[] args = { 0f, 0 };
        Assert.That(Call(player, "TryBeginLightAttack", args), Is.True); Assert.That(Begin(), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100));
    }

    [UnityTest]
    public IEnumerator KeyboardReleaseDuringPauseCancelsImmediately()
    {
        CreatePlayer(); yield return null; Keys(Key.Q);
        Assert.That(Charging, Is.True); Time.timeScale = 0; Keys();
        Assert.That(Charging, Is.False);
        Assert.That(Read<int>(Read<object>(player, "LastHeavyCharge"), "ChargeId"), Is.Zero);
    }

    [UnityTest]
    public IEnumerator MobileChargeStopsPhysicsButRawJoystickStillChangesReleaseDirection()
    {
        CreatePlayer(useMobile: true); yield return null;
        Runtime("Joystick").GetField("input", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joystick, Vector2.right);
        Call(mobile, "FixedUpdate");
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.GreaterThan(0));
        Assert.That(Begin(), Is.True); Vector3 origin = player.transform.position;
        Runtime("Joystick").GetField("input", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(joystick, Vector2.up);
        yield return new WaitForFixedUpdate(); yield return null;
        Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(player.transform.position, Is.EqualTo(origin));
        Call(player, "TryReleaseHeavyCharge", Id);
        Assert.That(Read<Vector2>(Read<object>(player, "LastHeavyCharge"), "Direction"), Is.EqualTo(Vector2.up));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DeathAlwaysClearsChargeAndIndicator(bool armor)
    {
        CreatePlayer(); Set(settings, "SuperArmor", armor); Begin();
        var arrow = player.GetComponentInChildren<LineRenderer>(true);
        Call(player, "TakeDamage", 1000f, null);
        Assert.That(Charging, Is.False); Assert.That(player.gameObject.activeSelf, Is.False);
        Assert.That(arrow != null && !arrow.enabled, Is.True);
    }

    [UnityTest]
    public IEnumerator ChargeBlocksPassiveAndLightAttackThenPassiveResumesOnRelease()
    {
        CreatePlayer(); yield return null;
        var camera = Create("charge camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.orthographic = true; camera.orthographicSize = 10; camera.transform.position = new Vector3(0, 0, -10);
        var enemy = Create("charge target"); enemy.tag = "Enemy"; enemy.transform.position = Vector2.right * 3;
        enemy.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
        var prefab = Create("charge passive prefab"); prefab.SetActive(false); prefab.AddComponent(Runtime("Bullet"));
        var skill = Activator.CreateInstance(Runtime("Data.SkillData"));
        Set(skill, "DataId", 10001); Set(skill, "PassiveConfigured", true); Set(skill, "PassivePrefabKey", "charge-test");
        Set(skill, "PassiveDamage", 6f); Set(skill, "PassiveInterval", .1f); Set(skill, "PassiveSpeed", 0f); Set(skill, "PassiveRange", 6f);
        Call(Read<object>(data, "Loadout"), "TryAddAttackSkill", skill);
        var combat = player.GetType().GetField("combat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
        Call(combat, "BindProjectile", "charge-test", prefab);
        Assert.That(Begin(), Is.True); Assert.That(Call(player, "TryUseLightSkill"), Is.False);
        yield return new WaitForSeconds(.2f);
        Assert.That(Object.FindObjectsByType(Runtime("Bullet")).Count(b => b.name == prefab.name), Is.Zero);
        Call(player, "TryReleaseHeavyCharge", Id); yield return null;
        Assert.That(Object.FindObjectsByType(Runtime("Bullet")).Count(b => b.name == prefab.name), Is.GreaterThan(0));
    }

    [UnityTest]
    public IEnumerator DefenseOnKeyboardReleaseFrameCancelsInsteadOfReleasing()
    {
        CreatePlayer(); yield return null; Keys(Key.Q); Keys(Key.E);
        Assert.That(Charging, Is.False);
        Assert.That(Read<int>(Read<object>(player, "LastHeavyCharge"), "ChargeId"), Is.Zero);
    }

    [Test]
    public void RepeatedChargesReuseArrowAndDisableHidesIt()
    {
        CreatePlayer(); Begin(); var arrow = player.GetComponentInChildren<LineRenderer>(true);
        Assert.That(arrow != null && arrow.enabled, Is.True);
        Call(player, "TryReleaseHeavyCharge", Id); Begin();
        Assert.That(player.GetComponentsInChildren<LineRenderer>(true).Length, Is.EqualTo(1));
        player.gameObject.SetActive(false); Assert.That(arrow.enabled, Is.False);
    }

    [Test]
    public void NewTouchAfterFocusLossCanStartWithoutMissingOldPointerUp()
    {
        CreatePlayer(); Call(bridge, "OnPointerDown", Pointer()); Call(player, "OnApplicationFocus", false);
        Assert.That(Charging, Is.False);
        Call(bridge, "OnPointerDown", Pointer(8)); Assert.That(Charging, Is.True);
        Call(bridge, "OnPointerUp", Pointer()); Assert.That(Charging, Is.True);
        Call(bridge, "OnPointerUp", Pointer(8)); Assert.That(Charging, Is.False);
    }

    [UnityTest]
    public IEnumerator NeutralMobileJoystickRetainsLastUnquantizedAim()
    {
        CreatePlayer(useMobile: true); yield return null;
        var input = Runtime("Joystick").GetField("input", BindingFlags.NonPublic | BindingFlags.Instance);
        input.SetValue(joystick, new Vector2(.6f, .8f)); Assert.That(Begin(), Is.True);
        yield return null; input.SetValue(joystick, Vector2.zero);
        Call(player, "TryReleaseHeavyCharge", Id);
        Vector2 direction = Read<Vector2>(Read<object>(player, "LastHeavyCharge"), "Direction");
        Assert.That(direction.x, Is.EqualTo(.6f).Within(.001f));
        Assert.That(direction.y, Is.EqualTo(.8f).Within(.001f));
    }
}
