using UnityEngine;

/// <summary>첫 15초만 조작 힌트. 이후 자동 숨김.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaInputHint : MonoBehaviour
{
    private float shown;
    private GUIStyle style;

    private void Update() => shown += Time.unscaledDeltaTime;

    private void OnGUI()
    {
        if (shown > 15f) return;
        style ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.LowerCenter,
            normal = { textColor = new Color(1f, 1f, 1f, 0.85f) }
        };
        GUI.Label(new Rect(0f, Screen.height - 70f, Screen.width, 24f),
            "WASD 이동 · Shift 대시 · E 패링 · 클릭 공격 · 보스 팔 올림=위험", style);
    }
}
