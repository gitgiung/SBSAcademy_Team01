using UnityEngine;

/// <summary>SoulArena 전용 일시정지. Main 팝업 timeScale 규칙과 분리된 최소 구현.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaPause : MonoBehaviour
{
    private bool paused;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    private void OnDisable()
    {
        if (paused)
        {
            paused = false;
            Time.timeScale = 1f;
        }
    }

    private void OnGUI()
    {
        if (!paused) return;
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Box(new Rect(Screen.width * 0.3f, Screen.height * 0.42f, Screen.width * 0.4f, 72f),
            "일시정지 (P)\nEsc 로비 · R 재시작", style);
    }
}
