using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
// The authored body collider may be a circle, box, or capsule; do not add a second shape.
public sealed class EnemyFollow : MonoBehaviour
{
    [SerializeField, Min(0f)] public float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float stoppingDistance = 0.5f;
    [SerializeField, Min(0f)] private float knockbackDistancePerDamage = 0.15f;

    private const float KnockbackDuration = 0.12f;
    private const float MaxKnockbackDistance = 1.25f;

    private Rigidbody2D body;
    private Transform target;
    private CollisionMovement2D movement;
    private Vector2 lastChaseDirection;
    private Vector2 knockbackRemaining;
    private float knockbackTimeRemaining;
    private bool knockbackDisabled;

    public Transform Target => target;
    internal bool MovedByHitKnockback { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        // Enemies and bosses move through code, not impulses from player contact.
        body.bodyType = RigidbodyType2D.Kinematic;
        // Keep solid contact callbacks, including against a kinematic player.
        body.useFullKinematicContacts = true;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        movement = new CollisionMovement2D(body);
    }

    public void SetTarget(Transform newTarget)
    {
        if (target != newTarget) lastChaseDirection = Vector2.zero;
        target = newTarget;
    }

    public void SetTarget(Transform newTarget, float newStoppingDistance)
    {
        SetTarget(newTarget);
        stoppingDistance = Mathf.Max(0f, newStoppingDistance);
    }

    private void FixedUpdate()
    {
        body.linearVelocity = Vector2.zero;
        MovedByHitKnockback = knockbackTimeRemaining > 0f;

        if (MovedByHitKnockback)
        {
            float elapsed = Mathf.Min(Time.fixedDeltaTime, knockbackTimeRemaining);
            Vector2 knockbackStep = knockbackRemaining * (elapsed / knockbackTimeRemaining);
            knockbackRemaining -= knockbackStep;
            knockbackTimeRemaining -= elapsed;
            movement.Move(knockbackStep);
            return;
        }

        if (target == null || !target.gameObject.activeInHierarchy)
            return;

        Vector2 offset = (Vector2)target.position - body.position;
        float distance = offset.magnitude;

        float remaining = distance - Mathf.Max(0f, stoppingDistance);
        if (remaining <= 0f || distance <= Mathf.Epsilon)
            return;

        float step = Mathf.Min(Mathf.Max(0f, moveSpeed) * Time.fixedDeltaTime, remaining);
        Vector2 moved = MoveTo(body.position + offset / distance * step);
        if (moved.sqrMagnitude > 0.00000001f) lastChaseDirection = moved.normalized;
    }

    internal void ApplyHitKnockback(float damage, float distanceMultiplier)
    {
        if (knockbackDisabled || !isActiveAndEnabled || movement == null ||
            target == null || !target.gameObject.activeInHierarchy || knockbackDistancePerDamage <= 0f)
            return;

        Vector2 direction = lastChaseDirection;
        if (direction == Vector2.zero)
            direction = ((Vector2)target.position - body.position).normalized;
        if (direction == Vector2.zero) return;

        float distance = Mathf.Min(damage * knockbackDistancePerDamage, MaxKnockbackDistance) * distanceMultiplier;
        float remainingLimit = Mathf.Max(knockbackRemaining.magnitude, MaxKnockbackDistance * distanceMultiplier);
        knockbackRemaining = Vector2.ClampMagnitude(knockbackRemaining - direction * distance, remainingLimit);
        knockbackTimeRemaining = KnockbackDuration;
    }

    public void DisableHitKnockback()
    {
        knockbackDisabled = true;
        ResetHitKnockback();
    }

    internal void ResetHitKnockback()
    {
        MovedByHitKnockback = false;
        lastChaseDirection = Vector2.zero;
        knockbackRemaining = Vector2.zero;
        knockbackTimeRemaining = 0f;
    }

    /// <summary>Also used by boss movement while following is disabled during a pattern.</summary>
    public Vector2 MoveTo(Vector2 position)
    {
        return movement.Move(position - body.position);
    }

    private void OnDisable()
    {
        ResetHitKnockback();
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
    }
}
