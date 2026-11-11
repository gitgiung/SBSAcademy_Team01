using UnityEngine;

/// <summary>윈드업/공격 중 보스 틴트. UI 텔레그래프가 아니라 몸 실루엣 보조.</summary>
[DisallowMultipleComponent]
public sealed class SoulBossWindupFlash : MonoBehaviour
{
    private SoulBossApproachSlash boss;
    private Renderer[] renderers;
    private Color[] baseColors;

    private void Awake()
    {
        boss = GetComponent<SoulBossApproachSlash>();
        Rebind();
    }

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

    private void LateUpdate()
    {
        if (boss == null || renderers == null) return;

        Color tint = baseColors.Length > 0 ? baseColors[0] : Color.white;
        float mix = 0f;
        if (boss.IsWindup)
        {
            // 팔을 올리는 동안 살짝 밝아짐
            mix = 0.35f + boss.AttackNormalizedTime * 0.4f;
            tint = new Color(1f, 0.75f, 0.35f);
        }
        else if (boss.IsAttacking)
        {
            mix = 0.55f;
            tint = new Color(1f, 0.35f, 0.2f);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var mat = renderers[i].material;
            if (mat == null || !mat.HasProperty("_Color")) continue;
            mat.color = mix > 0f
                ? Color.Lerp(baseColors[i], tint, mix)
                : baseColors[i];
        }
    }
}
