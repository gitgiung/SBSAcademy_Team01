using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PlayerCameraTests
{
    private const string ScenePath = "Assets/9.Scenes/MainTestScene.unity";

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        yield return null;
    }

    [UnityTest]
    public IEnumerator MainScene_KeepsMovingAndRotatingPlayerAtCenter()
    {
        yield return SceneManager.LoadSceneAsync(ScenePath);
        yield return null;
        Camera camera = Camera.main;
        Assert.That(camera, Is.Not.Null);
        Assert.That(camera.GetComponent("CinemachineBrain"), Is.Not.Null,
            "Main Camera must be driven by Cinemachine, replacing CameraPlayer.");
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        foreach (MonoBehaviour behaviour in player.GetComponents<MonoBehaviour>())
            behaviour.enabled = false;

        AssertCentered(camera, player);
        Vector3[] positions = { new Vector3(3, -2, 0), new Vector3(-18, 24, 0), Vector3.zero };
        foreach (Vector3 position in positions)
        {
            player.SetPositionAndRotation(position, Quaternion.Euler(0, 0, 73));
            yield return null;
            AssertCentered(camera, player);
        }

        Time.timeScale = 0f;
        yield return null;
        AssertCentered(camera, player);
        Time.timeScale = 1f;
        player.position = new Vector3(7, 9, 0);
        yield return null;
        AssertCentered(camera, player);
    }

    [UnityTest]
    public IEnumerator MainScene_ReentryCreatesOneBrainAndResumesTracking()
    {
        for (int entry = 0; entry < 2; entry++)
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Camera camera = Camera.main;
            Assert.That(camera.GetComponent("CinemachineBrain"), Is.Not.Null);
            int brains = 0;
            foreach (Camera candidate in Object.FindObjectsByType<Camera>())
                if (candidate.GetComponent("CinemachineBrain") != null) brains++;
            Assert.That(brains, Is.EqualTo(1));
            Transform player = GameObject.FindGameObjectWithTag("Player").transform;
            AssertCentered(camera, player);
        }
    }

    private static void AssertCentered(Camera camera, Transform player)
    {
        Vector3 viewport = camera.WorldToViewportPoint(player.position);
        Assert.That(viewport.x, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(viewport.z, Is.EqualTo(10f).Within(0.001f));
        Assert.That(Quaternion.Angle(camera.transform.rotation, Quaternion.identity), Is.LessThan(0.001f));
        Assert.That(camera.orthographic, Is.True);
        Assert.That(camera.orthographicSize, Is.EqualTo(10f).Within(0.001f));
    }
}

