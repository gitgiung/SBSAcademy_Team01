using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float speed;
    public float lifeTime = 3f;

    [Header("태그 설정")]
    public string enemyTag = "Enemy";

    private bool isHit = false; // 중복 충돌 처리 방지 플래그

    public void Init(Transform target, float damage, float speed)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 이미 적을 맞춘 총알이면 중복 실행 방지
        if (isHit) return;

        if (collision.CompareTag(enemyTag))
        {
            isHit = true; // 첫 충돌 시 즉시 감지 잠금

            if (collision.TryGetComponent<IDamageable>(out var damageableTarget))
            {
                // (int) 형변환을 제거하고 float 데미지를 그대로 전달
                damageableTarget.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}