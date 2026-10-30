using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class CombatPopupTests
{
    private readonly List<GameObject> owned = new List<GameObject>();
    private Component controller;
    private Component pause;
    private Component level;
    private Component back;
    private Component pauseButton;
    private static Type Runtime(string name, string assembly = "Assembly-CSharp")
    {
        Type type = Type.GetType(name + ", " + assembly);
        Assert.That(type, Is.Not.Null, name + " must exist");
        return type;
    }
    private static object Service(string name) => Runtime("Managers").GetProperty(name).GetValue(null);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethods()
        .Single(m => m.Name == name && !m.IsGenericMethod && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Click(Component button) => ((UnityEvent)button.GetType().GetProperty("onClick").GetValue(button)).Invoke();
    private GameObject New(string name, Transform parent = null)
    {
        var go = new GameObject(name);
        owned.Add(go);
        go.transform.SetParent(parent, false);
        return go;
    }

    private void Create()
    {
        var root = New("test combat UI");
        root.SetActive(false);
        controller = root.AddComponent(Runtime("CombatPopupController"));
        pause = New("CombatPausePopup", root.transform).AddComponent(Runtime("UI_CombatPopup"));
        level = New("CombatLevelUpPopup", root.transform).AddComponent(Runtime("UI_CombatPopup"));
        pause.gameObject.SetActive(false);
        level.gameObject.SetActive(false);
        back = New("BackButton", pause.transform).AddComponent(Runtime("UnityEngine.UI.Button", "UnityEngine.UI"));
        pauseButton = New("PauseButton", root.transform).AddComponent(Runtime("UnityEngine.UI.Button", "UnityEngine.UI"));
        Set(pause, "closeButton", back);
        Set(controller, "pausePopup", pause);
        Set(controller, "levelUpPopup", level);
        Set(controller, "pauseButton", pauseButton);
        root.SetActive(true);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (var go in owned) if (go != null) Object.Destroy(go);
        owned.Clear();
        Object.Destroy(GameObject.Find("@UI_Root"));
        Object.Destroy(GameObject.Find("@Managers"));
        yield return null;
        Time.timeScale = 1f;
    }

    [Test]
    public void PauseButtonAndBackButtonPauseAndResume()
    {
        Create();
        Click(pauseButton);
        Assert.That(pause.gameObject.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Click(back);
        Assert.That(pause.gameObject.activeSelf, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(0));
    }

    [Test]
    public void RepeatedShowKeepsOneEntryAndRestoresOriginalScale()
    {
        Create();
        Time.timeScale = 0.4f;
        Call(controller, "ShowPause");
        Call(controller, "ShowPause");
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(1));
        Click(back);
        Assert.That(Time.timeScale, Is.EqualTo(0.4f));
    }

    [Test]
    public void OverlappingPopupsKeepCombatPausedUntilBothClose()
    {
        Create();
        Call(controller, "ShowLevelUp");
        Call(controller, "ShowPause");
        Assert.That(pause.GetComponent<Canvas>().sortingOrder, Is.GreaterThan(level.GetComponent<Canvas>().sortingOrder));
        Click(back);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(level.gameObject.activeSelf, Is.True);
        Call(controller, "CompleteLevelUp");
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void CompletingLowerPopupDoesNotCloseItOrResumeCombat()
    {
        Create();
        Call(controller, "ShowLevelUp");
        Call(controller, "ShowPause");
        Call(controller, "CompleteLevelUp");
        Assert.That(level.gameObject.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(2));
    }

    [Test]
    public void ClearHidesPopupsAndReleasesTheirPause()
    {
        Create();
        Call(controller, "ShowLevelUp");
        Call(controller, "ShowPause");
        Call(Service("UI"), "Clear");
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(0));
    }

    [Test]
    public void ExternalDisableReleasesPause()
    {
        Create();
        Call(controller, "ShowPause");
        pause.gameObject.SetActive(false);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(0));
    }

    [Test]
    public void DisablingOwnerClosesBothPopupsAndAllowsReuse()
    {
        Create();
        Call(controller, "ShowLevelUp");
        Call(controller, "ShowPause");
        controller.gameObject.SetActive(false);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(pause.gameObject.activeSelf || level.gameObject.activeSelf, Is.False);
        controller.gameObject.SetActive(true);
        Click(pauseButton);
        Click(back);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(0));
    }

    [Test]
    public void AlreadyStoppedCombatIsNotResumed()
    {
        Create();
        Time.timeScale = 0f;
        Call(controller, "ShowPause");
        Click(back);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [Test]
    public void VictoryDuringPopupIsNotResumedByBack()
    {
        Create();
        var game = New("inactive result owner");
        game.SetActive(false);
        var manager = game.AddComponent(Runtime("GameManager"));
        Set(controller, "gameManager", manager);
        Call(controller, "ShowPause");
        Set(manager, "<state>k__BackingField", Enum.Parse(Runtime("GameState"), "Victory"));
        Click(back);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [UnityTest]
    public IEnumerator DestroyingOpenPopupReleasesPause()
    {
        Create();
        Call(controller, "ShowPause");
        Object.Destroy(pause.gameObject);
        yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [Test]
    public void RegisteringSameInstanceDoesNotHideAnOpenPopup()
    {
        Create();
        Call(controller, "ShowPause");
        Assert.That(Call(Service("UI"), "RegisterPopupUI", "CombatPausePopup", pause), Is.True);
        Assert.That(pause.gameObject.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [Test]
    public void ConflictingRegistrationPreservesExistingOpenInstance()
    {
        Create();
        Call(controller, "ShowPause");
        LogAssert.Expect(LogType.Error, "Popup name already registered: CombatPausePopup.");
        Assert.That(Call(Service("UI"), "RegisterPopupUI", "CombatPausePopup", level), Is.False);
        Assert.That(pause.gameObject.activeSelf, Is.True);
        Assert.That(level.gameObject.activeSelf, Is.False);
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(1));
    }

    [Test]
    public void RegisteringAnAliasDoesNotDuplicateOwnership()
    {
        Create();
        LogAssert.Expect(LogType.Error, "Popup instance already registered under another name.");
        Assert.That(Call(Service("UI"), "RegisterPopupUI", "OtherPause", pause), Is.False);
        Call(controller, "ShowPause");
        Click(back);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }
}
