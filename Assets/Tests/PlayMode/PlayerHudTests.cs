using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PlayerHudTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private Component data;
    private object view;
    private Slider hp;
    private Slider mp;
    private Slider exp;
    private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object instance, string name, params object[] args) => instance.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(instance, args);
    private static void Set(object instance, string name, object value) => instance.GetType()
        .GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
    private GameObject Create(string name)
    {
        var go = new GameObject(name);
        objects.Add(go);
        return go;
    }
    private Slider NewBar(string name) => Create(name).AddComponent<Slider>();

    [SetUp]
    public void SetUp()
    {
        var player = Create("HUD test player");
        data = player.AddComponent(Runtime("PlayerData"));
        Set(data, "maxMP", 10f);
        Call(data, "InitMP");
        hp = NewBar("HUD HP");
        mp = NewBar("HUD MP");
        exp = NewBar("HUD EXP");
        view = Activator.CreateInstance(Runtime("PlayerView"), player.transform, null, hp, data, null);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Call(view, "Dispose");
        foreach (var go in objects) if (go != null) Object.Destroy(go);
        objects.Clear();
        yield return null;
    }

    [Test]
    public void SelectedHPBarIgnoresLeftAndRightNavigation()
    {
        Call(data, "ApplyDamage", 50f);
        var eventSystem = Create("HUD event system").AddComponent<EventSystem>();
        eventSystem.SetSelectedGameObject(hp.gameObject);
        hp.OnMove(new AxisEventData(eventSystem) { moveDir = MoveDirection.Left });
        Assert.That(hp.normalizedValue, Is.EqualTo(0.5f).Within(0.001f),
            "Moving left must not decrease HP display while model HP is unchanged.");
        hp.OnMove(new AxisEventData(eventSystem) { moveDir = MoveDirection.Right });
        Assert.That(hp.normalizedValue, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(data.GetType().GetField("currentHealth").GetValue(data), Is.EqualTo(50f));
    }

    [Test]
    public void AllBarsAreDisplayOnlyWithoutDisabledColorTransitions()
    {
        Call(view, "BindResourceSliders", mp, exp);
        foreach (var slider in new[] { hp, mp, exp })
        {
            Assert.That(slider.interactable, Is.False);
            Assert.That(slider.navigation.mode, Is.EqualTo(Navigation.Mode.None));
            Assert.That(slider.transition, Is.EqualTo(Selectable.Transition.None));
        }
    }

    [Test]
    public void MPSpendAndRestoreUpdateViewFromModel()
    {
        Call(view, "BindResourceSliders", mp, exp);
        Assert.That(mp.normalizedValue, Is.EqualTo(1f));
        Call(data, "TrySpendMP", 4f);
        Assert.That(mp.normalizedValue, Is.EqualTo(0.6f).Within(0.001f));
        Call(data, "RestoreMP", 2f);
        Assert.That(mp.normalizedValue, Is.EqualTo(0.8f).Within(0.001f));
    }

    [Test]
    public void EXPUpdateAndOverflowAreDisplayedWithoutInventingLevelUp()
    {
        Call(view, "BindResourceSliders", mp, exp);
        Assert.That(Call(data, "SetEXP", 25f, 100f), Is.True);
        Assert.That(exp.normalizedValue, Is.EqualTo(0.25f).Within(0.001f));
        Call(data, "SetEXP", 125f, 100f);
        Assert.That(exp.normalizedValue, Is.EqualTo(1f));
        Assert.That(data.GetType().GetProperty("CurrentEXP").GetValue(data), Is.EqualTo(125f));
    }

    [TestCase(-1f, 100f)]
    [TestCase(float.NaN, 100f)]
    [TestCase(1f, float.PositiveInfinity)]
    public void InvalidEXPDoesNotChangeModelOrDisplay(float current, float maximum)
    {
        Call(view, "BindResourceSliders", mp, exp);
        Call(data, "SetEXP", 25f, 100f);
        Assert.That(Call(data, "SetEXP", current, maximum), Is.False);
        Assert.That(exp.normalizedValue, Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void ZeroMaximumDisplaysEmptyAndCustomSliderRangeIsRespected()
    {
        mp.minValue = 10f;
        mp.maxValue = 20f;
        Call(view, "BindResourceSliders", mp, exp);
        Call(data, "TrySpendMP", 4f);
        Assert.That(mp.value, Is.EqualTo(16f).Within(0.001f));
        Call(data, "SetEXP", 10f, 0f);
        Assert.That(exp.normalizedValue, Is.Zero);
        Set(data, "maxHealth", 0f);
        Call(view, "RefreshHealth");
        Assert.That(hp.normalizedValue, Is.Zero);
    }

    [Test]
    public void ModelUpdatesDoNotRaiseSliderValueCallbacks()
    {
        Call(view, "BindResourceSliders", mp, exp);
        int uiCallbacks = 0;
        foreach (var slider in new[] { hp, mp, exp }) slider.onValueChanged.AddListener(_ => uiCallbacks++);
        Call(data, "ApplyDamage", 10f);
        Call(data, "TrySpendMP", 1f);
        Call(data, "SetEXP", 25f, 100f);
        Assert.That(uiCallbacks, Is.Zero);
    }

    [Test]
    public void DisposedViewUnsubscribesAllThreeResourceEvents()
    {
        Call(view, "BindResourceSliders", mp, exp);
        Call(data, "SetEXP", 25f, 100f);
        Call(view, "Dispose");
        Call(data, "ApplyDamage", 10f);
        Call(data, "TrySpendMP", 1f);
        Call(data, "SetEXP", 50f, 100f);
        Assert.That(hp.normalizedValue, Is.EqualTo(1f));
        Assert.That(mp.normalizedValue, Is.EqualTo(1f));
        Assert.That(exp.normalizedValue, Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void MissingOptionalResourceBarsAreSafe()
    {
        Assert.DoesNotThrow(() => Call(view, "BindResourceSliders", null, null));
        Assert.DoesNotThrow(() => Call(data, "TrySpendMP", 1f));
        Assert.DoesNotThrow(() => Call(data, "SetEXP", 1f, 10f));
    }

    [Test]
    public void MainScenePlayerReferencesExistingHudBars()
    {
        string scene = File.ReadAllText(Path.Combine(Application.dataPath, "9.Scenes/MainTestScene.unity"));
        StringAssert.Contains("healthSlider: {fileID: 496842392}", scene);
        StringAssert.Contains("mpSlider: {fileID: 1395659084}", scene);
        StringAssert.Contains("expSlider: {fileID: 1801941379}", scene);
    }
}
