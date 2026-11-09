using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// 타이틀 → 로딩 → 로비 → 로딩 → 게임 흐름과 각 씬의 UI 배치를 확인한다.
// 런타임 클래스는 Assembly-CSharp에 있어 직접 참조하지 않고 씬 오브젝트 이름과 Unity API만 사용한다.
public sealed class SceneFlowTests
{
    private const float SceneTimeoutSeconds = 30f;

    [UnityTest]
    public IEnumerator TitleStartGoesThroughLoadingToLobby()
    {
        yield return SceneManager.LoadSceneAsync("TitleScene");
        yield return null;

        GameObject background = GameObject.Find("Background");
        Assert.That(background, Is.Not.Null, "타이틀 배경 이미지가 없습니다.");
        Assert.That(background.GetComponent<Image>().sprite, Is.Not.Null);
        Assert.That(GameObject.Find("Text (TMP)"), Is.Null, "임시 'Title Scene' 문구가 남아 있습니다.");

        Click("StartTouchArea");
        yield return WaitForScene("LobbyScene");

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LobbyScene"));
    }

    [UnityTest]
    public IEnumerator LobbyHasProfileTabsAndStartButton()
    {
        yield return SceneManager.LoadSceneAsync("LobbyScene");
        yield return null;

        foreach (string name in new[]
        {
            "StartButton", "ShopTab", "EquipmentTab", "MainTab", "UpgradeTab", "CodexTab",
            "SettingsButton", "MailButton", "NoticeButton",
        })
        {
            GameObject go = GameObject.Find(name);
            Assert.That(go, Is.Not.Null, $"{name} 오브젝트가 없습니다.");
            Assert.That(go.GetComponent<Button>(), Is.Not.Null, $"{name}에 Button이 없습니다.");
        }

        Assert.That(GameObject.Find("PlayerNameText").GetComponent<TMP_Text>().text, Is.EqualTo("플레이어 이름"));
        Assert.That(GameObject.Find("GoldText").GetComponent<TMP_Text>().text, Is.EqualTo("0"));
        Assert.That(GameObject.Find("MainTab").transform.Find("Fill/Label").GetComponent<TMP_Text>().text, Is.EqualTo("메인"));
    }

    [UnityTest]
    public IEnumerator LobbyStartGoesThroughLoadingToMain()
    {
        yield return SceneManager.LoadSceneAsync("LobbyScene");
        yield return null;

        Click("StartButton");
        yield return WaitForScene("MainScene");

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainScene"));
    }

    [UnityTest]
    public IEnumerator LoadingSceneWithoutDestinationStillGoesToMain()
    {
        // 재시작처럼 목적지를 예약하지 않고 로딩 씬을 열면 기존대로 게임 씬으로 간다.
        yield return SceneManager.LoadSceneAsync("LoadScene");

        // 로딩 씬은 Start 이후 곧바로 다음 씬을 불러오므로 로드 직후 같은 프레임에 구성만 확인한다.
        Assert.That(GameObject.Find("TipText"), Is.Not.Null, "로딩 팁 문구가 없습니다.");
        Assert.That(GameObject.Find("Spinner"), Is.Not.Null, "로딩 회전 고리가 없습니다.");

        yield return WaitForScene("MainScene");
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainScene"));
    }

    private static void Click(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        Assert.That(go, Is.Not.Null, $"{objectName} 오브젝트가 없습니다.");
        Assert.That(EventSystem.current, Is.Not.Null, "EventSystem이 없습니다.");

        var data = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
    }

    private static IEnumerator WaitForScene(string sceneName)
    {
        float elapsed = 0f;
        while (SceneManager.GetActiveScene().name != sceneName && elapsed < SceneTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(sceneName),
            $"{SceneTimeoutSeconds}초 안에 {sceneName}에 도착하지 못했습니다.");
        yield return null;
    }
}
