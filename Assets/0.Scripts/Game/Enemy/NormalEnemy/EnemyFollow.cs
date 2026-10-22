using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class EnemyFollow : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float stoppingDistance = 0.5f;

    private Rigidbody2D body;
    private Transform target;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetTarget(Transform newTarget, float newStoppingDistance)
    {
        target = newTarget;
        stoppingDistance = Mathf.Max(0f, newStoppingDistance);
    }

    private void FixedUpdate()
    {
        // A dynamic body retains velocity after MovePosition; stop stale motion before each decision.
        body.linearVelocity = Vector2.zero;
        if (target == null || !target.gameObject.activeInHierarchy)
            return;

        Vector2 offset = (Vector2)target.position - body.position;
        float distance = offset.magnitude;
        float remaining = distance - Mathf.Max(0f, stoppingDistance);
        if (remaining <= 0f || distance <= Mathf.Epsilon)
            return;

        float step = Mathf.Min(Mathf.Max(0f, moveSpeed) * Time.fixedDeltaTime, remaining);
        body.MovePosition(body.position + offset / distance * step);
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
