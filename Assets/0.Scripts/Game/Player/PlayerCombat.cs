using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlayerProjectileBinding
{
    public string Key;
    public GameObject Prefab;
}

/// <summary>Owns target selection and automatic projectile attacks.</summary>
internal sealed class PlayerCombat
{
    private readonly Transform owner;
    private readonly PlayerData data;
    private readonly GameObject bulletPrefab;
    private readonly string enemyTag;
    private readonly Dictionary<string, GameObject> projectiles = new Dictionary<string, GameObject>();
    private readonly Data.SkillData[] skills = new Data.SkillData[PlayerLoadout.AttackCapacity];
    private readonly float[] timers = new float[PlayerLoadout.AttackCapacity];
    private float attackTimer;

    public PlayerCombat(Transform owner, PlayerData data, GameObject bulletPrefab, string enemyTag)
    {
        this.owner = owner;
        this.data = data;
        this.bulletPrefab = bulletPrefab;
        this.enemyTag = enemyTag;
    }

    public void BindProjectile(string key, GameObject prefab)
    {
        if (!string.IsNullOrWhiteSpace(key)) projectiles[key] = prefab;
    }

    public void Reset()
    {
        attackTimer = 0f;
        Array.Clear(skills, 0, skills.Length);
        Array.Clear(timers, 0, timers.Length);
    }

    private static bool Nonnegative(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    public void Tick(float deltaTime)
    {
        if (!Nonnegative(deltaTime) || deltaTime == 0f || Time.timeScale <= 0f
            || owner == null || !owner.gameObject.activeInHierarchy || data == null || data.IsDead) return;
        TickBasicAttack(deltaTime);
        Camera camera = Camera.main;
        for (int i = 0; i < skills.Length; i++)
        {
            Data.SkillData skill = data.Loadout.GetAttackSkill(i);
            if (!ReferenceEquals(skills[i], skill)) { skills[i] = skill; timers[i] = 0f; }
            if (skill == null || !skill.PassiveConfigured || string.IsNullOrWhiteSpace(skill.PassivePrefabKey)
                || !Nonnegative(skill.PassiveDamage) || !Nonnegative(skill.PassiveInterval) || skill.PassiveInterval <= 0f
                || !Nonnegative(skill.PassiveSpeed) || !Nonnegative(skill.PassiveRange) || skill.PassiveRange <= 0f) continue;
            timers[i] = Mathf.Max(0f, timers[i] - deltaTime);
            if (timers[i] > 0f) continue;
            projectiles.TryGetValue(skill.PassivePrefabKey, out GameObject prefab);
            if (prefab == null) prefab = Managers.Resource.Load<GameObject>(skill.PassivePrefabKey);
            if (prefab == null || !prefab.TryGetComponent<Bullet>(out _)) continue;
            Transform target = camera != null ? FindClosestTarget(skill.PassiveRange, camera) : null;
            if (target == null) continue;
            Vector2 direction = target.position - owner.position;
            float damage = skill.PassiveDamage;
            float speed = skill.PassiveSpeed;
            // 누적 연사를 하지 않고, 풀 활성화 콜백 전에 다음 주기를 확정합니다.
            timers[i] = skill.PassiveInterval;
            Managers.Pool.Pop(prefab, owner.position, Quaternion.identity,
                initialize: go => go.GetComponent<Bullet>().InitDirection(direction, damage, speed));
            if (owner == null || !owner.gameObject.activeInHierarchy || data.IsDead || Time.timeScale <= 0f) break;
        }
    }

    private void TickBasicAttack(float deltaTime)
    {
        if (bulletPrefab == null || !Nonnegative(data.attackRange) || data.attackRange == 0f
            || !Nonnegative(data.attackCooldown) || !Nonnegative(data.attackDamage) || !Nonnegative(data.bulletSpeed)) return;
        attackTimer = Mathf.Max(0f, attackTimer - deltaTime);
        if (attackTimer > 0f || !bulletPrefab.TryGetComponent<Bullet>(out _)) return;

        Transform target = FindClosestTarget(data.attackRange);
        if (target == null) return;
        PoolManager pool = Managers.Pool;
        if (pool == null) return;

        Vector2 direction = target.position - owner.position;
        float damage = data.attackDamage;
        float speed = data.bulletSpeed;
        attackTimer = Mathf.Max(.01f, data.attackCooldown);
        pool.Pop(bulletPrefab, owner.position, Quaternion.identity,
            initialize: go => go.GetComponent<Bullet>().InitDirection(direction, damage, speed));
    }

    private Transform FindClosestTarget(float range, Camera camera = null)
    {
        Collider2D[] candidates = Physics2D.OverlapCircleAll(owner.position, range);
        Transform closest = null;
        float closestDistanceSquared = float.PositiveInfinity;

        foreach (Collider2D candidate in candidates)
        {
            if (candidate == null || !candidate.CompareTag(enemyTag)) continue;
            if (candidate.TryGetComponent<EnemyHealth>(out var health) && health.IsDead) continue;
            if (camera != null)
            {
                Vector3 viewport = camera.WorldToViewportPoint(candidate.transform.position);
                if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) continue;
            }
            float distanceSquared = ((Vector2)candidate.transform.position - (Vector2)owner.position).sqrMagnitude;
            if (distanceSquared > range * range || distanceSquared >= closestDistanceSquared) continue;
            closestDistanceSquared = distanceSquared;
            closest = candidate.transform;
        }

        return closest;
    }

}
