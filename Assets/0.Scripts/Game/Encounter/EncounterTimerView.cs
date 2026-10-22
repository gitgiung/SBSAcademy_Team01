using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
public sealed class EncounterTimerView : MonoBehaviour
{
    [SerializeField] private BossEncounterController encounter;
    [SerializeField] private Text label;
    private Font runtimeFont;

    private void Awake()
    {
        if (label == null) label = GetComponent<Text>();
        runtimeFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 36);
        if (runtimeFont != null) label.font = runtimeFont;
    }

    private void OnEnable()
    {
        if (encounter != null) encounter.DisplayChanged += Render;
        Render();
    }

    private void Render()
    {
        if (label == null || encounter == null) return;
        label.text = encounter.Phase == EncounterPhase.BossFight
            ? "보스를 처치하세요."
            : FormatTime(encounter.ElapsedTime);
    }

    public static string FormatTime(float elapsed)
    {
        int seconds = Mathf.FloorToInt(Mathf.Max(0f, elapsed));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void OnDisable()
    {
        if (encounter != null) encounter.DisplayChanged -= Render;
    }

    private void OnDestroy()
    {
        if (runtimeFont != null) Destroy(runtimeFont);
    }
}
