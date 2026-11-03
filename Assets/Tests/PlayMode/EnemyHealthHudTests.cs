using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class EnemyHealthHudTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private Component health;
    private Slider bar;
    private EventSystem events;
    private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object target, string method, params object[] args) => target.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == method && m.GetParameters().Length == args.Length
            && (args.Length == 0 || args[0] == null || m.GetParameters()[0].ParameterType.IsInstanceOfType(args[0])))
        .Invoke(target, args);
    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private GameObject Create(string name)
    { var go = new GameObject(name); objects.Add(go); return go; }
    private void CreateBoss(float min = 0, float max = 1, bool hiddenBar = false)
    {
        events = Create("boss HUD event system").AddComponent<EventSystem>();
        bar = Create("Boss_HP").AddComponent<Slider>();
        bar.minValue = min; bar.maxValue = max;
        var fill = Create("boss HP fill").AddComponent<RectTransform>(); fill.SetParent(bar.transform, false); bar.fillRect = fill;
        if (hiddenBar) bar.gameObject.SetActive(false);
        var boss = Create("boss with EnemyHealth"); boss.SetActive(false);
        health = boss.AddComponent(Runtime("EnemyHealth"));
        Set(health, "maxHealth", 100f); Set(health, "healthSlider", bar);
        boss.SetActive(true); Call(health, "Start");
    }
    private float HP => (float)health.GetType().GetProperty("CurrentHealth").GetValue(health);
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (var go in objects) if (go != null) Object.Destroy(go);
        objects.Clear(); yield return null;
    }
    [Test]
    public void SelectedBossHPBarIgnoresLeftAndRightWithoutChangingHealth()
    {
        CreateBoss(); Call(health, "TakeDamage", 50f);
        events.SetSelectedGameObject(bar.gameObject);
        bar.OnMove(new AxisEventData(events) { moveDir = MoveDirection.Left });
        Assert.That(bar.normalizedValue, Is.EqualTo(.5f).Within(.001f), "Left movement must not edit the boss HP display.");
        bar.OnMove(new AxisEventData(events) { moveDir = MoveDirection.Right });
        Assert.That(bar.normalizedValue, Is.EqualTo(.5f).Within(.001f)); Assert.That(HP, Is.EqualTo(50f));
    }
    [Test]
    public void BossHPIsDisplayOnlyAndKeepsOriginalColors()
    {
        CreateBoss();
        Assert.That(bar.interactable, Is.False);
        Assert.That(bar.navigation.mode, Is.EqualTo(Navigation.Mode.None));
        Assert.That(bar.transition, Is.EqualTo(Selectable.Transition.None));
    }
    [Test]
    public void PointerAndDragCannotEditBossHP()
    {
        CreateBoss(); Call(health, "TakeDamage", 50f);
        var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left, position = new Vector2(1000, 0) };
        bar.OnPointerDown(pointer); bar.OnDrag(pointer);
        Assert.That(bar.normalizedValue, Is.EqualTo(.5f).Within(.001f)); Assert.That(HP, Is.EqualTo(50));
    }
    [Test]
    public void DamageUpdatesCustomRangeWithoutSendingUserValueChangedEvent()
    {
        CreateBoss(10, 20); int notifications = 0; bar.onValueChanged.AddListener(_ => notifications++);
        Assert.That(bar.value, Is.EqualTo(20)); Call(health, "TakeDamage", 25f);
        Assert.That(bar.value, Is.EqualTo(17.5f)); Assert.That(HP, Is.EqualTo(75));
        Assert.That(notifications, Is.Zero);
    }
    [Test]
    public void PreparingPooledBossRestoresFullBarAndDisplayOnlySettings()
    {
        CreateBoss(); Call(health, "TakeDamage", 40f);
        bar.interactable = true; bar.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        Call(health, "PrepareForSpawn", (object)null);
        Assert.That(HP, Is.EqualTo(100)); Assert.That(bar.normalizedValue, Is.EqualTo(1));
        Assert.That(bar.interactable, Is.False); Assert.That(bar.navigation.mode, Is.EqualTo(Navigation.Mode.None));
    }
    [Test]
    public void InitiallyHiddenBossBarIsSafeWhenShown()
    {
        CreateBoss(hiddenBar: true); Call(health, "TakeDamage", 30f); bar.gameObject.SetActive(true);
        events.SetSelectedGameObject(bar.gameObject);
        bar.OnMove(new AxisEventData(events) { moveDir = MoveDirection.Left });
        Assert.That(bar.normalizedValue, Is.EqualTo(.7f).Within(.001f));
    }
    [Test]
    public void EnemyWithoutHealthBarStillTakesDamage()
    {
        var enemy = Create("enemy without HUD"); health = enemy.AddComponent(Runtime("EnemyHealth"));
        Call(health, "TakeDamage", 1f); Assert.That(HP, Is.EqualTo(2f));
    }
}
