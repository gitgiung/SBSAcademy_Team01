using UnityEngine;
using UnityEngine.UI;

public sealed partial class Player
{
    [Header("모바일 패링·가드")]
    [SerializeField] private Button guardButton;
    private PlayerGuardButton guardInput;

    private void InitializeGuardInput()
    {
        if (guardButton != null)
        {
            guardInput = Util.GetOrAddComponent<PlayerGuardButton>(guardButton.gameObject);
            guardInput.Bind(this);
        }
    }

    private bool IsGuardHeld() => (guardInput != null && guardInput.IsHeld)
        || (parryInput != null && parryInput.IsPressed());

    private void ReleaseTouchGuard()
    {
        if (guardInput == null || !guardInput.IsHeld) return;
        guardInput.Release();
        // 팝업 정지 중에는 코루틴이 진행되지 않으므로 즉시 기존 방어 상태를 정리합니다.
        defense?.Reset();
    }
}
