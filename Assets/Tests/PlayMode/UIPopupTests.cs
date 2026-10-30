using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class UIPopupTests
{
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private object ui;
    private IDictionary resources;
    private readonly List<GameObject> prefabs = new List<GameObject>();

    private object Call(string method, params object[] args) => ui.GetType().GetMethods()
        .Single(m => m.Name == method && !m.IsGenericMethod && m.GetParameters().Length == args.Length)
        .Invoke(ui, args);

    private object Create(string method, string name) => ui.GetType().GetMethod(method)
        .MakeGenericMethod(RuntimeType(method == "ShowSceneUI" ? "UI_Scene" :
            method == "ShowPopupUI" ? "UI_Popup" : "UI_Base"))
        .Invoke(ui, method == "MakeSubItem" ? new object[] { null, name, false } :
            method == "MakeWorldSpaceUI" ? new object[] { null, name } : new object[] { name });

    private Component Show(string name) => (Component)Create("ShowPopupUI", name);
    private int Count => (int)Call("GetPopupCount");
    private static int Order(Component popup) => popup.GetComponent<Canvas>().sortingOrder;

    private void AddPrefab(string name)
    {
        var prefab = new GameObject(name);
        prefab.SetActive(false);
        prefab.AddComponent(RuntimeType("UI_Popup"));
        resources[name] = prefab;
        prefabs.Add(prefab);
    }

    [SetUp]
    public void Setup()
    {
        var managers = RuntimeType("Managers");
        ui = managers.GetProperty("UI").GetValue(null);
        var resource = managers.GetProperty("Resource").GetValue(null);
        resources = (IDictionary)resource.GetType().GetField("_resources", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(resource);
        AddPrefab("PopupA");
        AddPrefab("PopupB");
        AddPrefab("UI_Popup");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Call("Clear");
        foreach (var prefab in prefabs) if (prefab != null) Object.Destroy(prefab);
        prefabs.Clear();
        foreach (var root in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            if (root.name == "@Managers" || root.name == "@UI_Root") Object.Destroy(root);
        yield return null;
    }

    [Test]
    public void RepeatedShowKeepsOneEntryAndOneCloseHidesIt()
    {
        var first = Show("PopupA");
        Assert.That(Show("PopupA"), Is.SameAs(first));
        Assert.That(Count, Is.EqualTo(1));
        Call("ClosePopupUI");
        Assert.That(Count, Is.Zero);
        Assert.That(first.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void ShowingAnOpenPopupBringsItAboveOtherPopups()
    {
        var a = Show("PopupA");
        var b = Show("PopupB");
        Show("PopupA");
        Assert.That(Count, Is.EqualTo(2));
        Assert.That(Order(a), Is.GreaterThan(Order(b)));
        Call("ClosePopupUI", a);
        Assert.That(a.gameObject.activeSelf, Is.False);
        Assert.That(b.gameObject.activeSelf, Is.True);
        Assert.That(Count, Is.EqualTo(1));
    }

    [Test]
    public void ReopeningCachedPopupRefreshesCanvasOrder()
    {
        var a = Show("PopupA");
        Call("ClosePopupUI");
        var b = Show("PopupB");
        Assert.That(Show("PopupA"), Is.SameAs(a));
        Assert.That(Order(a), Is.GreaterThan(Order(b)));
    }

    [Test]
    public void CachePreparesInactivePopupWithoutChangingOpenStack()
    {
        var a = Show("PopupA");
        Call("CachePopupUI", RuntimeType("UI_Popup"));
        Assert.That(Count, Is.EqualTo(1));
        var cache = (IDictionary)ui.GetType().GetField("_popups", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
        var cached = (Component)cache["UI_Popup"];
        Assert.That(cached.gameObject.activeSelf, Is.False);
        Assert.That(cached.transform.parent.name, Is.EqualTo("@UI_Root"));
        Call("CachePopupUI", RuntimeType("UI_Popup"));
        Assert.That(Count, Is.EqualTo(1));
        Assert.That(a.gameObject.activeSelf, Is.True);
        Assert.That(Show("UI_Popup"), Is.SameAs(cached));
    }

    [Test]
    public void CachingAnOpenPopupKeepsItOpenWithoutDuplicateEntry()
    {
        var popup = Show("UI_Popup");
        Call("CachePopupUI", RuntimeType("UI_Popup"));
        Assert.That(Count, Is.EqualTo(1));
        Assert.That(popup.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void CacheAllDoesNotCloseExistingOpenPopups()
    {
        var popup = Show("PopupA");
        Call("CacheAllPopups");
        Assert.That(Count, Is.EqualTo(1));
        Assert.That(popup.gameObject.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator InvalidCachedPrefabIsRejectedAndDestroyed()
    {
        var invalid = new GameObject("InvalidPopupFixture");
        invalid.SetActive(false);
        prefabs.Add(invalid);
        resources["UI_Popup"] = invalid;
        LogAssert.Expect(LogType.Error, "Popup prefab UI_Popup does not contain UI_Popup.");
        Call("CachePopupUI", RuntimeType("UI_Popup"));
        yield return null;
        var cache = (IDictionary)ui.GetType().GetField("_popups", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
        Assert.That(cache.Contains("UI_Popup"), Is.False);
        Assert.That(Count, Is.Zero);
        Assert.That(Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
            .Count(go => go.name == "InvalidPopupFixture"), Is.EqualTo(1), "only the fixture prefab may remain");
    }

    [UnityTest]
    public IEnumerator DestroyedPopupIsRemovedAndCanBeRecreated()
    {
        var a = Show("PopupA");
        Show("PopupB");
        Object.Destroy(a.gameObject);
        yield return null;
        Assert.That(Count, Is.EqualTo(1));
        var replacement = Show("PopupA");
        Assert.That(replacement != null, Is.True);
        Assert.That(Count, Is.EqualTo(2));
        Call("CloseAllPopupUI");
        Assert.That(Count, Is.Zero);
    }

    [Test]
    public void ExternallyHiddenPopupIsNotCountedAndCanReopen()
    {
        var a = Show("PopupA");
        a.gameObject.SetActive(false);
        Assert.That(Count, Is.Zero);
        Assert.That(Show("PopupA"), Is.SameAs(a));
        Assert.That(Count, Is.EqualTo(1));
    }

    [Test]
    public void ClosingNonTopPopupPreservesBothAndCloseAllHidesBoth()
    {
        var a = Show("PopupA");
        var b = Show("PopupB");
        Call("ClosePopupUI", a);
        Assert.That(Count, Is.EqualTo(2));
        Call("CloseAllPopupUI");
        Assert.That(Count, Is.Zero);
        Assert.That(a.gameObject.activeSelf || b.gameObject.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator ClearDestroysOwnedCachedPopupsAndAllowsFreshShow()
    {
        var a = Show("PopupA");
        Call("ClosePopupUI");
        Call("Clear");
        yield return null;
        Assert.That(a == null, Is.True);
        Assert.That(Count, Is.Zero);
        Assert.That(Show("PopupA") != null, Is.True);
    }

    [TestCase("ShowPopupUI")]
    [TestCase("ShowSceneUI")]
    [TestCase("ShowBaseUI")]
    [TestCase("MakeSubItem")]
    [TestCase("MakeWorldSpaceUI")]
    public void MissingPrefabReturnsNullWithoutChangingOpenPopups(string method)
    {
        Show("PopupA");
        LogAssert.Expect(LogType.Error, "Failed to load prefab : MissingUI. Addressables 로딩 완료 여부를 확인하세요.");
        Assert.That(Create(method, "MissingUI"), Is.Null);
        Assert.That(Count, Is.EqualTo(1));
    }

    [Test]
    public void MissingCachedPrefabDoesNotCreateAnEntry()
    {
        resources.Remove("UI_Popup");
        LogAssert.Expect(LogType.Error, "Failed to load prefab : UI_Popup. Addressables 로딩 완료 여부를 확인하세요.");
        Assert.DoesNotThrow(() => Call("CachePopupUI", RuntimeType("UI_Popup")));
        Assert.That(Count, Is.Zero);
        var cache = (IDictionary)ui.GetType().GetField("_popups", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ui);
        Assert.That(cache.Contains("UI_Popup"), Is.False);
    }
}
