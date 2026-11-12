using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Assembly-CSharp의 기존 경계를 보존하며 실제 Player/방어 코루틴을 실행합니다.
public sealed class MobileReadinessTests
{
    private GameObject playerObject;
    private GameObject guardObject;
    private GameObject eventObject;
    private Component player;
    private InputActionAsset actions;
    private readonly BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private object Get(object target, string name) => target.GetType().GetField(name, fields).GetValue(target);
    private void Set(object target, string name, object value) => target.GetType().GetField(name, fields).SetValue(target, value);
    private object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, fields).Invoke(target, args);
    private bool Blocking => (bool)Get(player, "defense").GetType().GetProperty("IsBlockingDamage").GetValue(Get(player, "defense"));

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        eventObject = new GameObject("MobileTest EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        guardObject = new GameObject("Guard", typeof(RectTransform), typeof(Button));
        playerObject = new GameObject("MobileTest Player");
        playerObject.SetActive(false);
        playerObject.AddComponent<SpriteRenderer>();
        var data = playerObject.AddComponent(RuntimeType("PlayerData"));
        player = playerObject.AddComponent(RuntimeType("Player"));
        actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
        map.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
        map.AddAction("Parry", InputActionType.Button, "<Keyboard>/e");
        actions.AddActionMap(map);
        Set(player, "inputActions", actions);
        Set(player, "data", data);
        // 새 필드가 없는 기존 코드에서도 테스트 자체는 컴파일되어 실제 미연결을 검출합니다.
        player.GetType().GetField("guardButton", fields)?.SetValue(player, guardObject.GetComponent<Button>());
        Set(player, "parryStartup", 0f);
        Set(player, "parryActive", .04f);
        Set(player, "guardTransitionTime", .04f);
        Set(player, "parryRecovery", 0f);
        Set(player, "normalGuardCooldown", 0f);
        playerObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        if (playerObject != null) UnityEngine.Object.DestroyImmediate(playerObject);
        if (guardObject != null) UnityEngine.Object.DestroyImmediate(guardObject);
        if (eventObject != null) UnityEngine.Object.DestroyImmediate(eventObject);
        if (actions != null) UnityEngine.Object.DestroyImmediate(actions);
    }

    private void Down(int id) => ExecuteEvents.Execute(guardObject, Pointer(id), ExecuteEvents.pointerDownHandler);
    private void Up(int id) => ExecuteEvents.Execute(guardObject, Pointer(id), ExecuteEvents.pointerUpHandler);
    private PointerEventData Pointer(int id) => new PointerEventData(EventSystem.current)
        { pointerId = id, button = PointerEventData.InputButton.Left };
    private IEnumerator AfterParry() { yield return new WaitForSeconds(.12f); }

    [UnityTest]
    public IEnumerator TouchStartsParryThenHoldsGuardUntilItsOwnFingerReleases()
    {
        Down(11);
        Assert.That(Blocking, Is.True, "Guard 터치가 기존 패링을 시작해야 합니다.");
        yield return AfterParry();
        Assert.That(Blocking, Is.True, "키보드 없이 손가락으로 가드를 유지해야 합니다.");
        Down(22);
        Up(22);
        yield return null;
        Assert.That(Blocking, Is.True, "다른 손가락의 해제가 가드를 풀면 안 됩니다.");
        Up(11);
        yield return null;
        Assert.That(Blocking, Is.False);
    }

    [UnityTest]
    public IEnumerator ReleasingDuringParryDoesNotBecomeHeldGuard()
    {
        Down(1);
        Assert.That(Blocking, Is.True);
        Up(1);
        yield return AfterParry();
        Assert.That(Blocking, Is.False);
    }

    [UnityTest]
    public IEnumerator PausedPressIsNotQueuedForResume()
    {
        Time.timeScale = 0f;
        Down(1);
        Assert.That(Blocking, Is.False);
        Time.timeScale = 1f;
        yield return AfterParry();
        Assert.That(Blocking, Is.False);
        Up(1);
        Down(2);
        Assert.That(Blocking, Is.True);
    }

    [UnityTest]
    public IEnumerator FocusLossEndsHeldDefenseAndAllowsANewTouch()
    {
        Down(1);
        yield return AfterParry();
        Assert.That(Blocking, Is.True);
        playerObject.SendMessage("OnApplicationFocus", false);
        yield return null;
        Assert.That(Blocking, Is.False, "포커스 손실 뒤 방어 상태가 남으면 안 됩니다.");
        playerObject.SendMessage("OnApplicationFocus", true);
        Down(2);
        Assert.That(Blocking, Is.True);
    }

    [UnityTest]
    public IEnumerator DisablingGuardButtonReleasesItsHeldInput()
    {
        Down(1);
        yield return AfterParry();
        Assert.That(Blocking, Is.True);
        guardObject.SetActive(false);
        yield return null;
        Assert.That(Blocking, Is.False);
    }

    [UnityTest]
    public IEnumerator PopupPauseDoesNotLeaveTouchHeldAfterResume()
    {
        Down(1);
        yield return AfterParry();
        Assert.That(Blocking, Is.True);
        Time.timeScale = 0f;
        yield return null;
        Time.timeScale = 1f;
        yield return null;
        Assert.That(Blocking, Is.False);
        Down(2);
        Assert.That(Blocking, Is.True);
    }

    [Test]
    public void SafeAreaKeepsHudInsideCutoutsWithoutShrinkingFullscreenBackground()
    {
        Type layoutType = Type.GetType("CanvasSafeArea, Assembly-CSharp", false);
        Assert.That(layoutType, Is.Not.Null, "화면 안전 영역 대응이 필요합니다.");
        var root = new GameObject("Mobile Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var hud = new GameObject("HUD", typeof(RectTransform)).GetComponent<RectTransform>();
            hud.SetParent(root.transform, false);
            hud.anchorMin = hud.anchorMax = new Vector2(0f, 1f);
            hud.anchoredPosition = new Vector2(242f, -145f);
            hud.sizeDelta = new Vector2(400f, 200f);
            var background = new GameObject("Background", typeof(RectTransform)).GetComponent<RectTransform>();
            background.SetParent(root.transform, false);
            background.anchorMin = Vector2.zero;
            background.anchorMax = Vector2.one;
            background.offsetMin = background.offsetMax = Vector2.zero;
            var layout = root.AddComponent(layoutType);
            Call(layout, "Apply", new Rect(120f, 0f, 2160f, 1080f), new Vector2(2400f, 1080f));
            Assert.That(hud.anchorMin.x, Is.EqualTo(.05f).Within(.0001f));
            Assert.That(hud.anchorMin.y, Is.EqualTo(1f));
            Assert.That(hud.anchoredPosition, Is.EqualTo(new Vector2(242f, -145f)));
            Assert.That(background.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(background.anchorMax, Is.EqualTo(Vector2.one));
            var scaler = root.GetComponent<CanvasScaler>();
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(2400f / scaler.referenceResolution.x, Is.EqualTo(1.125f).Within(.0001f));
            Assert.That(1080f / scaler.referenceResolution.y, Is.EqualTo(1f));
            // 회전·해상도 변경 뒤 이전 inset이 누적되면 안 됩니다.
            Call(layout, "Apply", new Rect(0f, 60f, 1920f, 1080f), new Vector2(1920f, 1200f));
            Assert.That(hud.anchorMin.x, Is.EqualTo(0f));
            Assert.That(hud.anchorMin.y, Is.EqualTo(.95f).Within(.0001f));
            Assert.That(Mathf.Min(1920f / scaler.referenceResolution.x, 1200f / scaler.referenceResolution.y), Is.EqualTo(1f).Within(.0001f));
            ((Behaviour)layout).enabled = false;
            Assert.That(hud.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void MissingEventSystemIsCreatedWithNewInputBackend()
    {
        UnityEngine.Object.DestroyImmediate(eventObject);
        eventObject = null;
        var sceneObject = new GameObject("MobileTest Scene");
        sceneObject.SetActive(false);
        try
        {
            var scene = sceneObject.AddComponent(RuntimeType("LobbyScene"));
            Call(scene, "Init");
            Assert.That(EventSystem.current, Is.Not.Null);
            eventObject = EventSystem.current.gameObject;
            Assert.That(eventObject.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            Assert.That(eventObject.GetComponent<StandaloneInputModule>(), Is.Null);
            Call(scene, "Init");
            Assert.That(UnityEngine.Object.FindObjectsByType<EventSystem>().Length, Is.EqualTo(1));
        }
        finally { UnityEngine.Object.DestroyImmediate(sceneObject); }
    }

    private float TacticalRemaining => (float)player.GetType().GetProperty("TacticalCooldownRemaining").GetValue(player);

    [UnityTest]
    public IEnumerator TacticalCooldownUsesSuccessfulCastAndStopsDuringPause()
    {
        Assert.That(player.GetType().GetProperty("TacticalCooldownRemaining"), Is.Not.Null);
        Assert.That(TacticalRemaining, Is.Zero);
        Assert.That((bool)Call(player, "TryUseTacticalSkill"), Is.True);
        float started = TacticalRemaining;
        Assert.That(started, Is.EqualTo(6f).Within(.01f), "기존 표의 30009는 6초입니다.");
        Assert.That((bool)Call(player, "TryUseTacticalSkill"), Is.False);
        Time.timeScale = 0;
        yield return null;
        yield return null;
        Assert.That(TacticalRemaining, Is.EqualTo(started).Within(.001f));
        Time.timeScale = 1;
        yield return new WaitForSeconds(.1f);
        Assert.That(TacticalRemaining, Is.LessThan(started));
    }

    [Test]
    public void FailedTacticalCastDoesNotStartCooldown()
    {
        Assert.That(player.GetType().GetProperty("TacticalCooldownRemaining"), Is.Not.Null);
        Time.timeScale = 0;
        Assert.That((bool)Call(player, "TryUseTacticalSkill"), Is.False);
        Assert.That(TacticalRemaining, Is.Zero);
        Set(player, "hasDied", true);
        Time.timeScale = 1;
        Assert.That((bool)Call(player, "TryUseTacticalSkill"), Is.False);
        Assert.That(TacticalRemaining, Is.Zero);
    }

    [Test]
    public void TacticalDisplayTracksPlayerWithoutCastingAgainOnClick()
    {
        var button = new GameObject("Skill", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            Set(player, "shieldButton", button.GetComponent<Button>());
            Call(player, "BindTacticalButton");
            var display = button.GetComponent(RuntimeType("SprintCooldownUI"));
            Assert.That(display, Is.Not.Null, "전략 버튼에도 기존 쿨타임 표시를 연결합니다.");
            Assert.That((bool)Call(player, "TryUseTacticalSkill"), Is.True);
            Call(display, "Refresh");
            var image = (Image)Get(display, "cooldownImage");
            var text = (TMPro.TMP_Text)Get(display, "cooldownText");
            Assert.That(image.gameObject.activeSelf, Is.True);
            Assert.That(text.text, Is.EqualTo("6"));
            Assert.That(image.raycastTarget, Is.False);
            float ready = (float)Get(player, "nextTacticalReadyTime");
            button.GetComponent<Button>().onClick.Invoke();
            Assert.That((float)Get(player, "nextTacticalReadyTime"), Is.EqualTo(ready));
            Call(player, "BindTacticalButton");
            Assert.That(button.GetComponentsInChildren<TMPro.TMP_Text>(true).Length, Is.EqualTo(1));
            Call(display, "BindTactical", (object)null);
            Call(display, "BindTactical", player);
            Assert.That(button.GetComponentsInChildren<Image>(true).Length, Is.EqualTo(2), "플레이어 재연결 시 덮개가 중복되면 안 됩니다.");
            Set(player, "nextTacticalReadyTime", Time.time - 1);
            Call(display, "Refresh");
            Assert.That(image.gameObject.activeSelf, Is.False);
            Assert.That(button.activeSelf, Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(button); }
    }

    private GameObject CreateControls(out Component layout, out Component safeArea, out RectTransform buttons,
        out RectTransform joystick, out RectTransform background)
    {
        var root = new GameObject("Controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.SetActive(false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        buttons = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
        buttons.SetParent(root.transform, false);
        buttons.anchorMin = buttons.anchorMax = new Vector2(0, .5f);
        buttons.anchoredPosition = new Vector2(85, -80);
        buttons.sizeDelta = new Vector2(120, 712);
        joystick = new GameObject("Joystick", typeof(RectTransform)).GetComponent<RectTransform>();
        joystick.SetParent(root.transform, false);
        joystick.anchorMin = joystick.anchorMax = new Vector2(1, 0);
        joystick.pivot = Vector2.zero;
        joystick.sizeDelta = new Vector2(960, 645);
        joystick.anchoredPosition = new Vector2(-960, 0);
        background = new GameObject("Background", typeof(RectTransform)).GetComponent<RectTransform>();
        background.SetParent(joystick, false);
        background.anchorMin = background.anchorMax = Vector2.zero;
        background.anchoredPosition = new Vector2(660, 280);
        safeArea = root.AddComponent(RuntimeType("CanvasSafeArea"));
        layout = root.AddComponent(RuntimeType("MobileControlLayout"));
        Set(layout, "buttonGroup", buttons);
        Set(layout, "joystickRoot", joystick);
        Set(layout, "joystickBackground", background);
        root.SetActive(true);
        return root;
    }

    [Test]
    public void ControlsSwapRoundTripsAcrossCutoutsAndRestoresSavedChoice()
    {
        const string key = "MobileControls.Swapped";
        bool existed = PlayerPrefs.HasKey(key);
        int old = PlayerPrefs.GetInt(key);
        GameObject root = null;
        try
        {
            PlayerPrefs.DeleteKey(key);
            root = CreateControls(out var layout, out var safe, out var buttons, out var joystick, out var background);
            Time.timeScale = 0;
            Call(safe, "Apply", new Rect(120, 0, 2160, 1080), new Vector2(2400, 1080));
            Call(layout, "SetControlsSwapped", true);
            Call(safe, "Apply", new Rect(120, 0, 2160, 1080), new Vector2(2400, 1080));
            Assert.That(buttons.anchorMin.x, Is.EqualTo(.95f).Within(.0001));
            Assert.That(buttons.anchoredPosition.x, Is.EqualTo(-85));
            Assert.That(joystick.anchorMin.x, Is.EqualTo(.05f).Within(.0001));
            Assert.That(background.anchorMin.x, Is.EqualTo(1));
            Assert.That(background.anchoredPosition.x, Is.EqualTo(-660));
            ((Behaviour)safe).enabled = false;
            ((Behaviour)safe).enabled = true;
            Call(safe, "Apply", new Rect(0, 60, 1920, 1080), new Vector2(1920, 1200));
            Assert.That(buttons.anchorMin.x, Is.EqualTo(1), "안전 영역 컴포넌트만 재활성화해도 반전을 유지합니다.");
            Assert.That(buttons.anchoredPosition.x, Is.EqualTo(-85));
            Assert.That(background.localScale.x, Is.EqualTo(1), "이미지를 좌우 반전하면 안 됩니다.");
            Call(layout, "SetControlsSwapped", true);
            Call(layout, "SetControlsSwapped", false);
            Call(safe, "Apply", new Rect(0, 60, 1920, 1080), new Vector2(1920, 1200));
            Assert.That(buttons.anchorMin.x, Is.Zero);
            Assert.That(buttons.anchoredPosition.x, Is.EqualTo(85));
            Assert.That(background.anchoredPosition.x, Is.EqualTo(660));
            Call(layout, "SetControlsSwapped", true);
            root.SetActive(false);
            root.SetActive(true);
            Call(safe, "Apply", new Rect(0, 60, 1920, 1080), new Vector2(1920, 1200));
            Assert.That(buttons.anchoredPosition.x, Is.EqualTo(-85), "재활성화 후 누적 반전을 방지합니다.");
            Assert.That(background.anchoredPosition.x, Is.EqualTo(-660));
            UnityEngine.Object.DestroyImmediate(root);
            root = CreateControls(out layout, out safe, out buttons, out joystick, out background);
            Assert.That((bool)layout.GetType().GetProperty("ControlsSwapped").GetValue(layout), Is.True);
            Assert.That(buttons.anchoredPosition.x, Is.EqualTo(-85));
            Assert.That(Time.timeScale, Is.Zero);
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (existed) PlayerPrefs.SetInt(key, old); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void PauseOptionsButtonSwapsControlsAndKeepsTheGamePaused()
    {
        const string key = "MobileControls.Swapped";
        bool existed = PlayerPrefs.HasKey(key);
        int old = PlayerPrefs.GetInt(key);
        GameObject controls = null;
        GameObject canvas = null;
        try
        {
            PlayerPrefs.DeleteKey(key);
            controls = CreateControls(out var layout, out _, out _, out _, out _);
            canvas = new GameObject("Pause Canvas", typeof(RectTransform), typeof(Canvas));
            var panel = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(panel, false);
            var back = new GameObject("Back", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            back.transform.SetParent(panel, false);
            var status = canvas.AddComponent(RuntimeType("PlayerStatusPanel"));
            Call(status, "Initialize", content, back, Get(player, "data"), null);
            Time.timeScale = 0;
            Call(status, "ShowOptions");
            var options = (GameObject)Get(status, "optionsRoot");
            var swap = options.transform.Find("SwapControls").GetComponent<Button>();
            Assert.That(swap.interactable, Is.True);
            swap.onClick.Invoke();
            Assert.That((bool)layout.GetType().GetProperty("ControlsSwapped").GetValue(layout), Is.True);
            Assert.That(PlayerPrefs.GetInt(key), Is.EqualTo(1));
            Assert.That(swap.GetComponentInChildren<TMPro.TMP_Text>().text, Does.Contain("이동 왼쪽"));
            Assert.That(Time.timeScale, Is.Zero);
            Call(status, "HideOptions");
            Call(status, "ShowOptions");
            Assert.That(swap.GetComponentInChildren<TMPro.TMP_Text>().text, Does.Contain("이동 왼쪽"));
            swap.onClick.Invoke();
            Assert.That(PlayerPrefs.GetInt(key), Is.Zero);
            Assert.That(Time.timeScale, Is.Zero);
        }
        finally
        {
            if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
            if (controls != null) UnityEngine.Object.DestroyImmediate(controls);
            if (existed) PlayerPrefs.SetInt(key, old); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void DashDisplayStillUsesDashCooldownAndKeepsItsClickListener()
    {
        var button = new GameObject("Sprint", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            var display = button.AddComponent(RuntimeType("SprintCooldownUI"));
            Set(display, "player", player);
            button.GetComponent<Button>().onClick.Invoke();
            float remaining = (float)player.GetType().GetProperty("DashCooldownRemaining").GetValue(player);
            Assert.That(remaining, Is.GreaterThan(0));
            Assert.That(TacticalRemaining, Is.Zero);
            Call(display, "Refresh");
            var text = (TMPro.TMP_Text)Get(display, "cooldownText");
            Assert.That(text.text, Is.EqualTo(Mathf.CeilToInt(remaining).ToString()));
            button.SetActive(false);
            button.SetActive(true);
            Assert.That(button.activeSelf, Is.True);
            Assert.That(text.enabled, Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(button); }
    }

    [Test]
    public void TacticalDisplayCanBindBeforeInactiveButtonAwakes()
    {
        var button = new GameObject("Skill", typeof(RectTransform), typeof(Image), typeof(Button));
        button.SetActive(false);
        try
        {
            Set(player, "shieldButton", button.GetComponent<Button>());
            Assert.DoesNotThrow(() => Call(player, "BindTacticalButton"));
            button.SetActive(true);
            var display = button.GetComponent(RuntimeType("SprintCooldownUI"));
            Assert.That(display, Is.Not.Null);
            Assert.DoesNotThrow(() => Call(display, "Refresh"));
            Assert.That(button.GetComponentsInChildren<TMPro.TMP_Text>(true).Length, Is.EqualTo(1));
            Assert.That(button.GetComponentsInChildren<Image>(true).Length, Is.EqualTo(2));
        }
        finally { UnityEngine.Object.DestroyImmediate(button); }
    }

    [UnityTest]
    public IEnumerator SwappingClearsActualJoystickInputWithoutFlippingItsArtwork()
    {
        const string key = "MobileControls.Swapped";
        bool existed = PlayerPrefs.HasKey(key);
        int old = PlayerPrefs.GetInt(key);
        GameObject root = null;
        try
        {
            PlayerPrefs.DeleteKey(key);
            root = CreateControls(out var layout, out _, out _, out var joystickRoot, out var background);
            var joystick = joystickRoot.gameObject.AddComponent(RuntimeType("VariableJoystick"));
            var handle = new GameObject("Handle", typeof(RectTransform)).GetComponent<RectTransform>();
            handle.SetParent(background, false);
            Set(joystick, "background", background);
            var baseType = joystick.GetType().BaseType;
            baseType.GetField("handle", fields).SetValue(joystick, handle);
            yield return null;
            baseType.GetField("input", fields).SetValue(joystick, Vector2.one);
            Assert.That((Vector2)joystick.GetType().GetProperty("Direction").GetValue(joystick), Is.Not.EqualTo(Vector2.zero));
            Time.timeScale = 0;
            Call(layout, "SetControlsSwapped", true);
            Assert.That((Vector2)joystick.GetType().GetProperty("Direction").GetValue(joystick), Is.EqualTo(Vector2.zero));
            Assert.That(handle.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(background.localScale, Is.EqualTo(Vector3.one));
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (existed) PlayerPrefs.SetInt(key, old); else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
