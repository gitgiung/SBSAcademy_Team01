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
}
