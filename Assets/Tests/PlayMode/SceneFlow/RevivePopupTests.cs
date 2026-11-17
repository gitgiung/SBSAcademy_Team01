using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RevivePopupTests
{
    private Scene scene;
    private Component manager, player, data;
    private InputActionAsset actions;
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static Type TypeOf(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
    private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);
    private static object Property(object obj, string name) => obj.GetType().GetProperty(name).GetValue(obj);
    private GameObject ObjectInScene(string name, params Type[] types)
    {
        var go = new GameObject(name, types);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1;
        scene = SceneManager.CreateScene("MainScene");
        ObjectInScene("Camera", typeof(Camera)).tag = "MainCamera";
        ObjectInScene("Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var go = ObjectInScene("Player", typeof(SpriteRenderer));
        go.SetActive(false);
        data = go.AddComponent(TypeOf("PlayerData"));
        player = go.AddComponent(TypeOf("Player"));
        actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
        map.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
        map.AddAction("Parry", InputActionType.Button, "<Keyboard>/e");
        actions.AddActionMap(map);
        Set(player, "inputActions", actions);
        Set(player, "data", data);
        go.SetActive(true);
        var oldPanel = ObjectInScene("OldPanel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var font = Resources.Load<TMP_FontAsset>("TestFonts/NanumGothic Combat SDF");
        if (font != null) oldPanel.GetComponent<TMP_Text>().font = font;
        var managerObject = ObjectInScene("GameManager");
        managerObject.SetActive(false);
        manager = managerObject.AddComponent(TypeOf("GameManager"));
        Set(manager, "player", player);
        Set(manager, "playerData", data);
        Set(manager, "gameOverPanel", oldPanel);
        managerObject.SetActive(true);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1;
        yield return SceneManager.UnloadSceneAsync(scene);
        UnityEngine.Object.DestroyImmediate(actions);
    }

    private IEnumerator Die()
    {
        Call(player, "TakeDamage", 100000f, null);
        yield return null;
        Assert.That(Property(manager, "state").ToString(), Is.EqualTo("ReviveOffer"));
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(player.gameObject.activeSelf, Is.False);
    }

    private Button ButtonNamed(string name)
    {
        foreach (var b in manager.GetComponentsInChildren<Button>(true))
            if (b.name == name) return b;
        throw new Exception("Missing button " + name);
    }

    [UnityTest]
    public IEnumerator YesRestoresHealthAndInputWithoutResettingMpOrTacticalUses()
    {
        Call(data, "TrySpendMP", 5f);
        float mp = (float)Property(data, "CurrentMP");
        var uses = (System.Collections.IDictionary)player.GetType().GetField("tacticalUsesRemaining", Flags).GetValue(player);
        uses[30001] = 2;
        yield return Die();
        ButtonNamed("Yes").onClick.Invoke();
        Assert.That(Property(manager, "state").ToString(), Is.EqualTo("Playing"));
        Assert.That(player.gameObject.activeSelf, Is.True);
        Assert.That(Property(player, "CanAcceptGameplayInput"), Is.True);
        Assert.That(data.GetType().GetField("currentHealth").GetValue(data), Is.EqualTo(data.GetType().GetField("maxHealth").GetValue(data)));
        Assert.That(Property(data, "CurrentMP"), Is.EqualTo(mp));
        Assert.That(uses[30001], Is.EqualTo(2));
        Assert.That(Call(manager, "TryPrototypeRevive"), Is.False, "중복 클릭으로 다시 부활하면 안 됩니다.");
        yield return Die();
        ButtonNamed("Yes").onClick.Invoke();
        Assert.That(Property(player, "CanAcceptGameplayInput"), Is.True);
    }

    [UnityTest]
    public IEnumerator NoShowsResultsAndCannotReviveAfterDefeat()
    {
        yield return Die();
        ButtonNamed("No").onClick.Invoke();
        Assert.That(Property(manager, "state").ToString(), Is.EqualTo("Defeat"));
        Assert.That(ButtonNamed("Restart").gameObject.activeInHierarchy, Is.True);
        Assert.That(ButtonNamed("ReturnToTitle").gameObject.activeInHierarchy, Is.True);
        Assert.That(Call(manager, "TryPrototypeRevive"), Is.False);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [UnityTest]
    public IEnumerator AdNoticeRequiresConfirmationBeforePrototypeRevival()
    {
        yield return Die();
        ButtonNamed("AdRevive").onClick.Invoke();
        Assert.That(ButtonNamed("Confirm").gameObject.activeInHierarchy, Is.True);
        Assert.That(ButtonNamed("Yes").gameObject.activeInHierarchy, Is.False);
        Assert.That(Property(manager, "state").ToString(), Is.EqualTo("ReviveOffer"));
        ButtonNamed("Confirm").onClick.Invoke();
        Assert.That(Property(manager, "state").ToString(), Is.EqualTo("Playing"));
        Assert.That(Time.timeScale, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator PopupArtAndLayoutsCanBeCaptured()
    {
        yield return Die();
        Assert.That(Resources.Load<Sprite>("UI/ReviveAngel"), Is.Not.Null);
        Canvas.ForceUpdateCanvases();
        yield return null;
        Capture("revive-popup.png");
        yield return null;
        yield return null;
        ButtonNamed("AdRevive").onClick.Invoke();
        yield return null;
        Capture("no-ad-popup.png");
        yield return null;
        yield return null;
        ButtonNamed("Confirm").onClick.Invoke();
        yield return Die();
        ButtonNamed("No").onClick.Invoke();
        yield return null;
        Capture("game-over-popup.png");
        yield return null;
        yield return null;
    }

    private void Capture(string name)
    {
        // 배치 테스트에는 GameView 프레임이 없으므로 실제 uGUI를 카메라에 렌더합니다.
        var canvas = manager.GetComponentInChildren<Canvas>();
        var camera = Camera.main;
        var target = new RenderTexture(1600, 900, 24);
        var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        foreach (var text in manager.GetComponentsInChildren<TMP_Text>())
        {
            Assert.That(text.font, Is.Not.Null);
            Assert.That(text.fontSharedMaterial.shader, Is.Not.Null, "폰트 셰이더도 검증 복사본에 있어야 합니다.");
            text.ForceMeshUpdate();
        }
        camera.Render();
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        pixels.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath, "../" + name), pixels.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.Object.DestroyImmediate(pixels);
        UnityEngine.Object.DestroyImmediate(target);
    }
}
