using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ResourceGaugePreview : MonoBehaviour
{
    private const float FlashInterval = 0.1f;
    private const int LevelUpFlashes = 3;
    private const float FlashDuration = FlashInterval * LevelUpFlashes * 2;

    [SerializeField, Min(0.01f), Tooltip("초당 이동하는 게이지 비율입니다. 1은 초당 전체 길이입니다.")]
    private float hpSpeed = 0.2f;
    [SerializeField, Min(0.01f)] private float mpSpeed = 1f;
    [SerializeField, Min(0.01f)] private float expSpeed = 1f;
    [SerializeField] private Material expLevelUpFlashMaterial;

    private Gauge hp;
    private Gauge mp;
    private Gauge exp;
    private PlayerData playerData;
    private int lastLevel;
    private float flashElapsed = FlashDuration;

    internal void Bind(PlayerData data, Slider healthSlider, Slider mpSlider, Slider expSlider)
    {
        playerData = data;
        lastLevel = data.CurrentLevel;
        hp = Gauge.Create(healthSlider);
        mp = Gauge.Create(mpSlider);
        exp = Gauge.Create(expSlider);
    }

    private void OnEnable()
    {
        if (playerData != null) lastLevel = playerData.CurrentLevel;
    }

    private void LateUpdate()
    {
        float deltaTime = Time.unscaledDeltaTime;
        hp?.Update(hpSpeed, deltaTime);
        mp?.Update(mpSpeed, deltaTime);
        if (exp == null || playerData == null) return;

        int level = playerData.CurrentLevel;
        if (level > lastLevel) flashElapsed = 0f;
        else if (level < lastLevel) flashElapsed = FlashDuration;
        lastLevel = level;

        // 카드 선택 창이 전투를 멈춘 동안에는 점멸 재생 시간을 소비하지 않습니다.
        if (flashElapsed < FlashDuration && Time.timeScale > 0f)
        {
            exp.DrawFullFlash(Mathf.FloorToInt(flashElapsed / FlashInterval) % 2 == 0, expLevelUpFlashMaterial);
            flashElapsed += deltaTime;
        }
        else
            exp.Update(expSpeed, deltaTime);
    }

    private void OnDisable()
    {
        flashElapsed = FlashDuration;
        hp?.Reset();
        mp?.Reset();
        exp?.Reset();
    }

    private void OnDestroy()
    {
        hp?.Dispose();
        mp?.Dispose();
        exp?.Dispose();
    }

    internal sealed class Gauge
    {
        private const float PreviewSeconds = 0.08f;
        private readonly Slider slider;
        private readonly Image original;
        private readonly Image white;
        private readonly Image colored;
        private float shown;
        private float lastDamageTarget;
        private float hold;
        private bool animating;

        public static Gauge Create(Slider slider, Material damageMaterial = null)
        {
            if (slider == null || slider.fillRect == null || slider.fillRect.parent == null
                || !slider.fillRect.TryGetComponent(out Image fill) || !fill.enabled)
                return null;

            return new Gauge(slider, fill, damageMaterial);
        }

        private Gauge(Slider slider, Image fill, Material damageMaterial)
        {
            this.slider = slider;
            original = fill;
            white = CreateFill("Resource Preview (White)", Color.white);
            white.material = damageMaterial;
            colored = CreateFill("Resource Preview (Color)", fill.color);
            shown = slider.normalizedValue;
            lastDamageTarget = shown;
        }

        public void UpdateDamage(float speed, float deltaTime)
        {
            if (slider == null) return;

            float target = slider.normalizedValue;
            if (!slider.isActiveAndEnabled || target > lastDamageTarget)
            {
                Reset();
                return;
            }

            lastDamageTarget = target;
            Update(speed, deltaTime);
        }

        public void Update(float speed, float deltaTime)
        {
            if (slider == null || original == null) return;
            colored.material = null;

            float target = slider.normalizedValue;
            if (Mathf.Approximately(shown, target))
            {
                if (animating) Reset();
                return;
            }

            if (!animating)
            {
                animating = true;
                hold = PreviewSeconds;
                original.enabled = false;
            }
            else if (hold > 0f)
            {
                hold -= deltaTime;
            }
            else
            {
                shown = Mathf.MoveTowards(shown, target, Mathf.Max(0.01f, speed) * deltaTime);
            }

            // 증가 시 흰색 목표 구간을 컬러가 채우고, 감소 시 흰색 잔량이 목표값까지 줄어듭니다.
            DrawFill(white, Mathf.Max(shown, target));
            DrawFill(colored, Mathf.Min(shown, target));
        }

        public void DrawFullFlash(bool isWhite, Material flashMaterial)
        {
            if (slider == null || original == null) return;

            animating = true;
            hold = 0f;
            shown = 1f;
            original.enabled = false;
            bool useWhiteImage = isWhite && flashMaterial == null;
            colored.material = isWhite ? flashMaterial : null;
            DrawFill(white, useWhiteImage ? 1f : 0f);
            DrawFill(colored, useWhiteImage ? 0f : 1f);
        }

        public void Reset()
        {
            if (slider != null)
            {
                shown = slider.normalizedValue;
                lastDamageTarget = shown;
            }
            if (animating && original != null) original.enabled = true;
            if (white != null) white.enabled = false;
            if (colored != null) colored.enabled = false;
            animating = false;
        }

        public void Dispose()
        {
            Reset();
            if (white != null) Destroy(white.gameObject);
            if (colored != null) Destroy(colored.gameObject);
        }

        private Image CreateFill(string name, Color color)
        {
            var fillObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = original.gameObject.layer;
            var image = fillObject.GetComponent<Image>();
            var rect = image.rectTransform;
            rect.SetParent(original.transform.parent, false);
            rect.SetSiblingIndex(original.transform.GetSiblingIndex());
            rect.pivot = original.rectTransform.pivot;
            rect.localRotation = original.rectTransform.localRotation;
            rect.localScale = original.rectTransform.localScale;
            image.sprite = original.overrideSprite;
            image.type = original.type;
            image.fillCenter = original.fillCenter;
            image.fillMethod = original.fillMethod;
            image.fillOrigin = original.fillOrigin;
            image.fillClockwise = original.fillClockwise;
            image.pixelsPerUnitMultiplier = original.pixelsPerUnitMultiplier;
            image.preserveAspect = original.preserveAspect;
            image.maskable = original.maskable;
            image.raycastTarget = false;
            image.color = color;
            image.enabled = false;
            return image;
        }

        private void DrawFill(Image image, float ratio)
        {
            var min = Vector2.zero;
            var max = Vector2.one;
            if (image.type == Image.Type.Filled)
                image.fillAmount = ratio;
            else
            {
                switch (slider.direction)
                {
                    case Slider.Direction.LeftToRight: max.x = ratio; break;
                    case Slider.Direction.RightToLeft: min.x = 1f - ratio; break;
                    case Slider.Direction.BottomToTop: max.y = ratio; break;
                    case Slider.Direction.TopToBottom: min.y = 1f - ratio; break;
                }
            }

            var rect = image.rectTransform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = original.rectTransform.offsetMin;
            rect.offsetMax = original.rectTransform.offsetMax;
            image.enabled = ratio > 0f;
        }
    }
}
