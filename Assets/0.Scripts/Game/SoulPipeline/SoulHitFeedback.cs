using UnityEngine;

/// <summary>피격 시 스케일 펀치 + 짧은 틴트. 연출만, 판정과 무관.</summary>
[DisallowMultipleComponent]
public sealed class SoulHitFeedback : MonoBehaviour
{
    [SerializeField] private float punch = 0.12f;
    [SerializeField] private float recoverSpeed = 10f;
    private Vector3 baseScale;
    private float punchRemaining;
    private Renderer[] renderers;
    private Color[] baseColors;

    private void Awake()
    {
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i] != null ? renderers[i].material : null;
            baseColors[i] = mat != null && mat.HasProperty("_Color") ? mat.color : Color.white;
        }
    }

    private void LateUpdate()
    {
        if (punchRemaining > 0f)
        {
            punchRemaining -= Time.deltaTime * recoverSpeed;
            float t = Mathf.Clamp01(punchRemaining);
            transform.localScale = baseScale * (1f + punch * t);
            ApplyTint(t);
        }
        else if (transform.localScale != baseScale)
        {
            transform.localScale = baseScale;
            ApplyTint(0f);
        }
    }

    private void ApplyTint(float amount)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var mat = renderers[i].material;
            if (mat == null || !mat.HasProperty("_Color")) continue;
            // 흰 스프라이트는 흰색으로 섞으면 변화가 없어 붉게 번쩍인다.
            Color flash = renderers[i] is SpriteRenderer ? new Color(1f, 0.35f, 0.35f) : Color.white;
            mat.color = amount > 0f
                ? Color.Lerp(baseColors[i], flash, amount * 0.7f)
                : baseColors[i];
        }
    }

    public void Play() => punchRemaining = 1f;
}
