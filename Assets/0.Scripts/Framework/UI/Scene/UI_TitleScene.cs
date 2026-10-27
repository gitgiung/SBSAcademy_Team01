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

        Managers.Scene.LoadScene(EScene.LoadTestScene);
    }
}