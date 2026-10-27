using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LineRenderer attackIndicator;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.01f)] private float attackRange = 1.5f;
    [SerializeField, Range(1f, 360f)] private float attackAngle = 100f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.3f;
    [SerializeField] private LayerMask damageLayers = ~0;

    private readonly List<Collider2D> hits = new List<Collider2D>();
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
    private InputAction attack;
    private float nextAttackTime;
    private Vector2 lastDirection = Vector2.right;
    private float showAttackUntil;

    private void OnEnable()
    {
        var source = inputActions != null ? inputActions.FindAction("Player/Attack") : null;
        if (source == null)
        {
            Debug.LogWarning("PlayerMeleeAttack requires a Player/Attack input action.", this);
            return;
        }

        attack = source.Clone();
        attack.performed += OnAttack;
        attack.Enable();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (Mouse.current == null || aimCamera == null) return;
        Ray ray = aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        var plane = new Plane(Vector3.forward, transform.position);
        if (plane.Raycast(ray, out float distance))
            TryAttack((Vector2)(ray.GetPoint(distance) - transform.position));
    }

    private void Update()
    {
        if (attackIndicator != null)
            attackIndicator.enabled = Time.time < showAttackUntil;
    }

    public bool TryAttack(Vector2 aimDirection)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || Time.time < nextAttackTime || aimDirection.sqrMagnitude < 0.0001f)
            return false;

        nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
        lastDirection = aimDirection.normalized;
        showAttackUntil = Time.time + 0.12f;
        Vector2 origin = transform.position;
        ShowAttackArc();
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(damageLayers);
        Physics2D.OverlapCircle(origin, Mathf.Max(0.01f, attackRange), filter, hits);
        damagedTargets.Clear();
        float minDot = Mathf.Cos(attackAngle * 0.5f * Mathf.Deg2Rad);
        foreach (var hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform)) continue;
            Vector2 offset = (Vector2)hit.bounds.center - origin;
            if (offset.sqrMagnitude > 0.0001f && Vector2.Dot(lastDirection, offset.normalized) < minDot)
                continue;
            var target = hit.GetComponentInParent<IDamageable>();
            if (target != null && damagedTargets.Add(target))
                target.TakeDamage(Mathf.Max(1, damage));
        }
        return true;
    }

    private void ShowAttackArc()
    {
        if (attackIndicator == null) return;
        const int segments = 16;
        attackIndicator.useWorldSpace = true;
        attackIndicator.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = -attackAngle * 0.5f + attackAngle * i / segments;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * (Vector3)lastDirection;
            attackIndicator.SetPosition(i, transform.position + direction * attackRange);
        }
        attackIndicator.enabled = true;
    }

    private void OnDisable()
    {
        if (attackIndicator != null) attackIndicator.enabled = false;
        if (attack != null)
        {
            attack.performed -= OnAttack;
            attack.Dispose();
            attack = null;
        }
        hits.Clear();
        damagedTargets.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Gizmos.color = Application.isPlaying && Time.time < showAttackUntil ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(origin, attackRange);
        Vector3 direction = lastDirection;
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, 0, attackAngle * 0.5f) * direction * attackRange);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, 0, -attackAngle * 0.5f) * direction * attackRange);
    }
}
