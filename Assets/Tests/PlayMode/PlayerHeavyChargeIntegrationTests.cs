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
    private void CreatePlayer(bool configured = true)
    {
        var go = Create("heavy integration player"); go.SetActive(false);
        data = go.AddComponent(Runtime("PlayerData")); Set(data, "maxMP", 100f);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<Animator>().runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("PlayerAnimator/Player");
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
}
