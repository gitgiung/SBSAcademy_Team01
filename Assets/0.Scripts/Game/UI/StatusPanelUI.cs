using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>상태창의 uGUI 배치와 재사용 가능한 표시 셀. 기준 도안은 1000x500입니다.</summary>
internal static class StatusPanelUI
{
    internal static readonly Color Navy = new Color32(10, 10, 35, 255);
    internal static readonly Color Ink = new Color32(13, 42, 49, 255);
    private static Sprite rounded;
    internal static Sprite Rounded
    {
        get
        {
            if (rounded != null) return rounded;
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - Mathf.Clamp(x, 8, 23), dy = y - Mathf.Clamp(y, 8, 23);
                    texture.SetPixel(x, y, new Color(1, 1, 1, dx * dx + dy * dy <= 64 ? 1 : 0));
                }
            texture.Apply();
            rounded = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 100, 0,
                SpriteMeshType.FullRect, Vector4.one * 8);
            rounded.hideFlags = HideFlags.HideAndDontSave;
            return rounded;
        }
    }
    internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(x / 1000f, 1f - (y + height) / 500f);
        rect.anchorMax = new Vector2((x + width) / 1000f, 1f - y / 500f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
    internal static RectTransform Stretch(Transform parent, string name)
    {
        return Rect(parent, name, 0, 0, 1000, 500);
    }
    internal static Image Image(RectTransform rect, Color color, bool raycast = false)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = Rounded;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }
    internal static TMP_Text Text(RectTransform rect, string value, TMP_FontAsset font, float size = 28,
        Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(12, size);
        text.fontSizeMax = size;
        text.color = color ?? Color.black;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }
    internal static Button Button(RectTransform rect, string label, TMP_FontAsset font, Color color, UnityAction action)
    {
        var image = Image(rect, color, true);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Text(Stretch(rect, "Label"), label, font, 30);
        return button;
    }
    internal sealed class Cell
    {
        internal readonly RectTransform Rect;
        private readonly Image background;
        private readonly Image icon;
        private readonly TMP_Text label;
        internal Cell(RectTransform rect, TMP_FontAsset font)
        {
            Rect = rect;
            background = Image(rect, new Color32(231, 232, 231, 255));
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(1, -1);
            icon = Image(StatusPanelUI.Rect(rect, "Icon", 100, 30, 800, 350), Color.white);
            icon.type = UnityEngine.UI.Image.Type.Simple;
            icon.preserveAspect = true;
            label = Text(StatusPanelUI.Rect(rect, "Name", 45, 20, 910, 460), "", font, 19);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
        internal void Show(string name, Sprite sprite = null, bool locked = false)
        {
            background.color = locked ? new Color32(47, 51, 66, 255) : new Color32(231, 232, 231, 255);
            icon.sprite = sprite;
            icon.enabled = sprite != null && !locked;
            label.text = locked ? "잠김" : name ?? "";
            label.color = locked ? new Color32(158, 166, 182, 255) : Ink;
            label.rectTransform.anchorMin = new Vector2(.045f, .04f);
            label.rectTransform.anchorMax = new Vector2(.955f, sprite != null ? .25f : .96f);
        }
    }
    internal sealed class ItemRow
    {
        private readonly ScrollRect scroll;
        private readonly RectTransform viewport;
        private readonly RectTransform content;
        private readonly TMP_FontAsset font;
        private readonly List<Cell> cells = new List<Cell>();
        private int count = 8;
        private int lastCount = -1;
        private float lastWidth = -1;
        internal ItemRow(Transform parent, string name, float y, TMP_FontAsset font)
        {
            this.font = font;
            var root = Rect(parent, name, 172, y, 565, 60);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            viewport = Stretch(root, "Viewport");
            Image(viewport, new Color(0, 0, 0, .001f), true);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Stretch(viewport, "Content");
            content.anchorMin = Vector2.zero;
            content.anchorMax = new Vector2(0, 1);
            content.pivot = new Vector2(0, .5f);
            scroll.viewport = viewport;
            scroll.content = content;
            var barRect = Rect(parent, name + "Scrollbar", 175, y + 77, 560, 9);
            Image(barRect, new Color32(185, 193, 198, 255), true);
            var bar = barRect.gameObject.AddComponent<Scrollbar>();
            var handle = Image(Stretch(barRect, "Handle"), new Color32(50, 232, 183, 255), true);
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.LeftToRight;
            scroll.horizontalScrollbar = bar;
        }
        internal void Refresh(IReadOnlyList<PlayerOwnedItem> items)
        {
            count = Mathf.Max(8, items != null ? items.Count : 0);
            while (cells.Count < count)
                cells.Add(new Cell(Stretch(content, "Item" + cells.Count), font));
            for (int i = 0; i < cells.Count; i++)
            {
                bool occupied = items != null && i < items.Count;
                cells[i].Show(occupied ? items[i].Name : "", occupied ? items[i].Icon : null);
                cells[i].Rect.gameObject.SetActive(i < count);
            }
            Layout();
        }
        internal void Layout()
        {
            float width = Mathf.Max(1, viewport.rect.width) * count / 8f;
            if (lastCount == count && Mathf.Approximately(lastWidth, width)) return;
            lastWidth = width;
            lastCount = count;
            content.sizeDelta = new Vector2(width, 0);
            for (int i = 0; i < count && i < cells.Count; i++)
            {
                var rect = cells[i].Rect;
                rect.anchorMin = new Vector2((float)i / count, 0);
                rect.anchorMax = new Vector2((float)(i + 1) / count, 1);
                rect.offsetMin = new Vector2(4, 0);
                rect.offsetMax = new Vector2(-4, 0);
            }
        }
    }
}
