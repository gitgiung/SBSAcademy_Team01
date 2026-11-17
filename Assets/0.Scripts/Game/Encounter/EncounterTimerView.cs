using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class EncounterTimerView : MonoBehaviour
{
    [SerializeField] private BossEncounterController encounter;
    [SerializeField] private TMP_Text label;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        if (encounter != null) encounter.DisplayChanged += Render;
        Render();
    }

    private void Render()
    {
        if (label == null || encounter == null) return;
        label.text = FormatTime(encounter.ElapsedTime);
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
}
