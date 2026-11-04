using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class Player
{
    [Header("강공격 차징 기반 테스트 (실제 스킬 효과 미연결)")]
    [SerializeField] private Button heavyAttackButton;
    [SerializeField] private PlayerHeavyChargeSettings heavyChargeSettings = new PlayerHeavyChargeSettings();
    private PlayerHeavyCharge heavyCharge;
    private InputAction heavyInput;
    private int keyboardChargeId;
    private bool startingDefense;
    public bool IsHeavyCharging => heavyCharge != null && heavyCharge.IsCharging;
    public int HeavyChargeId => heavyCharge != null ? heavyCharge.CurrentId : 0;
    public int HeavyChargeStage => heavyCharge != null ? heavyCharge.Stage : 0;
    public float HeavyChargeMultiplier => heavyCharge != null ? heavyCharge.Multiplier : 1f;
    public float HeavyChargeProgress => heavyCharge != null ? heavyCharge.Progress : 0f;
    public bool CanMove => CanAcceptGameplayInput && !IsHeavyCharging;
    public HeavyChargeRelease LastHeavyCharge { get; private set; }
    public event Action<HeavyChargeRelease> HeavyChargeReleased;

    private bool HeavyContextValid() => isActiveAndEnabled && !hasDied && data != null && !data.IsDead
        && !IsInputLocked && !IsDashing && !defense.IsInDefenseMotion && !startingDash && !startingDefense && Time.timeScale > 0f;
    private Vector2 ReadAimDirection()
    {
        Vector2 aim = joystickController != null && joystickController.enabled
            ? joystickController.AimDirection : (moveInput != null ? moveInput.ReadValue<Vector2>() : Vector2.zero);
        return aim.sqrMagnitude > .0001f ? aim : (IsHeavyCharging ? heavyCharge.Direction : view.FacingDirection);
    }
    private void InitializeHeavyCharge()
    {
        heavyCharge = new PlayerHeavyCharge(data, HeavyContextValid);
        if (heavyAttackButton != null)
            Util.GetOrAddComponent<PlayerHeavyAttackButton>(heavyAttackButton.gameObject).Bind(this);
    }
    private void EnableHeavyInput()
    {
        if (heavyChargeSettings == null || !heavyChargeSettings.Configured) return;
        heavyInput = inputActions.FindAction("Player/HeavyAttack", false)?.Clone()
            ?? new InputAction("HeavyChargePreview", InputActionType.Button, "<Keyboard>/q");
        heavyInput.Enable();
    }
    public bool TryBeginHeavyCharge()
    {
        if (heavyCharge == null || !CanAcceptGameplayInput || !HeavyContextValid()) return false;
        if (!heavyCharge.TryBegin(heavyChargeSettings, ReadAimDirection())) return false;
        joystickController?.StopMovement();
        view.SetMovementAnimation(heavyCharge.Direction, false, false, dashDuration);
        RefreshHeavyIndicator(); return true;
    }
    public bool TryReleaseHeavyCharge(int chargeId)
    {
        if (heavyCharge == null || chargeId == 0 || chargeId != HeavyChargeId) return false;
        if (Time.timeScale <= 0f) { CancelHeavyCharge(chargeId); return false; }
        heavyCharge.Tick(0f, ReadAimDirection());
        if (!heavyCharge.TryRelease(chargeId, out HeavyChargeRelease result)) { RefreshHeavyIndicator(); return false; }
        LastHeavyCharge = result; RefreshHeavyIndicator();
        HeavyChargeReleased?.Invoke(result); return true;
    }
    public void CancelHeavyCharge(int chargeId)
    {
        if (chargeId == 0 || chargeId != HeavyChargeId) return;
        heavyCharge.Cancel(); RefreshHeavyIndicator();
    }
    private void ResetHeavyCharge()
    {
        heavyCharge?.Cancel(); keyboardChargeId = 0; RefreshHeavyIndicator();
    }
    private void CancelReleasedHeavyKey()
    {
        if (keyboardChargeId == 0 || heavyInput == null || heavyInput.IsPressed()) return;
        int id = keyboardChargeId;
        keyboardChargeId = 0;
        CancelHeavyCharge(id);
    }
    private void TickHeavyCharge()
    {
        if (heavyInput != null && heavyInput.WasPressedThisFrame() && TryBeginHeavyCharge()) keyboardChargeId = HeavyChargeId;
        heavyCharge.Tick(Time.deltaTime, ReadAimDirection());
        if (keyboardChargeId != 0)
        {
            int id = keyboardChargeId;
            if (id != HeavyChargeId) keyboardChargeId = 0;
            else if (!heavyInput.IsPressed())
            {
                keyboardChargeId = 0;
                if (heavyInput.WasReleasedThisFrame()) TryReleaseHeavyCharge(id);
                else CancelHeavyCharge(id); // 정지 중 잃은 해제를 복귀 후 공격으로 예약하지 않습니다.
            }
        }
        if (IsHeavyCharging) view.SetMovementAnimation(heavyCharge.Direction, false, false, dashDuration);
        RefreshHeavyIndicator();
    }
    private void RefreshHeavyIndicator() => view?.SetHeavyChargeIndicator(IsHeavyCharging,
        IsHeavyCharging ? heavyCharge.Direction : Vector2.zero, HeavyChargeProgress, HeavyChargeStage);
    private void OnApplicationFocus(bool focused) { if (!focused) ResetHeavyCharge(); }
    private void OnApplicationPause(bool paused) { if (paused) ResetHeavyCharge(); }
}
