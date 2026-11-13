using UnityEngine;

/// <summary>장착 약공격 한 번의 스냅샷과 애니메이션 실행 ID를 관리합니다.</summary>
internal sealed class PlayerLightSkill
{
    private readonly Player owner;
    private readonly PlayerData data;
    private readonly PlayerView view;
    private int actionId;
    private bool emitted;
    private Vector2 direction;
    private float damage;
    private float speed;
    private float impactTime;
    private GameObject projectile;
    private RuntimeAnimatorController controller;
    public int ActionId => actionId;

    public PlayerLightSkill(Player owner, PlayerData data, PlayerView view)
    {
        this.owner = owner; this.data = data; this.view = view;
    }
    private static bool Nonnegative(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    public bool TryCast(Vector2 aim, GameObject prefab)
    {
        if (actionId != 0 || !owner.CanAcceptGameplayInput) return false;
        int id = data.Loadout.GetEquippedSkillId(PlayerSkillButton.Light);
        Data.SkillData skill = null;
        for (int i = 0; i < data.Loadout.UnlockedAttackSlots; i++)
            if (data.Loadout.GetAttackSkill(i)?.DataId == id && id > 0) { skill = data.Loadout.GetAttackSkill(i); break; }
        if (skill == null || !skill.ActiveConfigured || !Nonnegative(skill.MPCost)
            || !Nonnegative(skill.BaseDamage) || !Nonnegative(skill.DamageMultiplier)
            || !Nonnegative(skill.AnimImpactDuration) || !Nonnegative(data.bulletSpeed)
            || !view.CanPlayLightAttack(skill.AnimName) || prefab == null || prefab.GetComponent<Bullet>() == null) return false;
        float finalDamage = data.ScaleAttackDamage(skill.BaseDamage + data.attackDamage * skill.DamageMultiplier);
        if (!Nonnegative(finalDamage)) return false;
        float cost = skill.MPCost;
        float castSpeed = data.bulletSpeed;
        float castImpactTime = skill.AnimImpactDuration;
        string animationState = skill.AnimName;
        RuntimeAnimatorController castController = view.AnimationController;
        Vector2 castDirection = aim.sqrMagnitude > .0001f ? aim.normalized : view.FacingDirection;
        if (!owner.TryBeginLightAttack(cost, out int startedId)) return false;
        // 비용 변경 알림에서 Animator/리소스가 무효화되면 실제 시작 전만 환불합니다.
        if (!view.CanPlayLightAttack(animationState) || view.AnimationController != castController || prefab == null)
        {
            owner.CompleteLightAttack(startedId);
            data.RestoreMP(cost);
            return false;
        }
        actionId = startedId;
        emitted = false;
        direction = castDirection;
        damage = finalDamage;
        speed = castSpeed;
        impactTime = castImpactTime;
        projectile = prefab;
        controller = castController;
        view.PlayLightAttack(animationState, direction);
        return actionId == startedId;
    }
    public void OnAnimationProgress(int id, float elapsed)
    {
        if (id == 0 || id != actionId || emitted || elapsed < impactTime) return;
        if (!owner.isActiveAndEnabled || data.IsDead || Time.timeScale <= 0f) return;
        emitted = true;
        try
        {
            Managers.Pool.Pop(projectile, owner.transform.position, Quaternion.identity,
                initialize: go => go.GetComponent<Bullet>().InitDirection(direction, damage, speed));
        }
        catch (System.Exception exception)
        {
            Cancel();
            Debug.LogException(exception, owner);
        }
    }
    public void OnAnimationFinished(int id)
    {
        if (id == 0 || id != actionId) return;
        Cancel();
    }
    public void CheckAnimation()
    {
        if (actionId != 0 && (!view.AnimationAvailable || view.AnimationController != controller)) Cancel();
    }
    public void Cancel()
    {
        int previous = actionId;
        actionId = 0;
        projectile = null;
        controller = null;
        if (previous != 0) owner.CompleteLightAttack(previous);
    }
}
