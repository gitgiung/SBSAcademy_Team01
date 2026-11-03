using System.Collections;
using System;
using UnityEngine;

/// <summary>Runs the parry-to-guard state flow and exposes its defensive state.</summary>
internal sealed class PlayerDefense
{
    private readonly BossParry boss;
    private readonly PlayerView view;
    private readonly float parryStartup;
    private readonly float parryActive;
    private readonly float parryRecovery;
    private readonly float guardTransitionTime;
    private readonly float maxGuardDuration;
    private readonly float normalGuardCooldown;
    private readonly float breakGuardCooldown;
    private int actionVersion;
    private bool canStart = true;
    private bool isParrying;
    private bool isGuarding;
    private bool parrySucceeded;
    private bool parryVfxPlayed;

    public event Action ParrySucceeded;
    public bool IsInDefenseMotion { get; private set; }

    public bool CanStart => canStart;
    public bool IsBlockingDamage => isParrying || isGuarding;
    public bool IsMovementSlowed => IsBlockingDamage;

    public PlayerDefense(BossParry boss, PlayerView view, float parryStartup, float parryActive,
        float parryRecovery, float guardTransitionTime, float maxGuardDuration,
        float normalGuardCooldown, float breakGuardCooldown)
    {
        this.boss = boss;
        this.view = view;
        this.parryStartup = Mathf.Max(0f, parryStartup);
        this.parryActive = Mathf.Max(0f, parryActive);
        this.parryRecovery = Mathf.Max(0f, parryRecovery);
        this.guardTransitionTime = Mathf.Max(0f, guardTransitionTime);
        this.maxGuardDuration = Mathf.Max(0f, maxGuardDuration);
        this.normalGuardCooldown = Mathf.Max(0f, normalGuardCooldown);
        this.breakGuardCooldown = Mathf.Max(0f, breakGuardCooldown);
    }

    public IEnumerator GuardParryProcess(Func<bool> isGuardHeld)
    {
        int version = ++actionVersion;
        canStart = false;
        IsInDefenseMotion = true;
        isParrying = true;
        parrySucceeded = false;
        parryVfxPlayed = false;
        view.PlayParryAnimation();
        if (parryStartup > 0f) yield return new WaitForSeconds(parryStartup);
        if (version != actionVersion) yield break;

        float activeTimer = 0f;
        bool succeeded = false;
        while (activeTimer < parryActive)
        {
            if (version != actionVersion) yield break;
            if ((boss != null && boss.TryParry()) || parrySucceeded)
            {
                NotifyIncomingAttackBlocked();
                succeeded = true;
                if (!parryVfxPlayed) view.PlayParryVfx();
                break;
            }
            activeTimer += Time.deltaTime;
            yield return null;
        }

        isParrying = false;
        if (succeeded)
        {
            Debug.Log("[패링 성공] 공격을 무효화했습니다!");
            yield return null;
            if (version != actionVersion) yield break;
            view.SetGuardAnimation(false);
            IsInDefenseMotion = false;
            canStart = true;
            yield break;
        }

        Debug.Log("[패링 실패] 판정 창 닫힘");
        if (isGuardHeld())
        {
            float extraWait = guardTransitionTime - (parryStartup + parryActive);
            if (extraWait > 0f) yield return new WaitForSeconds(extraWait);
            if (version != actionVersion) yield break;

            isGuarding = true;
            view.SetGuardAnimation(true);
            float guardTimer = 0f;
            bool broken = false;
            while (isGuardHeld())
            {
                if (version != actionVersion) yield break;
                guardTimer += Time.deltaTime;
                if (guardTimer >= maxGuardDuration)
                {
                    broken = true;
                    break;
                }
                yield return null;
            }

            isGuarding = false;
            IsInDefenseMotion = false;
            view.SetGuardAnimation(false);
            yield return new WaitForSeconds(broken ? breakGuardCooldown : normalGuardCooldown);
            if (version != actionVersion) yield break;
        }
        else
        {
            view.SetGuardAnimation(false);
            if (parryRecovery > 0f) yield return new WaitForSeconds(parryRecovery);
            if (version != actionVersion) yield break;
        }

        canStart = true;
        IsInDefenseMotion = false;
        Debug.Log("[패링/가드 쿨타임 종료] 재사용 가능");
    }

    /// <summary>Marks an incoming attack blocked during the parry window as a successful parry.</summary>
    public void NotifyIncomingAttackBlocked()
    {
        if (!isParrying || parrySucceeded) return;
        parrySucceeded = true;
        ParrySucceeded?.Invoke();
        if (parryVfxPlayed) return;
        parryVfxPlayed = true;
        view.PlayParryVfx();
    }

    /// <summary>Immediately ends defense when a dash takes priority over parry or guard.</summary>
    public void CancelForDash()
    {
        actionVersion++;
        isParrying = false;
        isGuarding = false;
        IsInDefenseMotion = false;
        canStart = true;
        view.SetGuardAnimation(false);
    }

    public void Reset()
    {
        actionVersion++;
        isParrying = false;
        isGuarding = false;
        IsInDefenseMotion = false;
        canStart = true;
        view.SetGuardAnimation(false);
    }
}
