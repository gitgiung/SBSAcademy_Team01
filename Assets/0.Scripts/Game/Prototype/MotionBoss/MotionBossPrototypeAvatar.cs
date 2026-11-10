using UnityEngine;

/// <summary>샌드박스용 최소 플레이어. MainScene Player와 분리된다.</summary>
[DisallowMultipleComponent]
public sealed class MotionBossPrototypeAvatar : MonoBehaviour, IContactDamageReceiver
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float parryActiveSeconds = 0.13f;
    [SerializeField] private float invulnAfterHitSeconds = 0.35f;

    private float health;
    private float parryRemaining;
    private float invulnRemaining;
    private Rigidbody2D body;
    private SpriteRenderer view;
    private Color baseColor = new Color(0.35f, 0.75f, 1f, 1f);

    public float Health => health;
    public float MaxHealth => maxHealth;
    public bool IsParrying => parryRemaining > 0f;

    private void Awake()
    {
        health = maxHealth;
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        view = GetComponent<SpriteRenderer>();
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.45f;
        }
    }

    public void BindView(SpriteRenderer spriteRenderer, Color color)
    {
        view = spriteRenderer;
        baseColor = color;
        if (view != null) view.color = baseColor;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (parryRemaining > 0f) parryRemaining -= dt;
        if (invulnRemaining > 0f) invulnRemaining -= dt;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton3))
            parryRemaining = parryActiveSeconds;

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();
        body.linearVelocity = input * moveSpeed;

        if (view != null)
        {
            if (IsParrying) view.color = Color.yellow;
            else if (invulnRemaining > 0f) view.color = new Color(1f, 0.45f, 0.45f, 1f);
            else view.color = baseColor;
        }
    }

    public void TakeContactDamage(float damage, GameObject source)
    {
        if (health <= 0f || invulnRemaining > 0f) return;
        if (IsParrying)
        {
            parryRemaining = 0f;
            Debug.Log($"[MotionBossPrototype] 패링 성공 ({source?.name})", this);
            return;
        }

        health = Mathf.Max(0f, health - damage);
        invulnRemaining = invulnAfterHitSeconds;
        Debug.Log($"[MotionBossPrototype] 피격 {damage:0.#} → HP {health:0.#}/{maxHealth:0.#}", this);
    }
}
