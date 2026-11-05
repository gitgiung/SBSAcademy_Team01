using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StatusPanelUI;

/// <summary>성장 담당자가 제공하는 카드 표시 데이터. 보상 수치나 적용 로직은 소유하지 않습니다.</summary>
[Serializable]
public sealed class LevelUpCardData
{
    public int SkillId;
    public string Name;
    public string Rarity;
    public Color Accent = Color.white;
    public Sprite Icon;
    public string Description;
    public string ActiveDescription;
    public string PassiveDescription;
    public string ActiveButtonLabel = "N";
    public string FlavorText;
}

/// <summary>레벨업 카드 표시 전용 뷰. 선택 효과와 전투 정지는 호출자가 처리합니다.</summary>
public sealed class LevelUpSelectionPanel : MonoBehaviour
{
    private readonly List<Card> cards = new List<Card>();
    private RectTransform root;
    private GameObject detailsRoot;
    private TMP_Text detailsTitle;
    private TMP_Text detailsText;
    private Button detailsClose;
    private Action<int> onSelected;
    private bool selectionPending;
    private GameObject messageRoot;
    private TMP_Text messageTitle;
    private TMP_Text messageText;
    private Button messageConfirm;
    private Action onConfirmed;

    public void Initialize(RectTransform content, TMP_FontAsset font)
    {
        if (root != null) return;
        var panel = (RectTransform)content.parent;
        panel.anchorMin = new Vector2(.025f, .045f);
        panel.anchorMax = new Vector2(.975f, .90f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        var background = panel.GetComponent<Image>();
        if (background != null) background.color = Navy;
        // 교체 대상은 기존 프리팹의 제목뿐입니다. 외부에서 넣은 콘텐츠는 보존합니다.
        foreach (Transform child in panel)
            if (child != content && child.name == "Title") child.gameObject.SetActive(false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = content.offsetMax = Vector2.zero;
        root = Stretch(content, "LevelUpSelection");
        var title = Rect(root, "LevelUpHeading", 375, -26, 250, 50);
        Border(title, new Color32(113, 247, 255, 255), 2);
        var heading = Text(Stretch(title, "Label"), "레벨 업", font, 38, new Color32(153, 248, 255, 255));
        heading.fontStyle = FontStyles.Bold;
        Glow(heading.gameObject, new Color(0, .9f, 1f, .45f), 2);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            cards.Add(new Card(Rect(root, "Choice" + i, 60 + i * 305, 55, 275, 350), font,
                () => Select(index), () => ShowDetails(index)));
        }
        detailsRoot = Stretch(root, "CardDetails").gameObject;
        Image((RectTransform)detailsRoot.transform, new Color(0, 0, 0, .85f), true);
        var detailPanel = Rect(detailsRoot.transform, "Panel", 240, 75, 520, 350);
        Border(detailPanel, new Color32(113, 247, 255, 255), 2);
        detailsTitle = Text(Rect(detailPanel, "Title", 60, 25, 880, 65), "", font, 32, Color.white);
        detailsText = Text(Rect(detailPanel, "Description", 65, 110, 870, 270), "", font, 23, Color.white, TextAlignmentOptions.TopLeft);
        detailsClose = Button(Rect(detailPanel, "Close", 650, 425, 280, 50), "닫기", font,
            new Color32(113, 247, 255, 255), HideDetails);
        detailsRoot.SetActive(false);
        messageRoot = Rect(root, "Message", 260, 100, 480, 270).gameObject;
        Border((RectTransform)messageRoot.transform, new Color32(113, 247, 255, 255), 2);
        messageTitle = Text(Rect(messageRoot.transform, "Title", 60, 50, 880, 90), "", font, 34, Color.white);
        messageText = Text(Rect(messageRoot.transform, "Content", 75, 170, 850, 130), "", font, 24, Color.white);
        messageConfirm = Button(Rect(messageRoot.transform, "Confirm", 350, 370, 300, 65), "확인", font,
            new Color32(113, 247, 255, 255), ConfirmMessage);
        messageRoot.SetActive(false);
        SetChoices(Array.Empty<LevelUpCardData>(), null);
    }

    /// <summary>최대 세 선택지를 표시합니다. 콜백은 실제 효과 적용/팝업 완료를 담당합니다.</summary>
    public void SetChoices(IReadOnlyList<LevelUpCardData> choices, Action<int> selected)
    {
        if (root == null) throw new InvalidOperationException("Initialize must be called before SetChoices.");
        if (choices == null || choices.Count > 3) throw new ArgumentException("Level-up requires at most three cards.", nameof(choices));
        onSelected = selected;
        selectionPending = false;
        detailsRoot.SetActive(false);
        messageRoot.SetActive(false);
        onConfirmed = null;
        for (int i = 0; i < cards.Count; i++)
        {
            LevelUpCardData data = i < choices.Count ? choices[i] : null;
            cards[i].Show(data, selected != null);
        }
    }

    public void Focus()
    {
        if (messageRoot != null && messageRoot.activeSelf) { messageConfirm.Select(); return; }
        if (detailsRoot != null && detailsRoot.activeSelf) { detailsClose.Select(); return; }
        foreach (var card in cards)
            if (card.Root.gameObject.activeSelf) { card.Focus(); return; }
    }

    private void Select(int index)
    {
        if (selectionPending || onSelected == null) return;
        selectionPending = true;
        // 한 선택을 두 번 지급하지 않습니다. 후속 선택은 새 SetChoices로 시작합니다.
        onSelected(index);
    }

    public void ShowMessage(string title, string content, Action confirmed)
    {
        if (root == null) throw new InvalidOperationException("Initialize must be called before ShowMessage.");
        onSelected = null;
        selectionPending = false;
        onConfirmed = confirmed;
        foreach (var card in cards) card.Root.gameObject.SetActive(false);
        detailsRoot.SetActive(false);
        messageTitle.text = title;
        messageText.text = content;
        messageRoot.SetActive(true);
        Focus();
    }

    private void ConfirmMessage()
    {
        var callback = onConfirmed;
        onConfirmed = null;
        callback?.Invoke();
    }

    private void ShowDetails(int index)
    {
        var data = cards[index].Data;
        if (data == null) return;
        detailsTitle.text = data.Name;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(data.Description)) parts.Add(data.Description);
        if (!string.IsNullOrEmpty(data.ActiveDescription)) parts.Add("액티브 " + data.ActiveButtonLabel + "\n" + data.ActiveDescription);
        if (!string.IsNullOrEmpty(data.PassiveDescription)) parts.Add("패시브\n" + data.PassiveDescription);
        if (!string.IsNullOrEmpty(data.FlavorText)) parts.Add(data.FlavorText);
        detailsText.text = string.Join("\n\n", parts);
        detailsRoot.SetActive(true);
        detailsRoot.transform.SetAsLastSibling();
        detailsClose.Select();
    }

