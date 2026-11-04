#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PlayerLightSkillTests
{
    private readonly List<Object> objects = new List<Object>();
    private Component player;
    private Component data;
    private object skill;
    private GameObject projectile;
    private Button button;
    private Keyboard keyboard;
    private Mouse mouse;
    private InputSettings originalInputSettings;
    private static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Call(object target, string name, params object[] args) => target.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length && !m.IsGenericMethod).Invoke(target, args);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static object Field(object target, string name) => target.GetType().GetField(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static T Read<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
    private GameObject Create(string name)
    {
        var go = new GameObject(name); objects.Add(go); return go;
    }
    private void CreatePlayer(bool configured = true)
    {
        var go = Create("light skill test player"); go.SetActive(false);
        data = go.AddComponent(Runtime("PlayerData")); Set(data, "maxMP", 100f);
        go.AddComponent<SpriteRenderer>();
        var animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("PlayerAnimator/Player");
        var actions = ScriptableObject.CreateInstance<InputActionAsset>(); objects.Add(actions);
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value);
        map.AddAction("Dash", InputActionType.Button);
        map.AddAction("Parry", InputActionType.Button);
        map.AddAction("Attack", InputActionType.Button, "<Keyboard>/enter").AddBinding("<Mouse>/leftButton");
        actions.AddActionMap(map);
        var buttonObject = Create("N_Attack"); buttonObject.AddComponent<RectTransform>();
        button = buttonObject.AddComponent<Button>();
        projectile = Resources.Load<GameObject>("SkillProjectiles/FlameBullet");
        player = go.AddComponent(Runtime("Player"));
        Set(player, "inputActions", actions); Set(player, "lightAttackButton", button); Set(player, "lightAttackProjectile", projectile);
        skill = Activator.CreateInstance(Runtime("Data.SkillData"));
        Set(skill, "DataId", 10001); Set(skill, "Name", "불꽃 탄환");
        Set(skill, "ActiveConfigured", configured); Set(skill, "MPCost", 10f); Set(skill, "BaseDamage", 10f); Set(skill, "AnimName", "LightAttack");
        var loadout = Read<object>(data, "Loadout");
        Call(loadout, "TryAddAttackSkill", skill); Call(loadout, "TryEquip", Enum.Parse(Runtime("PlayerSkillButton"), "Light"), 10001);
        go.SetActive(true);
        var camera = Create("light skill camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.orthographic = true; camera.orthographicSize = 10; camera.transform.position = new Vector3(0, 0, -10);
        Call(Field(player, "view"), "SetMovementAnimation", Vector2.right, false, false, .15f);
    }
    private bool Cast() => (bool)Call(player, "TryUseLightSkill");
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        foreach (var go in objects.OfType<GameObject>()) if (go != null) Object.Destroy(go);
        yield return null;
        foreach (var obj in objects.Where(o => !(o is GameObject))) if (obj != null) Object.Destroy(obj);
        objects.Clear(); Object.Destroy(GameObject.Find("@Managers")); yield return null;
    }

    [UnityTest]
    public IEnumerator CastSpendsTenOnceAndUnlocksAtActualAnimationEnd()
    {
        CreatePlayer(); yield return null;
        Assert.That(Cast(), Is.True);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90f));
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
        Assert.That(Cast(), Is.False);
        Assert.That(Call(player, "TryDash", Vector2.right), Is.False);
        Assert.That(Call(player, "TryParry"), Is.False);
        yield return new WaitForSeconds(.1f);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
        yield return new WaitForSeconds(1f);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
        Assert.That(Cast(), Is.True);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(80f));
    }

    [UnityTest]
    public IEnumerator InsufficientMPOrMissingDefinitionNeverStartsAnimation()
    {
        CreatePlayer(configured: false); yield return null;
        Assert.That(Cast(), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100f));
        Set(skill, "ActiveConfigured", true); Call(data, "TrySpendMP", 95f);
        Assert.That(Cast(), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(5f));
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
    }

    [UnityTest]
    public IEnumerator MissingProjectileOrAnimationDoesNotSpendMP()
    {
        CreatePlayer(); yield return null;
        Set(player, "lightAttackProjectile", null);
        Assert.That(Cast(), Is.False);
        Set(player, "lightAttackProjectile", projectile); Set(skill, "AnimName", "MissingState");
        Assert.That(Cast(), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100f));
    }

    [UnityTest]
    public IEnumerator NAttackPointerDownStartsOneShotAndClickDoesNotQueueAnother()
    {
        CreatePlayer(); yield return null;
        var events = Create("light skill event system").AddComponent<EventSystem>();
        var pointer = new PointerEventData(events) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90f));
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        yield return new WaitForSeconds(1f);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90f));
    }

    [UnityTest]
    public IEnumerator PooledRedBulletDealsTenDamageInPhysicsCollision()
    {
        CreatePlayer(); yield return null;
        Assert.That(projectile, Is.Not.Null);
        var enemy = Create("light skill enemy"); enemy.SetActive(false); enemy.tag = "Enemy";
        enemy.transform.position = Vector3.right;
        enemy.AddComponent<CircleCollider2D>().radius = .2f;
        var health = enemy.AddComponent(Runtime("EnemyHealth")); Set(health, "maxHealth", 20f);
        enemy.SetActive(true); Physics2D.SyncTransforms();
        Assert.That(Cast(), Is.True);
        yield return new WaitForSeconds(.3f);
        Assert.That(Read<float>(health, "CurrentHealth"), Is.EqualTo(10f));
        var pool = Runtime("Managers").GetProperty("Pool").GetValue(null);
        Assert.That(Call(pool, "GetInactiveCount", projectile), Is.EqualTo(1));
        var renderer = projectile.GetComponent<SpriteRenderer>();
        Assert.That(renderer, Is.Not.Null); Assert.That(renderer.color.r, Is.GreaterThan(.9f));
        Assert.That(renderer.color.g, Is.LessThan(.3f));
    }

    [UnityTest]
    public IEnumerator PauseFreezesAnimationAndDeathOrDisableReleasesLock()
    {
        CreatePlayer(); yield return null; Cast();
        Time.timeScale = 0; yield return new WaitForSecondsRealtime(.9f);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
        Assert.That(Cast(), Is.False);
        Time.timeScale = 1;
        player.gameObject.SetActive(false);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
        player.gameObject.SetActive(true); yield return null;
        Assert.That(Cast(), Is.True);
        Call(player, "TakeDamage", 1000f, null);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
    }

    [UnityTest]
    public IEnumerator DisabledAnimatorCancelsAndCanCastAfterReenable()
    {
        CreatePlayer(); yield return null; Assert.That(Cast(), Is.True);
        var animator = player.GetComponent<Animator>(); animator.enabled = false;
        yield return null;
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
        animator.enabled = true; yield return null;
        Assert.That(Cast(), Is.True);
        yield return new WaitForSeconds(1f);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
    }

    [UnityTest]
    public IEnumerator PauseDuringMPNotificationRejectsAndRefundsUnstartedCast()
    {
        CreatePlayer(); yield return null;
        data.GetType().GetEvent("MPChanged").AddEventHandler(data, (Action<float, float>)((current, maximum) => Time.timeScale = 0));
        Assert.That(Cast(), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100f));
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
    }

    [UnityTest]
    public IEnumerator DisableAnimatorDuringMPNotificationRejectsAndRefundsUnstartedCast()
    {
        CreatePlayer(); yield return null;
        data.GetType().GetEvent("MPChanged").AddEventHandler(data,
            (Action<float, float>)((current, maximum) => player.GetComponent<Animator>().enabled = false));
        Assert.That(Cast(), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100f));
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
    }

    [UnityTest]
    public IEnumerator MetadataMutationInMPNotificationDoesNotChangeAcceptedAnimation()
    {
        CreatePlayer(); yield return null;
        data.GetType().GetEvent("MPChanged").AddEventHandler(data, (Action<float, float>)((current, maximum) =>
        {
            Set(skill, "AnimName", "MissingState"); Set(skill, "AnimImpactDuration", 999f);
        }));
        Assert.That(Cast(), Is.True);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90f));
        var pool = Runtime("Managers").GetProperty("Pool").GetValue(null);
        Assert.That(Call(pool, "GetActiveCount", projectile), Is.EqualTo(1));
        yield return new WaitForSeconds(1f);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
    }

    [UnityTest]
    public IEnumerator ExistingPCAttackActionTriggersEquippedLightSkill()
    {
        originalInputSettings = InputSystem.settings;
        var settings = Object.Instantiate(originalInputSettings); objects.Add(settings);
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = settings; keyboard = InputSystem.AddDevice<Keyboard>();
        CreatePlayer(); yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); InputSystem.Update();
        Assert.That(((InputAction)Field(player, "attackInput")).WasPressedThisFrame(), Is.True);
        Call(player, "Update");
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90f));
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
    }

    [Test]
    public void EightDirectionsAndApprovedMainSceneBindingsExist()
    {
        var clips = Resources.LoadAll<AnimationClip>("PlayerAnimator").Where(c => c.name.StartsWith("LightAttack_")).ToArray();
        Assert.That(clips, Has.Length.EqualTo(8));
        foreach (var clip in clips) Assert.That(clip.length, Is.EqualTo(.75f).Within(.001f));
        string scene = File.ReadAllText(Path.Combine(Application.dataPath, "9.Scenes/MainTestScene.unity"));
        StringAssert.Contains("lightAttackButton: {fileID: 1415531113}", scene);
        StringAssert.Contains("lightSkillId: 10001", scene);
        StringAssert.Contains("MPCost: 10", scene);
        StringAssert.Contains("BaseDamage: 10", scene);
    }

    [UnityTest]
    public IEnumerator FirstMousePressOverHUDDoesNotSpendMPBeforeUIUpdate()
    {
        originalInputSettings = InputSystem.settings;
        var settings = Object.Instantiate(originalInputSettings); objects.Add(settings);
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = settings; mouse = InputSystem.AddDevice<Mouse>();
        CreatePlayer(); yield return null;
        Create("mouse test EventSystem").AddComponent<EventSystem>();
        var canvasObject = Create("mouse test HUD");
        var canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();
        var panel = Create("other HUD button").AddComponent<RectTransform>(); panel.SetParent(canvas.transform, false);
        panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.offsetMin = panel.offsetMax = Vector2.zero;
        panel.gameObject.AddComponent<Image>().raycastTarget = true;
        yield return null; // 화면 배치를 확정하되 InputModule은 만들지 않아 포인터 캐시는 비워 둡니다.
        Canvas.ForceUpdateCanvases();
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
            { position = new Vector2(Screen.width / 2f, Screen.height / 2f) }, hits);
        Assert.That(hits.Any(hit => hit.module is GraphicRaycaster), Is.True, "The fixture HUD must receive the current-position raycast.");
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f), buttons = 1 });
        InputSystem.Update();
        Assert.That(((InputAction)Field(player, "attackInput")).WasPressedThisFrame(), Is.True);
        Call(player, "Update");
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100f));
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);
    }
}
#endif
