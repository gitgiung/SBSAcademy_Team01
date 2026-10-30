#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class GameResultFlowTests
{
    private Scene scene;
    private Component game;
    private Component ui;
    private Component player;
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Service(string name) => Runtime("Managers").GetProperty(name).GetValue(null);
    private Component Find(string name) => scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<MonoBehaviour>(true))
        .Single(c => c != null && c.GetType().Name == name);
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private string State => game.GetType().GetProperty("state").GetValue(game).ToString();
    private IEnumerator Finish(bool death)
    {
        Call(Field<Component>(game, death ? "deathSource" : "victorySource"), death ? "TriggerDeath" : "TriggerBossDefeated");
        yield return null;
    }

    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/9.Scenes/GiungTestScene.unity", new LoadSceneParameters(LoadSceneMode.Single));
        scene = SceneManager.GetSceneByName("GiungTestScene");
        yield return null;
        game = Find("GameManager");
        ui = Find("CombatPopupController");
        player = Find("Player");
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        var current = SceneManager.GetActiveScene();
        var empty = SceneManager.CreateScene("result cleanup");
        SceneManager.SetActiveScene(empty);
        if (current.IsValid() && current.isLoaded) yield return SceneManager.UnloadSceneAsync(current);
        Object.Destroy(GameObject.Find("@Managers"));
        Object.Destroy(GameObject.Find("@UI_Root"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ActualPlayerDeathReachesDefeat()
    {
        Call(player, "TakeDamage", 999999f, null);
        yield return null;
        Assert.That(State, Is.EqualTo("Defeat"));
        Assert.That(Field<GameObject>(game, "gameOverPanel").activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
    }

    [UnityTest]
    public IEnumerator VictoryClosesBothPopupsAndFocusesResult()
    {
        Call(ui, "ShowPause");
        Call(ui, "ShowLevelUp");
        yield return Finish(false);
        Assert.That(State, Is.EqualTo("Victory"));
        Assert.That(Field<Component>(ui, "pausePopup").gameObject.activeSelf, Is.False);
        Assert.That(Field<Component>(ui, "levelUpPopup").gameObject.activeSelf, Is.False);
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(0));
        Assert.That(Time.timeScale, Is.Zero);
        var panel = Field<GameObject>(game, "victoryPanel");
        Assert.That(panel.activeSelf, Is.True);
        Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel.transform), Is.True);
        Assert.That(Call(ui, "ShowPause"), Is.Null);
        Call(ui, "CompleteLevelUp");
        Assert.That(Time.timeScale, Is.Zero);
    }

    [UnityTest]
    public IEnumerator DeathWinsWhenBothSignalsArriveInSameFrame()
    {
        Call(game, "HandleBossDefeated");
        Call(Field<Component>(game, "deathSource"), "TriggerDeath");
        yield return null;
        Assert.That(State, Is.EqualTo("Defeat"));
        Call(game, "HandleBossDefeated");
        yield return null;
        Assert.That(State, Is.EqualTo("Defeat"));
    }

    [UnityTest]
    public IEnumerator RestartReturnsToGiungWithFreshStateAndOwnedManagers()
    {
        var original = game;
        var resources = Service("Resource");
        yield return Finish(true);
        Call(game, "RestartGame");
        yield return null;
        scene = SceneManager.GetSceneByName("GiungTestScene");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GiungTestScene"));
        game = Find("GameManager");
        Assert.That(original == null, Is.True);
        Assert.That(State, Is.EqualTo("Playing"));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Field<GameObject>(game, "gameOverPanel").activeSelf, Is.False);
        Assert.That(Service("Resource"), Is.SameAs(resources));
        Assert.That(Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include).Count(go => go.name == "@Managers"), Is.EqualTo(1));
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator ReturnToTitleCleansPopupsPoolAndSound()
    {
        var director = Find("EnemySpawnDirector");
        Call(director, "Tick", 5f);
        var pool = Find("SceneEnemyPool");
        Assert.That((int)pool.GetType().GetProperty("ActiveEnemyCount").GetValue(pool), Is.GreaterThan(0));
        Call(ui, "ShowLevelUp");
        var oldPopup = Field<Component>(ui, "levelUpPopup");
        var source = GameObject.Find("@SoundRoot").GetComponentInChildren<AudioSource>();
        var clip = AudioClip.Create("result sound", 4410, 1, 44100, false);
        source.clip = clip;
        Call(game, "ReturnToTitle");
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("TitleTestScene"));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(oldPopup == null, Is.True);
        Assert.That(source.clip, Is.Null);
        Assert.That(Call(Service("UI"), "GetPopupCount"), Is.EqualTo(0));
        Assert.That(Object.FindObjectsByType<MonoBehaviour>().Any(c => c != null && c.GetType().Name == "EnemyHealth"), Is.False);
        Object.Destroy(clip);
    }

    [UnityTest]
    public IEnumerator InvalidRestartPreservesResultAndAllowsRetry()
    {
        yield return Finish(true);
        var field = game.GetType().GetField("restartScene", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null);
        field.SetValue(game, Enum.Parse(Runtime("Define+EScene"), "Unknown"));
        Assert.Throws<TargetInvocationException>(() => Call(game, "RestartGame"));
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(Field<bool>(game, "isTransitioning"), Is.False);
        Assert.That(Field<GameObject>(game, "gameOverPanel").activeSelf, Is.True);
        field.SetValue(game, Enum.Parse(Runtime("Define+EScene"), "GiungTestScene"));
        Call(game, "RestartGame");
        yield return null;
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("GiungTestScene"));
    }

    [UnityTest]
    public IEnumerator RestoringLoadingRouteReachesMainWithPreparationReady()
    {
        yield return Finish(true);
        game.GetType().GetField("restartScene", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(game, Enum.Parse(Runtime("Define+EScene"), "LoadTestScene"));
        Call(game, "RestartGame");
        float deadline = Time.realtimeSinceStartup + 10f;
        while (SceneManager.GetActiveScene().name != "MainTestScene" && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainTestScene"));
        Assert.That(Runtime("Managers").GetProperty("PreparationState").GetValue(null).ToString(), Is.EqualTo("TestReady"));
        Assert.That(Time.timeScale, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator ActualDeathNotificationIsSentOnce()
    {
        var died = player.GetType().GetEvent("Died");
        Assert.That(died, Is.Not.Null);
        int notifications = 0;
        Action listener = () => notifications++;
        died.AddEventHandler(player, listener);
        try
        {
            Call(player, "TakeDamage", 999999f, null);
            Call(player, "TakeDamage", 999999f, null);
            Assert.That(notifications, Is.EqualTo(1));
            yield return null;
            Assert.That(State, Is.EqualTo("Defeat"));
        }
        finally { died.RemoveEventHandler(player, listener); }
    }
}
#endif
