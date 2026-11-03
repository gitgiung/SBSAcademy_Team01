using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PlayerStatusTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private float originalVolume;
    private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethods()
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length && !m.IsGenericMethod).Invoke(target, args);
    private static T Read<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name).SetValue(target, value);
    private static object NewLoadout() => Activator.CreateInstance(Runtime("PlayerLoadout"));
    private static object Skill(int id, string name = "검증 스킬")
    {
        var skill = Activator.CreateInstance(Runtime("Data.SkillData"));
        Set(skill, "DataId", id);
        Set(skill, "Name", name);
        return skill;
    }
    private GameObject Create(string name, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        objects.Add(go);
        go.transform.SetParent(parent, false);
        return go;
    }
    [SetUp]
    public void SetUp() { originalVolume = AudioListener.volume; Time.timeScale = 1f; }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        AudioListener.volume = originalVolume;
        Time.timeScale = 1f;
        foreach (var go in objects) if (go != null) Object.Destroy(go);
        Object.Destroy(GameObject.Find("@Managers"));
        Object.Destroy(GameObject.Find("@UI_Root"));
        objects.Clear();
        yield return null;
    }

    [Test]
    public void AttackSlotsStartAtSixAndRequireTwoUnlocksToReachEight()
    {
        var loadout = NewLoadout();
        Assert.That(Read<int>(loadout, "UnlockedAttackSlots"), Is.EqualTo(6));
        for (int i = 0; i < 6; i++) Assert.That(Call(loadout, "TryAddAttackSkill", Skill(10001 + i)), Is.True);
        Assert.That(Call(loadout, "TryAddAttackSkill", Skill(20001)), Is.False);
        Assert.That(Call(loadout, "UnlockAttackSlot"), Is.True);
        Assert.That(Call(loadout, "TryAddAttackSkill", Skill(20001)), Is.True);
        Assert.That(Call(loadout, "UnlockAttackSlot"), Is.True);
        Assert.That(Call(loadout, "TryAddAttackSkill", Skill(20002)), Is.True);
        Assert.That(Call(loadout, "UnlockAttackSlot"), Is.False);
        Assert.That(Call(loadout, "TryAddAttackSkill", Skill(20003)), Is.False);
    }

    [Test]
    public void TacticalSkillsHaveFiveSlotsAndRejectDuplicatesAndWrongCategories()
    {
        var loadout = NewLoadout();
        Assert.That(Call(loadout, "TryAddAttackSkill", Skill(30001)), Is.False);
        Assert.That(Call(loadout, "TryAddTacticalSkill", Skill(10001)), Is.False);
        for (int i = 0; i < 5; i++) Assert.That(Call(loadout, "TryAddTacticalSkill", Skill(30001 + i)), Is.True);
        Assert.That(Call(loadout, "TryAddTacticalSkill", Skill(30001)), Is.False);
        Assert.That(Call(loadout, "TryAddTacticalSkill", Skill(30006)), Is.False);
    }

    [Test]
    public void OnlyOwnedMatchingSkillsCanBeEquippedAndRemovalClearsTheButton()
    {
        var loadout = NewLoadout();
        object light = Enum.Parse(Runtime("PlayerSkillButton"), "Light");
        object heavy = Enum.Parse(Runtime("PlayerSkillButton"), "Heavy");
        Call(loadout, "TryAddAttackSkill", Skill(10001));
        Assert.That(Call(loadout, "TryEquip", light, 10001), Is.True);
        Assert.That(Call(loadout, "TryEquip", heavy, 10001), Is.False);
        Assert.That(Call(loadout, "TryEquip", light, 10002), Is.False);
        Assert.That(Call(loadout, "GetEquippedSkillId", light), Is.EqualTo(10001));
        Assert.That(Call(loadout, "RemoveSkill", 10001), Is.True);
        Assert.That(Call(loadout, "GetEquippedSkillId", light), Is.Zero);
    }

    [Test]
    public void RelicsAndAugmentsKeepSeparateSixtyItemLimits()
    {
        var loadout = NewLoadout();
        for (int i = 0; i < 60; i++)
        {
            var relic = Activator.CreateInstance(Runtime("PlayerOwnedItem"));
            Set(relic, "DataId", 50001 + i); Set(relic, "Name", "검증 유물");
            Assert.That(Call(loadout, "TryAddRelic", relic), Is.True);
            var augment = Activator.CreateInstance(Runtime("PlayerOwnedItem"));
            Set(augment, "DataId", 40001 + i); Set(augment, "Name", "검증 증강");
            Assert.That(Call(loadout, "TryAddAugment", augment), Is.True);
        }
        var extra = Activator.CreateInstance(Runtime("PlayerOwnedItem"));
        Set(extra, "DataId", 99999);
        Assert.That(Call(loadout, "TryAddRelic", extra), Is.False);
        Assert.That(Call(loadout, "TryAddAugment", extra), Is.False);
    }

    private Component BuildPanel(out Component data)
    {
        var canvas = Create("status test canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var popup = Create("status test popup", canvas.transform);
        var popupRect = (RectTransform)popup.transform;
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.offsetMin = popupRect.offsetMax = Vector2.zero;
        var panel = Create("Panel", popup.transform);
        var content = Create("ContentRoot", panel.transform);
        var back = Create("BackButton", panel.transform).AddComponent<Button>();
        var source = Create("status test player");
        data = source.AddComponent(Runtime("PlayerData"));
        var view = popup.AddComponent(Runtime("PlayerStatusPanel"));
        Call(view, "Initialize", content.GetComponent<RectTransform>(), back, data, null);
        return view;
    }

    [Test]
    public void StatusPanelShowsEightAttackSlotsFiveTacticalSlotsAndTwoLocks()
    {
        var view = BuildPanel(out var data);
        Assert.That(Read<int>(view, "AttackSlotCount"), Is.EqualTo(8));
        Assert.That(Read<int>(view, "TacticalSlotCount"), Is.EqualTo(5));
        var texts = view.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text).ToArray();
        Assert.That(texts.Count(t => t == "잠김"), Is.EqualTo(2));
        Assert.That(texts, Does.Contain("공격 스킬").And.Contain("전략 스킬").And.Contain("유물").And.Contain("증강"));
        Assert.That(texts, Does.Contain("100 / 100"));
        var loadout = Read<object>(data, "Loadout");
        Call(loadout, "TryAddAttackSkill", Skill(10001, "검증 불꽃 탄환"));
        Assert.That(view.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains("검증 불꽃 탄환")), Is.True);
        Call(loadout, "UnlockAttackSlot");
        Assert.That(view.GetComponentsInChildren<TMP_Text>(true).Count(t => t.text == "잠김"), Is.EqualTo(1));
    }

    [Test]
    public void ReinitializingStatusViewDoesNotDuplicateLayoutOrRetainOldPlayer()
    {
        var view = BuildPanel(out var first);
        int oldCount = view.GetComponentsInChildren<Button>(true).Length;
        var second = Create("second status player").AddComponent(Runtime("PlayerData"));
        var content = view.transform.Find("Panel/ContentRoot").GetComponent<RectTransform>();
        var back = view.transform.Find("Panel/BackButton").GetComponent<Button>();
        Call(view, "Initialize", content, back, second, null);
        Assert.That(view.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(oldCount));
        Call(Read<object>(first, "Loadout"), "TryAddAttackSkill", Skill(10001, "이전 플레이어 스킬"));
        Assert.That(view.GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains("이전 플레이어")), Is.False);
    }

    [Test]
    public void AudioOptionsKeepCombatPausedAndApplyMasterAndChannelVolumes()
    {
        var view = BuildPanel(out _);
        Time.timeScale = 0f;
        Call(view, "ShowOptions");
        Assert.That(Read<bool>(view, "OptionsVisible"), Is.True);
        var master = view.GetComponentsInChildren<Slider>(true).Single(s => s.name == "MasterVolume");
        master.value = 0.35f;
        Assert.That(AudioListener.volume, Is.EqualTo(0.35f).Within(0.001f));
        var bgm = view.GetComponentsInChildren<Slider>(true).Single(s => s.name == "BgmVolume");
        bgm.value = 0.25f;
        var sound = Runtime("Managers").GetProperty("Sound").GetValue(null);
        object kind = Enum.Parse(Runtime("Define+ESound"), "Bgm");
        Assert.That(Call(sound, "GetVolume", kind), Is.EqualTo(0.25f).Within(0.001f));
        Call(view, "HideOptions");
        Assert.That(Read<bool>(view, "OptionsVisible"), Is.False);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [Test]
    public void KeyboardSubmitCanEnterAdjustAndLeaveOptionsWithoutLosingSelection()
    {
        AudioListener.volume = 1f;
        var view = BuildPanel(out _);
        var events = Create("status test EventSystem").AddComponent<EventSystem>();
        var options = view.GetComponentsInChildren<Button>(true).Single(b => b.name == "OptionsButton");
        events.SetSelectedGameObject(options.gameObject);
        ExecuteEvents.Execute(options.gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
        var master = view.GetComponentsInChildren<Slider>(true).Single(s => s.name == "MasterVolume");
        Assert.That(events.currentSelectedGameObject, Is.EqualTo(master.gameObject));
        ExecuteEvents.Execute(master.gameObject, new AxisEventData(events) { moveDir = MoveDirection.Left }, ExecuteEvents.moveHandler);
        Assert.That(AudioListener.volume, Is.LessThan(1f));
        var back = view.GetComponentsInChildren<Button>(true).Single(b => b.name == "BackToStatus");
        events.SetSelectedGameObject(back.gameObject);
        ExecuteEvents.Execute(back.gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
        Assert.That(events.currentSelectedGameObject.activeInHierarchy, Is.True);
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("BackButton"));
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator ActualPausePrefabKeepsPauseThroughOptionsAndResumesOnBack()
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/CombatUI.prefab");
        Assert.That(prefab, Is.Not.Null);
        var root = Object.Instantiate(prefab);
        objects.Add(root);
        var controller = root.GetComponent(Runtime("CombatPopupController"));
        var popup = (Component)Call(controller, "ShowPause");
        var view = popup.GetComponent(Runtime("PlayerStatusPanel"));
        Assert.That(view, Is.Not.Null);
        Assert.That(Time.timeScale, Is.Zero);
        Call(view, "ShowOptions");
        yield return null;
        Assert.That(Time.timeScale, Is.Zero);
        Call(view, "HideOptions");
        Assert.That(Time.timeScale, Is.Zero);
        var back = (Button)popup.GetType().GetProperty("CloseButton").GetValue(popup);
        back.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Call(controller, "ShowPause");
        Assert.That(Read<int>(view, "AttackSlotCount"), Is.EqualTo(8));
        Assert.That(view.GetComponentsInChildren<Button>(true).Count(b => b.name == "OptionsButton"), Is.EqualTo(1));
        back.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }
#endif
}
