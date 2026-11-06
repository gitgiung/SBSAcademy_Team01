using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
// The authored body collider may be a circle, box, or capsule; do not add a second shape.
public sealed class EnemyFollow : MonoBehaviour
{
    [SerializeField, Min(0f)] public float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float stoppingDistance = 0.5f;

    private Rigidbody2D body;
    private Transform target;
    private CollisionMovement2D movement;

    public Transform Target => target;

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

        if (target == null || !target.gameObject.activeInHierarchy)
            return;

        Vector2 offset = (Vector2)target.position - body.position;
        float distance = offset.magnitude;

        float remaining = distance - Mathf.Max(0f, stoppingDistance);
        if (remaining <= 0f || distance <= Mathf.Epsilon)
            return;

        float step = Mathf.Min(Mathf.Max(0f, moveSpeed) * Time.fixedDeltaTime, remaining);
        MoveTo(body.position + offset / distance * step);
    }

    /// <summary>Also used by boss movement while following is disabled during a pattern.</summary>
    public Vector2 MoveTo(Vector2 position)
    {
        return movement.Move(position - body.position);
    }

    private void OnDisable()
    {
        if (body != null) body.linearVelocity = Vector2.zero;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
    }
}
