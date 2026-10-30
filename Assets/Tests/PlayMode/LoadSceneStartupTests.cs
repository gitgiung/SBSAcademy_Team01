using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LoadSceneStartupTests
{
    [UnityTest]
    public IEnumerator EmptyLoadingSceneReachesMainTestSceneWithCoreReady()
    {
        yield return SceneManager.LoadSceneAsync("LoadTestScene");
        float deadline = Time.realtimeSinceStartup + 10f;
        while (SceneManager.GetActiveScene().name != "MainTestScene" && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainTestScene"));
        Type managers = Type.GetType("Managers, Assembly-CSharp", true);
        Assert.That(managers.GetProperty("Initialized").GetValue(null), Is.True);
        Assert.That(managers.GetProperty("PreparationState").GetValue(null).ToString(), Is.EqualTo("TestReady"));
        Assert.That(managers.GetProperty("GameDataReady").GetValue(null), Is.False);
        Assert.That(GameObject.Find("@Managers"), Is.Not.Null);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        UnityEngine.Object.Destroy(GameObject.Find("@Managers"));
        Scene scene = SceneManager.GetActiveScene();
        Scene empty = SceneManager.CreateScene("CoreStartupTestCleanup");
        SceneManager.SetActiveScene(empty);
        yield return SceneManager.UnloadSceneAsync(scene);
        Time.timeScale = 1f;
    }
}
