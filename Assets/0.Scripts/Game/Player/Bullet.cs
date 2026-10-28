using UnityEngine;
public class Bullet : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float speed;
    [SerializeField] private float DeleteTime = 6f; //총알이 사라질 시간
    private Vector3 moveDirection; // 발사될 직선 방향

    [Header("태그 설정")]
    public string enemyTag = "Enemy";

    private bool isHit = false; // 중복 충돌 처리 방지 플래그

    public void Init(Transform target, float damage, float speed)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;

        moveDirection = (target.position - transform.position).normalized; // 적 위치를 담을 변수(딱한번 적용, 유도시 제거)

    }

    private void Update()
    {
        CameraBullet();
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }


        transform.position += moveDirection * speed * Time.deltaTime; // moveDirection에 담긴 위치값을 향애 Speed만큼 전진

        //transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime); //유도(실시간 적 위치 추적)

        if (Vector2.Distance(transform.position, target.position) < 0.1f)
        {
            Destroy(gameObject);
        }
        Destroy(gameObject, DeleteTime);
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

    private void CameraBullet() //총알이 카메라 밖으로 나가면 제거
    {
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);

        if (viewPos.x < 0f || viewPos.x > 1f || viewPos.y < 0f || viewPos.y > 1f)
        {
            Destroy(gameObject);
        }
    }

}