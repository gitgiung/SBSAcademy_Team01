using UnityEngine;

/// <summary>패링→리커버리 추가타 학습용 한 줄. 승패/킬피드와 별도.</summary>
[DisallowMultipleComponent]
public sealed class SoulParryRiposteHint : MonoBehaviour
{
    private float showUntil;
    private GUIStyle style;

    public static void Show(float seconds = 1.4f)
    {
        var hint = FindFirstObjectByType<SoulParryRiposteHint>();
        if (hint == null) return;
        hint.showUntil = Time.unscaledTime + seconds;
    }

    private void OnGUI()
    {
        if (Time.unscaledTime > showUntil) return;
        style ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.7f, 1f, 0.85f) }
        };
        GUI.Label(new Rect(0f, Screen.height * 0.24f, Screen.width, 28f), "패링 성공 → 추가타!", style);
    }
}
