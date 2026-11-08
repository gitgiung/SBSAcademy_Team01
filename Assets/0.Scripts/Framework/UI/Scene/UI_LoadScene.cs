using UnityEngine;

/// <summary>로딩 씬 화면. 팁 문구는 씬에 배치된 텍스트를 그대로 쓰고, 회전 고리만 돌린다.</summary>
public class UI_LoadScene : UI_Scene
{
    enum Objects
    {
        Spinner,
    }

    [SerializeField, Tooltip("회전 고리의 초당 회전 각도입니다. 시계 방향이 양수입니다.")]
    private float spinnerDegreesPerSecond = 180f;

    private Transform spinner;

    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        BindObjects(typeof(Objects));

        GameObject spinnerObject = GetObject((int)Objects.Spinner);
        spinner = spinnerObject != null ? spinnerObject.transform : null;

        return true;
    }

    private void Update()
    {
        if (spinner == null)
            return;

        // 로딩 중 Time.timeScale과 무관하게 돌도록 unscaledDeltaTime을 쓴다.
        spinner.Rotate(0f, 0f, -spinnerDegreesPerSecond * Time.unscaledDeltaTime);
    }
}
