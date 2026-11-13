using UnityEngine;

public sealed partial class Player
{
    public bool TryDash(Vector2 direction)
    {
        if (!CanAcceptGameplayInput || !movement.CanBeginDash(direction)) return false;
        float cost = data.dashMPCost;
        int version = lifecycleVersion;
        bool paid = false;
        bool started = false;
        // MPChanged는 동기 알림입니다. 알림 중 다른 행동이 시작되지 않도록 예약합니다.
        startingDash = true;
        try
        {
            paid = data.TrySpendMP(cost);
            if (!paid)
            {
                if (cost > data.CurrentMP) resourceGaugePreview?.FlashInsufficientMp();
                return false;
            }
            if (version != lifecycleVersion || !isActiveAndEnabled || hasDied
                || data.IsDead || IsInputLocked || Time.timeScale <= 0f) return false;
            if (!movement.TryBeginDash(direction, dashDuration, dashSpeedMultiplier, dashCooldown)) return false;
            started = true;
            ResetHeavyCharge();
            guardInput?.Release();
            defense.CancelForDash();
            joystickController?.StopMovement();
            StartCoroutine(movement.DashRoutine());
            return true;
        }
        finally
        {
            // 아직 시작하지 못한 회피만 되돌립니다. 강공격의 취소 환불 정책과 별개입니다.
            try { if (paid && !started) data.RestoreMP(cost); }
            finally { startingDash = false; }
        }
    }

    /// <summary>Updates player visuals from an external movement source such as the mobile joystick.</summary>
    public void SetExternalMovementAnimation(Vector2 moveDirection, bool isDashing = false)
    {
        if (!CanAcceptGameplayInput) moveDirection = Vector2.zero;
        bool isMoving = moveDirection.sqrMagnitude > 0.0001f;
        view?.SetMovementAnimation(moveDirection, isMoving && !isDashing, isDashing, dashDuration);
        if (isMoving != lastExternalMoving)
        {
            lastExternalMoving = isMoving;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
    }
}
