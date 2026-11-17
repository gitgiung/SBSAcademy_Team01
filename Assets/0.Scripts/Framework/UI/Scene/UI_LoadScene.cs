using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private GameObject remotePanel;
    private TMP_Text remoteLabel;
    private TMP_Text actionLabel;
    private Button remoteAction;

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
        UpdateRemoteStatus();
        if (spinner == null)
            return;

        // 로딩 중 Time.timeScale과 무관하게 돌도록 unscaledDeltaTime을 쓴다.
        spinner.Rotate(0f, 0f, -spinnerDegreesPerSecond * Time.unscaledDeltaTime);
    }

    private void UpdateRemoteStatus()
    {
        RemoteContentStatus status = Managers.RemoteStatus;
        bool visible = status.State != RemoteContentState.Disabled && status.State != RemoteContentState.Ready;
        if (!visible)
        {
            if (remotePanel != null) remotePanel.SetActive(false);
            return;
        }
        if (remotePanel == null) CreateRemotePanel();
        remotePanel.SetActive(true);
        bool consent = status.State == RemoteContentState.AwaitingConsent;
        bool failed = status.State == RemoteContentState.Failed;
        remoteAction.gameObject.SetActive(consent || failed);
        remoteAction.interactable = consent || failed;
        actionLabel.text = failed ? "다시 시도" : "다운로드";
        switch (status.State)
        {
            case RemoteContentState.CheckingRelease: remoteLabel.text = "리소스 버전 확인 중"; break;
            case RemoteContentState.LoadingCatalog: remoteLabel.text = "리소스 목록 확인 중"; break;
            case RemoteContentState.CheckingSize: remoteLabel.text = "추가 다운로드 용량 확인 중"; break;
            case RemoteContentState.AwaitingConsent:
                remoteLabel.text = $"추가 리소스 {Megabytes(status.RequiredBytes)} MB\n네트워크 데이터를 사용해 다운로드합니다.";
                break;
            case RemoteContentState.Downloading:
                remoteLabel.text = $"리소스 다운로드 중\n{Megabytes(status.ReceivedBytes)} / {Megabytes(status.RequiredBytes)} MB";
                break;
            case RemoteContentState.Failed:
                string error = status.Error ?? "리소스를 준비하지 못했습니다.";
                remoteLabel.text = error.Length > 250 ? error.Substring(0, 250) + "…" : error;
                break;
        }
    }

    private static string Megabytes(long bytes) => (bytes / 1000000d).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private void CreateRemotePanel()
    {
        // 기존 로컬 TipText의 폰트를 재사용한다. 원격 폰트가 없어서 동의/오류 UI까지 사라지면 안 된다.
        TMP_Text template = GetComponentInChildren<TMP_Text>(true);
        TMP_FontAsset font = template != null ? template.font : TMP_Settings.defaultFontAsset;
        remotePanel = new GameObject("RemoteContentStatus", typeof(RectTransform), typeof(Image));
        RectTransform panel = (RectTransform)remotePanel.transform;
        panel.SetParent(transform, false);
        SetRect(panel, new Vector2(.12f, .25f), new Vector2(.88f, .72f));
        remotePanel.GetComponent<Image>().color = new Color(.04f, .06f, .10f, .96f);
        remoteLabel = CreateLabel(panel, "Status", font, new Vector2(.05f, .35f), new Vector2(.95f, .93f), 32);
        var actionObject = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform actionRect = (RectTransform)actionObject.transform;
        actionRect.SetParent(panel, false);
        SetRect(actionRect, new Vector2(.3f, .06f), new Vector2(.7f, .27f));
        actionObject.GetComponent<Image>().color = new Color(.10f, .36f, .52f, 1f);
        remoteAction = actionObject.GetComponent<Button>();
        remoteAction.targetGraphic = actionObject.GetComponent<Image>();
        actionLabel = CreateLabel(actionRect, "Label", font, Vector2.zero, Vector2.one, 28);
        remoteAction.onClick.AddListener(() =>
        {
            if (Managers.RemoteStatus.State == RemoteContentState.AwaitingConsent) Managers.ApproveRemoteDownload();
            else if (Managers.RemoteStatus.State == RemoteContentState.Failed) Managers.RetryRemoteContent();
        });
    }

    private static TMP_Text CreateLabel(Transform parent, string name, TMP_FontAsset font, Vector2 min, Vector2 max, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        SetRect(rect, min, max);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
