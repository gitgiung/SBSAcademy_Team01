using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StatusPanelUI;

/// <summary>MainScene 전용 부활/결과 표시. 진행 상태와 부활 규칙은 GameManager/Player가 소유합니다.</summary>
[DisallowMultipleComponent]
public sealed class RunEndPopup : MonoBehaviour
{
    private GameManager owner;
    private TMP_FontAsset font;
    private RectTransform root;
    private RectTransform safeArea;
    private GameObject revive;
    private GameObject result;
    private GameObject adNotice;
    private TMP_Text statistics;
    private RectTransform inventory;
    private Button yes;
    private Button retry;
    private Button adConfirm;
    private readonly Color textColor = new Color32(221, 232, 238, 255);
    private readonly Color frameColor = new Color32(78, 109, 126, 255);
    private readonly Color panelColor = new Color32(25, 35, 48, 255);
    private readonly Color accent = new Color32(42, 112, 126, 255);
    private readonly Color secondary = new Color32(54, 70, 88, 255);

    public void Initialize(GameManager manager, GameObject oldPanel)
    {
        owner = manager;
        if (oldPanel != null)
            foreach (TMP_Text label in oldPanel.GetComponentsInChildren<TMP_Text>(true))
                if (label.font != null) { font = label.font; break; }
        // 영문 전용 구 결과창보다 전투 UI의 한글 폰트를 우선합니다.
        foreach (TMP_Text label in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
            if (label.gameObject.scene == gameObject.scene && label.font != null && label.font.name.Contains("Nanum"))
            { font = label.font; break; }
        var go = new GameObject("RunEndCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        root = Stretch(go.transform, "RunEndOverlay");
        var dim = Image(root, new Color(0.035f, 0.025f, .10f, .78f), true);
        dim.sprite = null;
        safeArea = Stretch(root, "SafeArea");
        BuildRevive();
        BuildResult();
        BuildAdNotice();
        Hide();
    }

    private void LateUpdate()
    {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        UnityEngine.Rect area = Screen.safeArea;
        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
    }

    private RectTransform Panel(string name, float left, float top, float width, float height)
    {
        var panel = Rect(safeArea, name, left, top, width, height);
        Image(panel, frameColor, true);
        var inner = Rect(panel, "Surface", 7, 4, 986, 492);
        Image(inner, panelColor);
        return inner;
    }

    private TMP_Text Label(Transform parent, string name, string value, float x, float y, float w, float h, float size)
        => Text(Rect(parent, name, x, y, w, h), value, font, size, textColor);

    private Button Action(Transform parent, string name, string value, float x, float y, float w, float h,
        Color color, UnityEngine.Events.UnityAction action)
    {
        var button = Button(Rect(parent, name, x, y, w, h), value, font, color, action);
        button.GetComponentInChildren<TMP_Text>().color = textColor;
        return button;
    }

    private void BuildRevive()
    {
        var panel = Panel("RevivePopup", 310, 30, 380, 440);
        revive = panel.parent.gameObject;
        Label(panel, "Eyebrow", "REVIVE", 80, 15, 840, 24, 15);
        var emblem = Image(Rect(panel, "Angel", 145, 42, 710, 145), Color.white);
        emblem.sprite = Resources.Load<Sprite>("UI/ReviveAngel");
        emblem.type = UnityEngine.UI.Image.Type.Simple;
        emblem.preserveAspect = true;
        Label(panel, "Title", "부활하시겠습니까?", 40, 200, 920, 42, 31);
        Label(panel, "Prototype", "프로토타입 · HP 100% 회복\n부활권은 소모하지 않습니다", 60, 247, 880, 40, 15);
        yes = Action(panel, "Yes", "예", 110, 307, 780, 49, accent, ReviveNow);
        Action(panel, "AdRevive", "광고 후 부활", 110, 369, 780, 49, secondary, ShowAdNotice);
        Action(panel, "No", "아니요", 300, 436, 400, 38, secondary, owner.DeclineRevive);
    }

    private void BuildResult()
    {
        var panel = Panel("GameOverPopup", 115, 40, 770, 420);
        result = panel.parent.gameObject;
        Label(panel, "Title", "GAME OVER", 50, 20, 900, 65, 48);
        Image(Rect(panel, "Divider", 85, 99, 830, 2), accent);
        statistics = Label(panel, "Statistics", "", 85, 115, 830, 115, 24);
        Label(panel, "InventoryTitle", "획득 유물 / 증강 / 스킬", 85, 239, 830, 25, 20);
        var viewport = Rect(panel, "Inventory", 85, 273, 830, 90);
        Image(viewport, new Color32(17, 25, 36, 255), true);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        inventory = Stretch(viewport, "Content");
        inventory.anchorMin = new Vector2(0, 1);
        inventory.anchorMax = Vector2.one;
        inventory.pivot = new Vector2(.5f, 1);
        var grid = inventory.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(78, 78);
        grid.spacing = new Vector2(8, 8);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 10;
        var fitter = inventory.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = inventory;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        Label(panel, "RetryQuestion", "다시 시작하시겠습니까?", 85, 377, 830, 34, 26);
        Action(panel, "ReturnToTitle", "아니요", 180, 434, 280, 43, secondary, owner.ReturnToTitle);
        retry = Action(panel, "Restart", "예", 540, 434, 280, 43, accent, owner.RestartGame);
    }

    private void BuildAdNotice()
    {
        var blocker = Stretch(safeArea, "NoAdNotice");
        Image(blocker, new Color(0, 0, 0, .65f), true);
        adNotice = blocker.gameObject;
        var panel = Rect(blocker, "Message", 315, 150, 370, 200);
        Image(panel, panelColor, true);
        Label(panel, "Title", "광고 없음", 50, 50, 900, 90, 32);
        Label(panel, "Message", "아직 연결된 광고가 없습니다.\n확인을 누르면 부활합니다.\n(프로토타입)", 60, 160, 880, 150, 18);
        adConfirm = Action(panel, "Confirm", "확인", 200, 365, 600, 85, accent, ReviveNow);
    }

    public void ShowRevive()
    {
        root.gameObject.SetActive(true);
        revive.SetActive(true);
        result.SetActive(false);
        adNotice.SetActive(false);
        yes.Select();
    }

    private void ShowAdNotice()
    {
        if (owner.state != GameState.ReviveOffer) return;
        revive.SetActive(false);
        adNotice.SetActive(true);
        adConfirm.Select();
    }

    private void ReviveNow()
    {
        if (!owner.TryPrototypeRevive()) Debug.LogWarning("부활할 Player 또는 사망 상태를 확인하세요.", this);
    }

    public void ShowGameOver()
    {
        root.gameObject.SetActive(true);
        revive.SetActive(false);
        adNotice.SetActive(false);
        result.SetActive(true);
        var data = owner.RunPlayer != null ? owner.RunPlayer.data : null;
        int kills = 0;
        foreach (EnemySpawnDirector director in FindObjectsByType<EnemySpawnDirector>())
            if (director.gameObject.scene == gameObject.scene) kills += director.TotalKills;
        statistics.text = $"처치 몬스터 (일반)    {kills:N0} 마리                  점수    미정\n"
            + $"생존 시간    {EncounterTimerView.FormatTime(owner.SurvivalSeconds)}                  획득 골드    미정\n"
            + $"달성 레벨    {(data != null ? data.CurrentLevel : 0)}";
        foreach (Transform child in inventory) Destroy(child.gameObject);
        if (data != null) FillInventory(data.Loadout);
        inventory.anchoredPosition = Vector2.zero;
        retry.Select();
    }

    private void FillInventory(PlayerLoadout loadout)
    {
        int count = 0;
        foreach (PlayerOwnedItem item in loadout.Relics) { AddItem(item.Name, item.Icon); count++; }
        foreach (PlayerOwnedItem item in loadout.Augments) { AddItem(item.Name, item.Icon); count++; }
        for (int i = 0; i < PlayerLoadout.AttackCapacity; i++)
        {
            var skill = loadout.GetAttackSkill(i);
            if (skill == null) continue;
            AddItem(skill.Name, SkillCardIcons.Get(skill.DataId, skill.IconLabel)); count++;
        }
        for (int i = 0; i < PlayerLoadout.TacticalCapacity; i++)
        {
            var skill = loadout.GetTacticalSkill(i);
            if (skill == null) continue;
            AddItem(skill.Name, SkillCardIcons.Get(skill.DataId, skill.IconLabel)); count++;
        }
        if (count == 0) AddItem("없음", null);
    }

    private void AddItem(string name, Sprite sprite)
    {
        var cell = new Cell(Rect(inventory, "OwnedItem", 0, 0, 100, 100), font);
        cell.Show(name, sprite);
    }

    public void Hide()
    {
        if (root != null) root.gameObject.SetActive(false);
    }
}
