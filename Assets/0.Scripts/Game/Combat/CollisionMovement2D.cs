using System.Collections.Generic;
using UnityEngine;

/// <summary>Moves a body up to solid obstacles, without transferring movement to them.</summary>
public sealed class CollisionMovement2D // 플레이어하고 Enemy,Boss 밀림 방지
{
    private const float Skin = 0.001f;
    private readonly Rigidbody2D body;
    private readonly Collider2D[] colliders;
    private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);

    public CollisionMovement2D(Rigidbody2D body)
    {
        this.body = body;
        colliders = body.GetComponentsInChildren<Collider2D>(true);
    }

    public Vector2 Move(Vector2 displacement)
    {
        if (body == null || !body.simulated) return Vector2.zero;
        Vector2 start = body.position;
        body.linearVelocity = Vector2.zero;
        Vector2 remaining = displacement;
        // A second sweep lets movement slide along a surface; a third handles corners.
        for (int pass = 0; pass < 3 && remaining.sqrMagnitude > 0.00000001f; pass++)
        {
            float distance = remaining.magnitude;
            Vector2 direction = remaining / distance;
            float allowed = distance;
            Vector2 blockingNormal = Vector2.zero;
            foreach (Collider2D source in colliders)
            {
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy ||
                    source.isTrigger || source.attachedRigidbody != body) continue;
                var filter = new ContactFilter2D { useTriggers = false };
                int mask = (Physics2D.GetLayerCollisionMask(source.gameObject.layer) | source.includeLayers.value)
                    & ~source.excludeLayers.value;
                filter.SetLayerMask(mask);
                source.Cast(direction, filter, hits, distance + Skin, true);
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider == null || Physics2D.GetIgnoreCollision(source, hit.collider)) continue;
                    Vector2 normal = hit.normal;
                    if (hit.distance <= Skin)
                    {
                        // A cast beginning in contact returns -direction. Use the actual
                        // separation normal so moving away or along the surface still works.
                        ColliderDistance2D separation = source.Distance(hit.collider);
                        if (separation.isValid) normal = -separation.normal;
                    }
                    if (Vector2.Dot(direction, normal) >= -0.00001f) continue;
                    float stopDistance = Mathf.Max(0, hit.distance - Skin);
                    if (stopDistance < allowed)
                    {
                        allowed = stopDistance;
                        blockingNormal = normal;
                    }
                }
            }
            Vector2 step = direction * allowed;
            // Commit immediately so the next actor's sweep sees the new position too.
            body.position += step;
            remaining -= step;
            if (blockingNormal == Vector2.zero) break;
            remaining -= blockingNormal * Vector2.Dot(remaining, blockingNormal);
        }
        return body.position - start;
    }
}