    private void HideDetails() { detailsRoot.SetActive(false); Focus(); }
    private void OnDisable() { if (detailsRoot != null) detailsRoot.SetActive(false); }

    private static void Glow(GameObject target, Color color, float width)
    {
        var glow = target.AddComponent<Outline>();
        glow.effectColor = color;
        glow.effectDistance = new Vector2(width, -width);
    }

    private static void Border(RectTransform rect, Color color, float width)
    {
        Image(rect, Navy, true);
        // 네 변은 직사각형 안에 배치하여 해상도에 따라 같은 비율로 늘어납니다.
        Image(Rect(rect, "Top", 0, 0, 1000, width * 2), color);
        Image(Rect(rect, "Bottom", 0, 500 - width * 2, 1000, width * 2), color);
        Image(Rect(rect, "Left", 0, 0, width * 3, 500), color);
        Image(Rect(rect, "Right", 1000 - width * 3, 0, width * 3, 500), color);
        Glow(rect.gameObject, new Color(color.r, color.g, color.b, .4f), 2);
    }

    private sealed class Card
    {
        internal readonly RectTransform Root;
        internal readonly Button Info;
        internal LevelUpCardData Data { get; private set; }
        private readonly Button select;
        private readonly Image icon;
        private readonly TMP_Text name;
        private readonly TMP_Text rarity;
        private readonly TMP_Text description;
        private readonly TMP_Text active;
        private readonly TMP_Text passive;
        private readonly TMP_Text flavor;
        private readonly TMP_Text activeTag;
        private readonly TMP_Text passiveTag;
        private readonly List<Image> edges = new List<Image>();
        private readonly List<Image> iconEdges = new List<Image>();

