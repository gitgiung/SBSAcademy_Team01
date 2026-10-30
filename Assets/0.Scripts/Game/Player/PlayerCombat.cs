using UnityEngine;

/// <summary>Owns target selection and automatic projectile attacks.</summary>
internal sealed class PlayerCombat
{
    private readonly Transform owner;
    private readonly PlayerData data;
    private readonly GameObject bulletPrefab;
    private readonly string enemyTag;
    private float attackTimer;

    public PlayerCombat(Transform owner, PlayerData data, GameObject bulletPrefab, string enemyTag)
    {
        this.owner = owner;
        this.data = data;
        this.bulletPrefab = bulletPrefab;
        this.enemyTag = enemyTag;
    }

    public void Tick(float deltaTime)
    {
        attackTimer -= deltaTime;
        if (attackTimer > 0f) return;

        Transform target = FindClosestTarget();
        if (target == null) return;

        FireAt(target);
        attackTimer = Mathf.Max(0.01f, data.attackCooldown);
    }

    private Transform FindClosestTarget()
    {
        Collider2D[] candidates = Physics2D.OverlapCircleAll(owner.position, data.attackRange);
        Transform closest = null;
        float closestDistanceSquared = float.PositiveInfinity;

        foreach (Collider2D candidate in candidates)
        {
            if (candidate == null || !candidate.CompareTag(enemyTag)) continue;
            float distanceSquared = ((Vector2)candidate.transform.position - (Vector2)owner.position).sqrMagnitude;
            if (distanceSquared >= closestDistanceSquared) continue;
            closestDistanceSquared = distanceSquared;
            closest = candidate.transform;
        }

        return closest;
    }

    private void FireAt(Transform target)
    {
        if (bulletPrefab == null) return;
        GameObject bulletObject = Object.Instantiate(bulletPrefab, owner.position, Quaternion.identity);
        if (bulletObject.TryGetComponent<Bullet>(out Bullet bullet))
            bullet.Init(target, data.attackDamage, data.bulletSpeed);
    }
}
