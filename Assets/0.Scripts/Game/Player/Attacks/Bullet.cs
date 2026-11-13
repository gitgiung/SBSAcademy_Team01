using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
    private float damage;
    private float speed;
    // 기존 씬/프리팹의 직렬화된 필드명을 유지합니다.
    [SerializeField] private float DeleteTime = 6f;
    private Vector3 moveDirection;
    private float remainingLife;
    private bool initialized;
    private bool isHit;
    private PooledObject pooled;
    private Rigidbody2D body;
    internal System.Action<Vector3> HitFlash { get; set; }

    [Header("태그 설정")]
    public string enemyTag = "Enemy";

    public void Init(Transform target, float damage, float speed)
    {
        InitDirection(target != null ? (Vector2)(target.position - transform.position) : Vector2.zero, damage, speed);
    }

    public void InitDirection(Vector2 direction, float damage, float speed)
    {
        CacheReferences();
        this.damage = damage;
        this.speed = speed;
        // 표적 참조를 보관하지 않습니다. 발사 후 표적의 이동/풀 재사용과 무관한 직선탄입니다.
        moveDirection = direction.normalized;
        remainingLife = Mathf.Max(0f, DeleteTime);
        isHit = false;
        initialized = true;
    }

    public void OnPoolRent()
    {
        CacheReferences();
        ResetState();
    }

    public void OnPoolReturn()
    {
        ResetState();
        StopAllCoroutines();
        CancelInvoke();
    }

    private void CacheReferences()
    {
        // 풀의 초기화는 최초 Awake보다 먼저 호출될 수 있습니다.
        if (pooled == null) pooled = GetComponent<PooledObject>();
        if (body == null) body = GetComponent<Rigidbody2D>();
    }

    private void ResetState()
    {
        damage = 0f;
        speed = 0f;
        moveDirection = Vector3.zero;
        remainingLife = 0f;
        isHit = false;
        initialized = false;
        HitFlash = null;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private void Update()
    {
        if (!initialized || Time.deltaTime <= 0f) return;
        remainingLife -= Time.deltaTime;
        if (remainingLife <= 0f)
        {
            Despawn();
            return;
        }

        transform.position += moveDirection * speed * Time.deltaTime;
        Camera camera = Camera.main;
        if (camera == null) return; // 카메라가 없어도 수명으로 정상 회수합니다.
        Vector3 viewPos = camera.WorldToViewportPoint(transform.position);
        if (viewPos.x < 0f || viewPos.x > 1f || viewPos.y < 0f || viewPos.y > 1f)
            Despawn();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!initialized || isHit || !isActiveAndEnabled) return;
        if (!collision.CompareTag(enemyTag)) return;
        isHit = true;
        uint hitLease = pooled != null ? pooled.LeaseVersion : 0;
        try
        {
            if (collision.TryGetComponent<IDamageable>(out var damageableTarget))
            {
                var hitFlash = HitFlash;
                Vector3 impact = hitFlash != null ? collision.ClosestPoint(transform.position) : Vector2.zero;
                impact.z = transform.position.z;
                EnemyHealth health = damageableTarget as EnemyHealth;
                float previousHealth = health != null ? health.CurrentHealth : 0f;
                damageableTarget.TakeDamage(damage);
                if (health == null || health.CurrentHealth < previousHealth) hitFlash?.Invoke(impact);
            }
        }
        finally
        {
            // 피해 콜백이 이 탄환을 반환·재대여해도 새 발사를 회수하지 않습니다.
            if (this != null && (pooled == null || pooled.LeaseVersion == hitLease))
                Despawn();
        }
    }

    private void Despawn()
    {
        initialized = false;
        if (pooled != null && pooled.IsPoolOwned)
            pooled.ReturnToPool();
        else
            Destroy(gameObject); // 기존 비풀링 Init 호출과의 호환 경로.
    }
}
