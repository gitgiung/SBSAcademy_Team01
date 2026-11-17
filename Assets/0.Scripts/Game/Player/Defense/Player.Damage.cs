using UnityEngine;

public sealed partial class Player
{
    /// <summary>사망한 플레이어만 복구합니다. HP 외 MP/레벨/보유 스킬은 유지합니다.</summary>
    public bool TryRevive()
    {
        if (data == null || !data.IsDead) return false;
        data.InitHealth();
        hasDied = false;
        gameObject.SetActive(true);
        view?.RefreshResources();
        return true;
    }

    public bool TryParry()
    {
        if (!CanAcceptGameplayInput || movement.IsDashing || !defense.CanStart) return false;
        ResetHeavyCharge();
        StartCoroutine(defense.GuardParryProcess(IsGuardHeld));
        return true;
    }

    private void HandleParrySucceeded()
    {
        DeviceHaptics.Play(HapticCue.Parry);
        int version = lifecycleVersion;
        data.RestoreMP(data.parryMPRecovery);
        if (parryInvulnerableDuration > 0f)
            data.GrantInvulnerable(parryInvulnerableDuration);
        if (version == lifecycleVersion && isActiveAndEnabled && !hasDied && !data.IsDead)
        {
            BeginParryFeedback(defense.LastParrySource);
            PlayParryCameraZoom();
        }
    }

    /// <summary>Compatibility entry point used by boss attacks and existing scene logic.</summary>
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (data == null || hasDied || defense == null || movement == null)
            return;

        if (data.IsInvulnerable)
            return;

        if (movement.IsDashing || defense.IsBlockingDamage)
        {
            if (defense.IsBlockingDamage)
            {
                bool blockedByGuard = defense.IsGuarding;
                defense.NotifyIncomingAttackBlockedFrom(attacker);
                if (blockedByGuard) DeviceHaptics.Play(HapticCue.Guard);
                Debug.Log("[패링/가드 성공] 공격을 방어했습니다.");
            }
            return;
        }

        float healthBefore = data.currentHealth;
        if (!data.ApplyDamage(damage)) return;
        if (data.currentHealth < healthBefore) DeviceHaptics.Play(HapticCue.Hurt);
        if (IsHeavyCharging && !heavyCharge.SuperArmor) ResetHeavyCharge();
        DamageApplied?.Invoke(this, damage);

        Debug.Log($"[피격] 받은 데미지: {damage} / 남은 체력: {data.currentHealth}");
        if (data.IsDead)
        {
            hasDied = true;
            Died?.Invoke();
            Debug.Log("[Player 사망] 플레이어가 사망했습니다.");
            gameObject.SetActive(false);
        }
        else Damaged?.Invoke(damage, attacker);
    }

    public void TakeContactDamage(float damage, GameObject source)
    {
        TakeDamage(damage, source);
    }
}
