using UnityEngine;

/// <summary>무리군주가 던지는 느린 구체. 플레이어에게 한 번 닿으면 사라진다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class HordeLordOrb : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;
    private float remainingLife;
    private GameObject source;
    private Rigidbody2D body;
    private bool launched;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Launch(Vector2 travelDirection, float travelSpeed, float hitDamage, float lifeSeconds, GameObject owner)
    {
        direction = travelDirection.sqrMagnitude <= Mathf.Epsilon ? Vector2.down : travelDirection.normalized;
        speed = Mathf.Max(0f, travelSpeed);
        damage = Mathf.Max(0f, hitDamage);
        remainingLife = Mathf.Max(0.05f, lifeSeconds);
        source = owner;
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched || Time.timeScale <= 0f)
            return;

        body.MovePosition(body.position + direction * speed * Time.fixedDeltaTime);
        remainingLife -= Time.fixedDeltaTime;
        if (remainingLife <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!launched || other == null)
            return;

        var receiver = other.GetComponentInParent(typeof(IContactDamageReceiver)) as IContactDamageReceiver;
        if (receiver == null)
            return;

        receiver.TakeContactDamage(damage, source != null ? source : gameObject);
        Destroy(gameObject);
    }
}
