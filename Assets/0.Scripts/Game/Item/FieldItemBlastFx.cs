using UnityEngine;

/// <summary>폭탄 아이템의 파동 연출입니다. 판정은 SceneEnemyPool이 하고 여기서는 그림만 그립니다.</summary>
[DisallowMultipleComponent]
public sealed class FieldItemBlastFx : MonoBehaviour
{
    private const string EntitySortingLayer = "Entity";
    private const int SortingOrder = 40;
    private const float Duration = 0.35f;
    private const float StartAlpha = 0.85f;
    private static readonly Color Tint = new Color(1f, 0.93f, 0.68f, 1f);

    private SpriteRenderer ring;
    private float radius;
    private float age;

    /// <summary>중심에서 radius까지 퍼지는 고리를 한 번 재생합니다.</summary>
    public static FieldItemBlastFx Play(Vector3 center, float radius)
    {
        if (float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f) return null;
        Sprite sprite = FieldItemSprites.BlastRing();
        if (sprite == null) return null;

        var fx = new GameObject("FieldItem Blast").AddComponent<FieldItemBlastFx>();
        fx.radius = radius;
        fx.transform.position = center;
        fx.ring = fx.gameObject.AddComponent<SpriteRenderer>();
        fx.ring.sprite = sprite;
        fx.ring.sortingLayerName = EntitySortingLayer;
        fx.ring.sortingOrder = SortingOrder;
        fx.ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fx.ring.receiveShadows = false;
        fx.Apply(0f);
        return fx;
    }

    private void Update()
    {
        // 직후에 레벨업 창이 떠도 화면에 남지 않도록 실제 시간으로 진행합니다.
        float deltaTime = Time.unscaledDeltaTime;
        if (deltaTime > 0f && !float.IsNaN(deltaTime) && !float.IsInfinity(deltaTime)) age += deltaTime;
        float progress = Mathf.Clamp01(age / Duration);
        Apply(progress);
        if (progress >= 1f) Destroy(gameObject);
    }

    private void Apply(float progress)
    {
        float eased = 1f - (1f - progress) * (1f - progress);
        float diameter = Mathf.Max(0.01f, radius * 2f * eased);
        transform.localScale = new Vector3(diameter, diameter, 1f);
        if (ring == null) return;
        Color color = Tint;
        color.a = StartAlpha * (1f - progress);
        ring.color = color;
    }
}
