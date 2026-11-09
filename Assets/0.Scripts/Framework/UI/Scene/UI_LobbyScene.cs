using TMPro;
using UnityEngine;
using static Define;

/// <summary>
/// 로비 화면. 기획서(씬 별 UI)의 로비 구성 중 게임 시작만 동작한다.
/// 하단 탭(상점·장비·업그레이드·도감)과 우상단 아이콘(설정·우편·알림)은 대상 화면이
/// 정해지지 않아 배치만 했고 눌러도 아무 일도 일어나지 않는다.
/// </summary>
public class UI_LobbyScene : UI_Scene
{
    enum Buttons
    {
        StartButton,
        ShopTab,
        EquipmentTab,
        MainTab,
        UpgradeTab,
        CodexTab,
        SettingsButton,
        MailButton,
        NoticeButton,
    }

    enum Texts
    {
        PlayerNameText,
        GoldText,
    }

    private bool isStarting;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButtons(typeof(Buttons));
        BindTexts(typeof(Texts));

        GameObject startButton = GetButton((int)Buttons.StartButton)?.gameObject;
        if (startButton != null)
            startButton.BindEvent(evt => OnClickStart());

        return true;
    }

    /// <summary>프로필 영역의 이름과 골드를 표시한다. 플레이어 데이터가 생기면 여기에 연결한다.</summary>
    public void SetProfile(string playerName, long gold)
    {
        TMP_Text nameText = GetText((int)Texts.PlayerNameText);
        if (nameText != null)
            nameText.text = playerName;

        TMP_Text goldText = GetText((int)Texts.GoldText);
        if (goldText != null)
            goldText.text = gold.ToString("N0");
    }

    private void OnClickStart()
    {
        if (isStarting)
            return;

        isStarting = true;

        try
        {
            // 로비 → 로딩 → 게임. 로딩 씬이 준비 상태를 확인한 뒤 MainScene을 연다.
            Managers.Scene.LoadSceneThroughLoading(EScene.MainScene);
        }
        catch
        {
            // 씬 등록 오류를 고친 뒤 다시 누를 수 있게 한다.
            isStarting = false;
            throw;
        }
    }
}
