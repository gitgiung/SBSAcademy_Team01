#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class CombatPopupSceneTests
{
    private Scene scene;
    private Component controller;
    private Component Find(string name) => scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
        .Single(c => c != null && c.GetType().Name == name);
    private static T Field<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethods()
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static T Read<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);

    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/9.Scenes/GiungTestScene.unity", new LoadSceneParameters(LoadSceneMode.Additive));
        scene = SceneManager.GetSceneByName("GiungTestScene");
        yield return null;
        controller = Find("CombatPopupController");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            if (go.name == "@Managers" || go.name == "@UI_Root") Object.Destroy(go);
        yield return null;
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator SceneButtonsFreezeCombatAndResumeOnlyAfterBothPopupsClose()
    {
        var director = Find("EnemySpawnDirector");
        var encounter = Find("BossEncounterController");
        var pool = Find("SceneEnemyPool");
        var player = Find("Player");
        var pauseButton = Field<Button>(controller, "pauseButton");
        var pause = Field<Component>(controller, "pausePopup");
        var back = Field<Button>(pause, "closeButton");
        pauseButton.onClick.Invoke();
        float directorTime = Read<float>(director, "ElapsedTime");
        float encounterTime = Read<float>(encounter, "ElapsedTime");
        int enemyCount = Read<int>(pool, "ActiveEnemyCount");
        float attackTimer = Field<float>(player, "attackTimer");
        Vector3 playerPosition = player.transform.position;
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Read<float>(director, "ElapsedTime"), Is.EqualTo(directorTime));
        Assert.That(Read<float>(encounter, "ElapsedTime"), Is.EqualTo(encounterTime));
        Assert.That(Read<int>(pool, "ActiveEnemyCount"), Is.EqualTo(enemyCount));
        Assert.That(Field<float>(player, "attackTimer"), Is.EqualTo(attackTimer));
        Assert.That(player.transform.position, Is.EqualTo(playerPosition));
        Call(controller, "ShowLevelUp");
        Call(controller, "CompleteLevelUp");
        Assert.That(Time.timeScale, Is.Zero);
        back.onClick.Invoke();
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        yield return new WaitForSecondsRealtime(0.08f);
        Assert.That(Read<float>(director, "ElapsedTime"), Is.GreaterThan(directorTime));
    }

    [UnityTest]
    public IEnumerator PopupRaycastBlocksWorldAndBackWorksWhilePaused()
    {
        var button = Field<Button>(controller, "pauseButton");
        EventSystem.current.SetSelectedGameObject(button.gameObject);
        button.onClick.Invoke();
        yield return null;
        Canvas.ForceUpdateCanvases();
        var popup = Field<Component>(controller, "pausePopup");
        var back = Field<Button>(popup, "closeButton");
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(back.gameObject));
        var data = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, back.transform.position),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Assert.That(hits, Is.Not.Empty);
        Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.EqualTo(back));
        ExecuteEvents.Execute(back.gameObject, data, ExecuteEvents.pointerClickHandler);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(button.gameObject));
    }

    [UnityTest]
    public IEnumerator BossFightKeepsTimerPausedButCombatResumesAfterBack()
    {
        var encounter = Find("BossEncounterController");
        encounter.GetType().GetMethod("BeginBossFight", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(encounter, null);
        var director = Find("EnemySpawnDirector");
        Assert.That(Read<bool>(director, "IsPaused"), Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Call(controller, "ShowPause");
        var popup = Field<Component>(controller, "pausePopup");
        Field<Button>(popup, "closeButton").onClick.Invoke();
        yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Read<bool>(director, "IsPaused"), Is.True);
        Assert.That(Read<object>(encounter, "Phase").ToString(), Is.EqualTo("BossFight"));
    }

    [UnityTest]
    public IEnumerator SceneUnloadReleasesPauseAndSceneOwnedPopupInstances()
    {
        var popup = Field<Component>(controller, "pausePopup");
        var level = Field<Component>(controller, "levelUpPopup");
        Call(controller, "ShowPause");
        yield return SceneManager.UnloadSceneAsync(scene);
        yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(popup == null && level == null, Is.True, "cached scene UI must not survive its owner scene");
    }

    [UnityTest]
    public IEnumerator KoreanLabelsRenderAndLevelContentRemainsEmpty()
    {
        Call(controller, "ShowPause");
        var popup = Field<Component>(controller, "pausePopup");
        var labels = popup.GetComponentsInChildren<TMP_Text>();
        Assert.That(labels.Any(label => label.text == "뒤로가기"), Is.True);
        foreach (var label in labels)
        {
            foreach (char character in label.text.Where(c => !char.IsWhiteSpace(c)))
                Assert.That(label.font.HasCharacter(character, false, true), Is.True, "Missing glyph: " + character);
            label.ForceMeshUpdate();
            Assert.That(label.textInfo.characterCount, Is.GreaterThan(0));
        }
        Call(controller, "ShowLevelUp");
        var content = Read<RectTransform>(controller, "LevelUpContentRoot");
        Assert.That(content, Is.Not.Null);
        Assert.That(content.childCount, Is.Zero, "unplanned rewards must not be fabricated");
        yield return null;
    }

    [UnityTest]
    public IEnumerator PausedPlayerDoesNotStartAutoAttackWhenCooldownReady()
    {
        var player = Find("Player");
        var enemy = new GameObject("paused autoattack target");
        SceneManager.MoveGameObjectToScene(enemy, scene);
        enemy.tag = "Enemy";
        enemy.transform.position = player.transform.position + Vector3.right;
        enemy.AddComponent<BoxCollider2D>();
        Physics2D.SyncTransforms();
        player.GetType().GetField("attackTimer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, 0f);
        Call(controller, "ShowPause");
        player.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
        Assert.That(Field<float>(player, "attackTimer"), Is.Zero, "paused Update must not begin a ready attack");
        yield return null;
    }

    [UnityTest]
    public IEnumerator BringingAnOpenPausePopupToFrontRestoresButtonFocus()
    {
        Call(controller, "ShowPause");
        Call(controller, "ShowLevelUp");
        Call(controller, "ShowPause");
        var popup = Field<Component>(controller, "pausePopup");
        var back = Field<Button>(popup, "closeButton");
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(back.gameObject));
        back.onClick.Invoke();
        Assert.That(Time.timeScale, Is.Zero);
        Call(controller, "CompleteLevelUp");
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        yield return null;
    }
}
#endif
