using static Define;

public class UI_TitleScene : UI_Scene
{
    enum Buttons
    {
        StartTouchArea,
    }

    private bool isStarting;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindButtons(typeof(Buttons));

        GetButton((int)Buttons.StartTouchArea)
            .gameObject.BindEvent(evt => OnTouchStart());

        return true;
    }

    private void OnTouchStart()
    {
        if (isStarting)
            return;

        isStarting = true;

        // 타이틀 → 로딩 → 로비. 로딩 씬이 게임 준비를 마친 뒤 로비를 연다.
        Managers.Scene.LoadSceneThroughLoading(EScene.LobbyScene);
    }
}