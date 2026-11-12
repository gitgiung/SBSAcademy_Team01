using UnityEngine;

/// <summary>
/// 폭탄 아이템의 연출입니다. 터진 자리의 폭발 그림과 화면 전체로 퍼지는 파동 고리를 재생합니다.
/// 판정은 SceneEnemyPool이 하고 여기서는 그림만 그립니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class FieldItemBlastFx : MonoBehaviour
{
    private const string EntitySortingLayer = "Entity";
    private const int RingSortingOrder = 40;
    private const int BurstSortingOrder = 41;
    private const float RingDuration = 0.35f;
    private const float RingStartAlpha = 0.85f;
    /// <summary>폭발 그림(5유닛)을 키우는 배율입니다.</summary>
    private const float BurstScale = 1.25f;
    private static readonly Color RingTint = new Color(1f, 0.93f, 0.68f, 1f);

    private SpriteRenderer ring;
    private SpriteRenderer burst;
    private Sprite[] burstFrames;
    private float ringSpriteWidth = 1f;
    private float radius;
    private float lifetime;
    private float age;

    /// <summary>중심에서 폭발을 한 번 재생하고 radius까지 퍼지는 고리를 그립니다.</summary>
    public static FieldItemBlastFx Play(Vector3 center, float radius)
    {
        if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f) return null;
        Sprite ringSprite = FieldItemSprites.BlastRing();
        if (ringSprite == null) return null;

        // 고리만 크기가 변하도록 자식에 두고, 폭발 그림은 크기가 고정된 뿌리에 둡니다.
        var fx = new GameObject("FieldItem Blast").AddComponent<FieldItemBlastFx>();
        fx.radius = radius;
        fx.transform.position = center;
        fx.transform.localScale = new Vector3(BurstScale, BurstScale, 1f);

        var ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(fx.transform, false);
        fx.ring = ringObject.AddComponent<SpriteRenderer>();
        fx.ring.sprite = ringSprite;
        Prepare(fx.ring, RingSortingOrder);
        float width = ringSprite.bounds.size.x;
        if (!float.IsNaN(width) && !float.IsInfinity(width) && width > 0.0001f) fx.ringSpriteWidth = width;

        fx.lifetime = RingDuration;
        Sprite[] frames = FieldItemSprites.BlastFrames();
        if (frames != null && frames.Length > 0)
        {
            fx.burstFrames = frames;
            fx.burst = fx.gameObject.AddComponent<SpriteRenderer>();
            Prepare(fx.burst, BurstSortingOrder);
            fx.lifetime = Mathf.Max(RingDuration, frames.Length * FieldItemSprites.BlastFrameDuration);
        }

        fx.Apply();
        return fx;
    }

    private static void Prepare(SpriteRenderer renderer, int sortingOrder)
    {
        renderer.sortingLayerName = EntitySortingLayer;
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void Update()
    {
        // 직후에 레벨업 창이 떠도 화면에 남지 않도록 실제 시간으로 진행합니다.
        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime > 0f && !float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime)) age += deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        Apply();
    }

    private void Apply()
    {
        if (ring != null)
        {
            float progress = Mathf.Clamp01(age / RingDuration);
            float eased = 1f - (1f - progress) * (1f - progress);
            // 뿌리의 배율과 고리 그림의 원래 너비를 상쇄해 지름이 정확히 radius × 2가 되게 합니다.
            float diameter = Mathf.Max(0.01f, radius * 2f * eased) / (ringSpriteWidth * BurstScale);
            ring.transform.localScale = new Vector3(diameter, diameter, 1f);
            Color color = RingTint;
            color.a = RingStartAlpha * (1f - progress);
            ring.color = color;
            ring.enabled = progress < 1f;
        }

        if (burst != null)
        {
            int frame = (int)(age / FieldItemSprites.BlastFrameDuration);
            bool playing = frame >= 0 && frame < burstFrames.Length;
            burst.enabled = playing;
            if (playing) burst.sprite = burstFrames[frame];
        }
    }
}
