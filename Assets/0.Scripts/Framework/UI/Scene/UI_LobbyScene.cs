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

        // 임시: 도감 탭 = SoulArena3D 본선 파이프라인 진입(마감 전 검증용).
        // 도감 화면이 정해지면 이 바인딩을 정식 화면으로 바꾼다.
        GameObject soulArenaButton = GetButton((int)Buttons.CodexTab)?.gameObject;
        if (soulArenaButton != null)
            soulArenaButton.BindEvent(evt => OnClickSoulArena());

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

    private void OnClickSoulArena()
    {
        if (isStarting)
            return;

        isStarting = true;
        try
        {
            Managers.Scene.LoadSceneThroughLoading(EScene.SoulArena3D);
        }
        catch
        {
            isStarting = false;
            Debug.LogError("[SoulPipeline] SoulArena3D 로드 실패. Build Settings에 씬이 있는지 확인.");
            throw;
        }
    }

    private void Update()
    {
        // 키보드 단축: F6 = SoulArena3D (버튼 바인딩이 없을 때 대비)
        if (!isStarting && Input.GetKeyDown(KeyCode.F6))
            OnClickSoulArena();
    }
}
