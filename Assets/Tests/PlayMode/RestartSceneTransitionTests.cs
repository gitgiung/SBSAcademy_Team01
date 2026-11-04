#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RestartSceneTransitionTests
{
    private static Component Find(string name) => Object.FindObjectsByType<MonoBehaviour>()
        .Single(component => component != null && component.GetType().Name == name);
    private static object Call(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Public | BindingFlags.Instance).Invoke(target, args);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("restart transition cleanup"));
        if (current.IsValid() && current.isLoaded) yield return SceneManager.UnloadSceneAsync(current);
        Object.Destroy(GameObject.Find("@Managers"));
        Object.Destroy(GameObject.Find("@UI_Root"));
        yield return null;
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator RestartButtonAfterSpawningEnemiesCanReloadThreeTimes()
    {
        Time.timeScale = 1f;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            "Assets/9.Scenes/MainTestScene.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        for (int cycle = 0; cycle < 3; cycle++)
        {
            Component director = Find("EnemySpawnDirector");
            Call(director, "Tick", 5f);
            Component pool = Find("SceneEnemyPool");
            Assert.That((int)pool.GetType().GetProperty("ActiveEnemyCount").GetValue(pool), Is.GreaterThan(0));
            Component enemy = Object.FindObjectsByType<MonoBehaviour>().First(component =>
                component != null && component.GetType().Name == "EnemyHealth");
            // 스폰된 SpriteRenderer가 렌더러와 에셋 변경 알림에 등록된 상태를 검증합니다.
            yield return null;
            Component game = Find("GameManager");
            Component player = Find("Player");
            Call(player, "TakeDamage", 999999f, null);
            yield return null;
            Assert.That(Time.timeScale, Is.Zero);
            Button restart = Object.FindObjectsByType<Button>().Single(button =>
                Enumerable.Range(0, button.onClick.GetPersistentEventCount()).Any(index =>
                    button.onClick.GetPersistentMethodName(index) == "RestartGame"));
            Assert.That(restart.IsInteractable(), Is.True);
            Debug.Log("[RestartRegression] request " + cycle);
            restart.onClick.Invoke();
            // 이전 씬은 유지하면서, 풀에서 예약한 파괴를 끝낼 프레임이 필요합니다.
            yield return null;
            Assert.That(enemy == null, Is.True, "새 씬 로딩 전에 이전 풀의 스프라이트가 제거돼야 합니다.");
            Assert.That(game != null, Is.True, "삭제 처리 프레임까지 이전 씬을 유지해야 합니다.");
            float deadline = Time.realtimeSinceStartup + 15f;
            while (game != null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game == null, Is.True, "이전 게임 씬이 제한 시간 안에 제거돼야 합니다.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainTestScene"));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return null;
            Debug.Log("[RestartRegression] completed " + cycle);
        }
    }
}
#endif