        internal Card(RectTransform rect, TMP_FontAsset font, UnityEngine.Events.UnityAction choose, UnityEngine.Events.UnityAction info)
        {
            Root = rect;
            Border(rect, Color.white, 3);
            foreach (Transform child in rect)
                if (child.TryGetComponent<Image>(out var edge)) edges.Add(edge);
            select = rect.gameObject.AddComponent<Button>();
            select.targetGraphic = rect.GetComponent<Image>();
            select.transition = Selectable.Transition.None;
            select.onClick.AddListener(choose);
            var iconRoot = Rect(rect, "IconFrame", 405, 12, 190, 70);
            Border(iconRoot, Color.white, 2);
            foreach (Transform child in iconRoot)
                if (child.TryGetComponent<Image>(out var edge)) iconEdges.Add(edge);
            icon = Image(Rect(iconRoot, "Icon", 75, 35, 850, 430), Color.white);
            icon.type = UnityEngine.UI.Image.Type.Simple;
            icon.preserveAspect = true;
            name = Text(Rect(rect, "Name", 55, 95, 890, 55), "", font, 35);
            name.fontStyle = FontStyles.Bold;
            Glow(name.gameObject, new Color(1, 1, 1, .25f), 2);
            rarity = Text(Rect(rect, "Rarity", 100, 157, 800, 28), "", font, 16);
            rarity.fontStyle = FontStyles.Bold;
            Info = Button(Rect(rect, "Info", 915, 10, 65, 28), "i", font, Navy, info);
            var infoOutline = Info.gameObject.AddComponent<Outline>();
            infoOutline.effectColor = new Color32(105, 190, 28, 255);
            infoOutline.effectDistance = Vector2.one;
            Info.GetComponentInChildren<TMP_Text>().color = new Color32(105, 230, 50, 255);
            description = Text(Rect(rect, "Description", 80, 220, 840, 210), "", font, 20, Color.white, TextAlignmentOptions.TopLeft);
            activeTag = Tag(rect, "Active", 190, font);
            active = Text(Rect(rect, "ActiveDescription", 80, 245, 840, 88), "", font, 19, Color.white, TextAlignmentOptions.TopLeft);
            passiveTag = Tag(rect, "Passive", 335, font);
            passive = Text(Rect(rect, "PassiveDescription", 80, 390, 840, 80), "", font, 19, Color.white, TextAlignmentOptions.TopLeft);
            flavor = Text(Rect(rect, "Flavor", 65, 475, 900, 18), "", font, 11, new Color32(191, 190, 203, 255), TextAlignmentOptions.Right);
        }

        private static TMP_Text Tag(Transform parent, string name, float y, TMP_FontAsset font)
        {
            var tag = Rect(parent, name + "Tag", 45, y, name == "Active" ? 320 : 245, 40);
            Border(tag, new Color32(145, 250, 255, 255), 3);
            return Text(Stretch(tag, "Label"), "", font, 20, new Color32(145, 250, 255, 255));
        }

        internal void Show(LevelUpCardData data, bool selectable)
        {
            Data = data;
            Root.gameObject.SetActive(data != null);
            if (data == null) return;
            foreach (var edge in edges) edge.color = data.Accent;
            foreach (var edge in iconEdges) edge.color = data.Accent;
            icon.sprite = data.Icon;
            icon.enabled = data.Icon != null;
            name.text = data.Name;
            name.color = rarity.color = data.Accent;
            rarity.text = data.Rarity;
            description.text = data.Description;
            active.text = data.ActiveDescription;
            passive.text = data.PassiveDescription;
            activeTag.text = "액티브 " + data.ActiveButtonLabel;
            passiveTag.text = "패시브";
            activeTag.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(data.ActiveDescription));
            passiveTag.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(data.PassiveDescription));
            flavor.text = data.FlavorText;
            select.interactable = selectable;
        }

        internal void Focus()
        {
            if (select.interactable) select.Select();
            else Info.Select();
        }
    }

}
