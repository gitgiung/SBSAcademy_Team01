using UnityEngine;

/// <summary>
/// 바닥 VFX 대신, 보스 윈드업일 때만 화면 중앙 상단에 짧은 위험 표시.
/// 예고 본체는 여전히 접근+모션. 이 HUD는 학습용 보조.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulArenaBossTelegraphHud : MonoBehaviour
{
    private SoulBossApproachSlash boss;
    private GUIStyle style;

    private void Start() => boss = FindFirstObjectByType<SoulBossApproachSlash>();

    private void OnGUI()
    {
        if (boss == null || !boss.enabled) return;
        style ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.55f, 0.2f) }
        };

        if (boss.IsWindup)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 18f);
            style.normal.textColor = new Color(1f, 0.45f + 0.2f * pulse, 0.15f);
            GUI.Label(new Rect(0f, Screen.height * 0.18f, Screen.width, 40f), "↑ 휘두르기! (E 패링)", style);
        }
        else if (boss.IsRecovering)
        {
            style.fontSize = 20;
            style.normal.textColor = new Color(0.55f, 1f, 0.65f);
            GUI.Label(new Rect(0f, Screen.height * 0.18f, Screen.width, 36f), "반격 창 — 클릭!", style);
        }
    }
}
