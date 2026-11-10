using System.Collections.Generic;
using UnityEngine;

/// <summary>처치/학습 로그 몇 줄.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaKillFeed : MonoBehaviour
{
    private static SoulArenaKillFeed instance;
    private readonly Queue<string> lines = new Queue<string>();
    private readonly Queue<float> times = new Queue<float>();
    private GUIStyle style;

    private void Awake() => instance = this;

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Push(string message)
    {
        if (instance == null) return;
        instance.lines.Enqueue(message);
        instance.times.Enqueue(Time.unscaledTime + 2.8f);
        while (instance.lines.Count > 6)
        {
            instance.lines.Dequeue();
            instance.times.Dequeue();
        }
    }

    private void Update()
    {
        while (times.Count > 0 && times.Peek() <= Time.unscaledTime)
        {
            times.Dequeue();
            lines.Dequeue();
        }
    }

    private void OnGUI()
    {
        if (lines.Count == 0) return;
        style ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = new Color(0.95f, 0.95f, 0.85f) }
        };
        int i = 0;
        foreach (string line in lines)
        {
            float alpha = 1f;
            // 오래된 줄일수록 흐리게(대략).
            alpha = Mathf.Clamp01(1f - i * 0.12f);
            style.normal.textColor = new Color(0.95f, 0.95f, 0.85f, alpha);
            GUI.Label(new Rect(Screen.width - 380, 12 + i * 20, 360, 20), line, style);
            i++;
        }
    }
}
