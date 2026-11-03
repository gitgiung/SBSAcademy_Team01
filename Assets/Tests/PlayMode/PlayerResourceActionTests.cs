using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class PlayerResourceActionTests
{
    private readonly List<Object> objects = new List<Object>();
    private Keyboard keyboard;
    private InputSettings originalInputSettings;
    private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object instance, string name, params object[] args) => instance.GetType()
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(instance, args);
    private static void Set(object instance, string name, object value) => instance.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(instance, value);
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name).GetValue(instance);
    private static object Field(object instance, string name) => instance.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);
    private GameObject Create(string name)
    {
        var go = new GameObject(name);
        objects.Add(go);
        return go;
    }

    private Component NewData()
    {
        var data = Create("MP test data").AddComponent(Runtime("PlayerData"));
        Set(data, "maxMP", 10f);
        Call(data, "InitMP");
        return data;
    }

    private Component NewPlayer(bool mobile = false)
    {
        var go = Create("MP test player");
        go.SetActive(false);
        var data = go.AddComponent(Runtime("PlayerData"));
        Set(data, "maxMP", 10f);
        Set(data, "dashMPCost", 3f);
        Set(data, "parryMPRecovery", 2f);
        if (mobile)
        {
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            go.AddComponent(Runtime("PlayerController2D"));
        }
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        objects.Add(asset);
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value).AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        map.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
        map.AddAction("Parry", InputActionType.Button, "<Keyboard>/q");
        asset.AddActionMap(map);
        var player = go.AddComponent(Runtime("Player"));
        Set(player, "inputActions", asset);
        go.SetActive(true);
        return player;
    }

    private static bool Begin(Component player, float cost, out int actionId)
    {
        object[] args = { cost, 0 };
        bool result = (bool)Call(player, "TryBeginLightAttack", args);
        actionId = (int)args[1];
        return result;
    }

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        originalInputSettings = InputSystem.settings;
        var settings = Object.Instantiate(originalInputSettings);
        objects.Add(settings);
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings = settings;
        keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        InputSystem.settings = originalInputSettings;
        foreach (var go in objects.OfType<GameObject>()) if (go != null) Object.Destroy(go);
        yield return null;
        foreach (var obj in objects.Where(o => !(o is GameObject))) if (obj != null) Object.Destroy(obj);
        objects.Clear();
        InputSystem.RemoveDevice(keyboard);
        yield return null;
    }

    [Test]
    public void MPSpendsAndRestoresWithinBoundsWithNotifications()
    {
        var data = NewData();
        var values = new List<float>();
        data.GetType().GetEvent("MPChanged").AddEventHandler(data,
            (Action<float, float>)((current, maximum) => values.Add(current)));
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(10f));
        Assert.That(Call(data, "TrySpendMP", 4f), Is.True);
        Assert.That(Call(data, "RestoreMP", 20f), Is.EqualTo(4f));
        Assert.That(values, Is.EqualTo(new[] { 6f, 10f }));
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void InvalidCostDoesNotChangeMP(float cost)
    {
        var data = NewData();
        Assert.That(Call(data, "TrySpendMP", cost), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(10f));
    }

    [Test]
    public void InsufficientMPRejectsWithoutSpendingAndPublishesFeedback()
    {
        var data = NewData();
        int rejected = 0;
        data.GetType().GetEvent("MPSpendRejected").AddEventHandler(data, (Action<float>)(_ => rejected++));
        Assert.That(Call(data, "TrySpendMP", 11f), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(10f));
        Assert.That(rejected, Is.EqualTo(1));
    }

    [Test]
    public void RecoveryUsesConfiguredRateAndIgnoresZeroOrInvalidDelta()
    {
        var data = NewData();
        Call(data, "TrySpendMP", 8f);
        Set(data, "mpRecoveryPerSecond", 2f);
        Call(data, "TickMPRecovery", 2f);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(6f));
        foreach (float dt in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            Call(data, "TickMPRecovery", dt);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(6f));
    }

    [Test]
    public void LightAttackBlocksOtherActionsWithoutFurtherSpending()
    {
        var player = NewPlayer();
        var data = Field(player, "data");
        Assert.That(Begin(player, 4f, out int actionId), Is.True);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
        Assert.That(Read<bool>(player, "CanAcceptGameplayInput"), Is.False);
        Assert.That(Begin(player, 1f, out _), Is.False);
        Assert.That(Call(player, "TryDash", Vector2.right), Is.False);
        Assert.That(Call(player, "TryParry"), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(6f));
        Assert.That(Call(player, "CompleteLightAttack", actionId), Is.True);
        Assert.That(Read<bool>(player, "CanAcceptGameplayInput"), Is.True);
    }

    [Test]
    public void FailedLightAttackDoesNotLockOrSpend()
    {
        var player = NewPlayer();
        Assert.That(Begin(player, 11f, out _), Is.False);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
        Assert.That(Read<float>(Field(player, "data"), "CurrentMP"), Is.EqualTo(10f));
    }

    [Test]
    public void OldAnimationCompletionCannotUnlockNewAttack()
    {
        var player = NewPlayer();
        Begin(player, 1f, out int first);
        Call(player, "CompleteLightAttack", first);
        Begin(player, 1f, out int second);
        Assert.That(Call(player, "CompleteLightAttack", first), Is.False);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
        Assert.That(Call(player, "CompleteLightAttack", second), Is.True);
    }

    [Test]
    public void DisableClearsLockAndInvalidatesOldCompletion()
    {
        var player = NewPlayer();
        Begin(player, 1f, out int first);
        player.gameObject.SetActive(false);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
        player.gameObject.SetActive(true);
        Begin(player, 1f, out int second);
        Assert.That(Call(player, "CompleteLightAttack", first), Is.False);
        Assert.That(Call(player, "CompleteLightAttack", second), Is.True);
    }

    [Test]
    public void DeathClearsLockAndRejectsNewActions()
    {
        var player = NewPlayer();
        Begin(player, 1f, out _);
        Call(player, "TakeDamage", 1000f, null);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
        Assert.That(Begin(player, 0f, out _), Is.False);
    }

    [Test]
    public void PausedPlayerDoesNotRegenerateOrStartActions()
    {
        var player = NewPlayer();
        var data = Field(player, "data");
        Call(data, "TrySpendMP", 5f);
        Set(data, "mpRecoveryPerSecond", 10f);
        Time.timeScale = 0f;
        Call(player, "Update");
        Assert.That(Begin(player, 1f, out _), Is.False);
        Assert.That(Call(player, "TryDash", Vector2.right), Is.False);
        Assert.That(Call(player, "TryParry"), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(5f));
    }

    [UnityTest]
    public IEnumerator PCMovementStopsDuringLockAndResumesCurrentHeldInput()
    {
        var player = NewPlayer();
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        InputSystem.Update();
        Assert.That(((InputAction)Field(player, "moveInput")).ReadValue<Vector2>().x, Is.GreaterThan(0f),
            "The movement input fixture must deliver the held key before testing the lock.");
        Assert.That(Begin(player, 0f, out int actionId), Is.True);
        Vector3 start = player.transform.position;
        Call(player, "Update");
        Assert.That(player.transform.position, Is.EqualTo(start));
        Call(player, "CompleteLightAttack", actionId);
        yield return null;
        Call(player, "Update");
        Assert.That(player.transform.position.x, Is.GreaterThan(start.x));
    }

    [UnityTest]
    public IEnumerator JoystickVelocityStopsImmediatelyAndResumesCurrentDirection()
    {
        var player = NewPlayer(mobile: true);
        var controller = player.GetComponent(Runtime("PlayerController2D"));
        var stickObject = Create("MP test joystick");
        stickObject.SetActive(false); // input fixture does not need joystick UI initialization.
        var stick = stickObject.AddComponent(Runtime("VariableJoystick"));
        Runtime("Joystick").GetField("input", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(stick, Vector2.right);
        Set(controller, "variableJoystick", stick);
        yield return null;
        Call(controller, "FixedUpdate");
        var body = player.GetComponent<Rigidbody2D>();
        Assert.That(body.linearVelocity.x, Is.GreaterThan(0f));
        Assert.That(Begin(player, 0f, out int actionId), Is.True);
        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        Call(controller, "FixedUpdate");
        Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        Call(player, "CompleteLightAttack", actionId);
        Call(controller, "FixedUpdate");
        Assert.That(body.linearVelocity.x, Is.GreaterThan(0f));
    }

    [Test]
    public void DashChargesOnceAndRejectsInvalidOrBusyAttemptsWithoutCost()
    {
        var player = NewPlayer();
        var data = Field(player, "data");
        Assert.That(Call(player, "TryDash", Vector2.zero), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(10f));
        Assert.That(Call(player, "TryDash", Vector2.right), Is.True);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(7f));
        Assert.That(Call(player, "TryDash", Vector2.right), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(7f));
    }

    [Test]
    public void InsufficientMPDoesNotStartDash()
    {
        var player = NewPlayer();
        Call(Field(player, "data"), "TrySpendMP", 9f);
        Assert.That(Call(player, "TryDash", Vector2.right), Is.False);
        Assert.That(Read<bool>(player, "IsDashing"), Is.False);
        Assert.That(Read<float>(Field(player, "data"), "CurrentMP"), Is.EqualTo(1f));
    }

    [Test]
    public void SuccessfulParryRecoversMPOnceForMultipleContacts()
    {
        var player = NewPlayer();
        var data = Field(player, "data");
        Call(data, "TrySpendMP", 5f);
        Assert.That(Call(player, "TryParry"), Is.True);
        var defense = Field(player, "defense");
        Call(defense, "NotifyIncomingAttackBlocked");
        Call(defense, "NotifyIncomingAttackBlocked");
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(7f));
    }

    [Test]
    public void MPNotificationCannotReenterDashOrBeginLightAttack()
    {
        var player = NewPlayer();
        var data = Field(player, "data");
        bool nestedDash = true;
        bool nestedLight = true;
        int notifications = 0;
        data.GetType().GetEvent("MPChanged").AddEventHandler(data, (Action<float, float>)((current, maximum) =>
        {
            if (++notifications != 1) return;
            nestedDash = (bool)Call(player, "TryDash", Vector2.right);
            nestedLight = Begin(player, 0f, out _);
        }));
        Assert.That(Call(player, "TryDash", Vector2.right), Is.True);
        Assert.That(nestedDash, Is.False);
        Assert.That(nestedLight, Is.False);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(7f));
    }

    [Test]
    public void DisableDuringMPNotificationCancelsUnstartedDashWithoutCost()
    {
        var player = NewPlayer();
        var data = Field(player, "data");
        data.GetType().GetEvent("MPChanged").AddEventHandler(data,
            (Action<float, float>)((_, __) => player.gameObject.SetActive(false)));
        Assert.That(Call(player, "TryDash", Vector2.right), Is.False);
        Assert.That(Read<bool>(player, "IsDashing"), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(10f));
    }
}
