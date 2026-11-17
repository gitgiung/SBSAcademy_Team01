using UnityEngine;

public sealed partial class Player
{
    public bool TryParry()
    {
        if (!CanAcceptGameplayInput || movement.IsDashing || !defense.CanStart) return false;
        ResetHeavyCharge();
        StartCoroutine(defense.GuardParryProcess(IsGuardHeld));
        return true;
    }

    private void HandleParrySucceeded()
    {
        DeviceHaptics.Pulse();
        int version = lifecycleVersion;
        data.RestoreMP(data.parryMPRecovery);
        if (parryInvulnerableDuration > 0f)
            data.GrantInvulnerable(parryInvulnerableDuration);
        if (version == lifecycleVersion && isActiveAndEnabled && !hasDied && !data.IsDead)
        {
            movement.ResetDashCooldown();
            BeginParryFeedback(defense.LastParrySource);
            PlayParryCameraZoom();
        }
    }

    /// <summary>Compatibility entry point used by boss attacks and existing scene logic.</summary>
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        TryDamageOrTriggerParry(damage, attacker);
    }

    private bool TryDamageOrTriggerParry(float damage, GameObject attacker)
    {
        if (data == null || hasDied || defense == null || movement == null)
            return false;

        if (data.IsInvulnerable)
            return false;

        if (defense.IsBlockingDamage)
        {
            bool parried = defense.TryRegisterParry(attacker);
            Debug.Log("[패링/가드 성공] 공격을 방어했습니다.");
            return parried;
        }

        if (movement.IsDashing || !data.ApplyDamage(damage, out float healthLost))
            return false;
        if (IsHeavyCharging && !heavyCharge.SuperArmor) ResetHeavyCharge();
        if (healthLost > 0f) DamageApplied?.Invoke(this, healthLost);

        Debug.Log($"[피격] 감소한 체력: {healthLost} / 남은 체력: {data.currentHealth}");
        if (data.IsDead)
        {
            hasDied = true;
            Died?.Invoke();
            Debug.Log("[Player 사망] 플레이어가 사망했습니다.");
            gameObject.SetActive(false);
        }
        else Damaged?.Invoke(damage, attacker);
        return true;
    }

    public void TakeContactDamage(float damage, GameObject source)
    {
        TakeDamage(damage, source);
    }

    bool IContactDamageResultReceiver.TryDamageOrTriggerParry(float damage, GameObject source)
    {
        return TryDamageOrTriggerParry(damage, source);
    }
}
