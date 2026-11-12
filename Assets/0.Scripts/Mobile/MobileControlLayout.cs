using UnityEngine;

/// <summary>이 씬의 버튼 묶음/조이스틱 위치와 저장된 좌우 선택을 적용합니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasSafeArea))]
[DefaultExecutionOrder(-900)]
public sealed class MobileControlLayout : MonoBehaviour
{
    private const string PreferenceKey = "MobileControls.Swapped";
    [SerializeField] private RectTransform buttonGroup;
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private RectTransform joystickBackground;
    private CanvasSafeArea safeArea;
    private Vector2 backgroundMin;
    private Vector2 backgroundMax;
    private Vector2 backgroundPivot;
    private Vector2 backgroundPosition;
    private bool initialized;
    public bool ControlsSwapped { get; private set; }

    private void OnEnable()
    {
        safeArea = GetComponent<CanvasSafeArea>();
        if (!initialized && joystickBackground != null)
        {
            backgroundMin = joystickBackground.anchorMin;
            backgroundMax = joystickBackground.anchorMax;
            backgroundPivot = joystickBackground.pivot;
            backgroundPosition = joystickBackground.anchoredPosition;
            initialized = true;
        }
        ControlsSwapped = PlayerPrefs.GetInt(PreferenceKey, 0) != 0;
        Apply();
    }

    public void SetControlsSwapped(bool swapped)
    {
        ControlsSwapped = swapped;
        Apply();
        PlayerPrefs.SetInt(PreferenceKey, swapped ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Apply()
    {
        if (!initialized || safeArea == null) return;
        // 위치 변경 시 누르고 있던 조이스틱 입력을 새 배치로 넘기지 않습니다.
        if (joystickRoot != null) joystickRoot.GetComponent<Joystick>()?.OnPointerUp(null);
        safeArea.SetHorizontalMirrored(buttonGroup, ControlsSwapped);
        safeArea.SetHorizontalMirrored(joystickRoot, ControlsSwapped);
        ApplyBackground(ControlsSwapped);
    }

    private void ApplyBackground(bool swapped)
    {
        if (joystickBackground == null || !initialized) return;
        joystickBackground.anchorMin = swapped ? new Vector2(1 - backgroundMax.x, backgroundMin.y) : backgroundMin;
        joystickBackground.anchorMax = swapped ? new Vector2(1 - backgroundMin.x, backgroundMax.y) : backgroundMax;
        joystickBackground.pivot = swapped ? new Vector2(1 - backgroundPivot.x, backgroundPivot.y) : backgroundPivot;
        joystickBackground.anchoredPosition = swapped ? new Vector2(-backgroundPosition.x, backgroundPosition.y) : backgroundPosition;
    }

    private void OnDisable()
    {
        if (safeArea != null)
        {
            safeArea.SetHorizontalMirrored(buttonGroup, false);
            safeArea.SetHorizontalMirrored(joystickRoot, false);
        }
        ApplyBackground(false);
    }
}
