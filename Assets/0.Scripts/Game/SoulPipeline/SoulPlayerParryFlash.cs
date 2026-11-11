using UnityEngine;

/// <summary>패링 창이 열려 있을 때 플레이어 틴트. 바닥 경고 없음.</summary>
[DisallowMultipleComponent]
public sealed class SoulPlayerParryFlash : MonoBehaviour
{
    private Renderer[] renderers;
    private Color[] baseColors;
    private float parryLeft;
    private float hurtLeft;
    private bool reflecting;

    private void Awake() => Rebind();

    /// <summary>스킨 교체 등으로 자식 렌더러가 바뀐 뒤 다시 수집한다.</summary>
    public void Rebind()
    {
        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i] != null ? renderers[i].material : null;
            baseColors[i] = mat != null && mat.HasProperty("_Color") ? mat.color : Color.white;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton3))
            parryLeft = 0.13f;
        if (parryLeft > 0f) parryLeft -= Time.deltaTime;
        if (hurtLeft > 0f) hurtLeft -= Time.deltaTime;
    }

    /// <summary>패링 성공 직후 짧게 반짝.</summary>
    public void ReflectSuccess()
    {
        reflecting = true;
        parryLeft = 0.2f;
        hurtLeft = 0f;
    }

    public void HurtFlash() => hurtLeft = 0.25f;

    private void LateUpdate()
    {
        Color tint = baseColors.Length > 0 ? baseColors[0] : Color.white;
        float mix = 0f;
        if (hurtLeft > 0f)
        {
            tint = new Color(1f, 0.25f, 0.25f);
            mix = 0.65f;
        }
        else if (parryLeft > 0f)
        {
            tint = reflecting ? new Color(0.6f, 1f, 1f) : new Color(0.85f, 0.95f, 1f);
            mix = reflecting ? 0.7f : 0.45f;
        }
        else
            reflecting = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var mat = renderers[i].material;
            if (mat == null || !mat.HasProperty("_Color")) continue;
            mat.color = mix > 0f ? Color.Lerp(baseColors[i], tint, mix) : baseColors[i];
        }
    }
}
