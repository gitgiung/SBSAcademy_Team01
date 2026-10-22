using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public PlayerData data;
    public GameObject bulletPrefab;
    public LayerMask enemyLayer;
    public Slider healthSlider; // 체력 UI (슬라이더)

    private float attackTimer;

    void Start()
    {
        // 게임 시작 시 현재 체력을 UI에 즉시 반영
        UpdateHealthUI();
    }

    void Update()
    {
        Move();
        AutoAttack();
    }

    void Move() // 플레이어 이동
    {
        // 기존 코드 그대로 유지
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, v, 0).normalized;

        // PlayerData의 speed를 가져와서 이동
        if (data != null)
        {
            transform.position += dir * data.speed * Time.deltaTime;
        }
    }

    void AutoAttack() // 자동 공격
    {
        if (data == null) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            // 설정한 사거리(attackRange) 내의 Enemy 레이어 감지
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, data.attackRange, enemyLayer);

            if (enemies.Length > 0)
            {
                // 가장 가까운 적 찾기
                Transform closestEnemy = null;
                float minDistance = float.MaxValue;

                foreach (var enemy in enemies)
                {
                    float dist = Vector2.Distance(transform.position, enemy.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestEnemy = enemy.transform;
                    }
                }

                if (closestEnemy != null)
                {
                    Shoot(closestEnemy);
                    attackTimer = data.attackCooldown;
                }
            }
        }
    }

    void Shoot(Transform target) // 총알 발사
    {
        if (bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            bullet?.Init(target, data.attackDamage, data.bulletSpeed);
        }
    }

    // 적과 부딪혔을 때 데미지 처리
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("enemy"))
        {
            TakeDamage(10f);
        }
    }

    public void TakeDamage(float damage) // 플레이어가 데미지를 받는 메서드
    {
        if (data == null) return;

        data.currentHealth -= damage;
        UpdateHealthUI();

        // 피격 확인용 콘솔 로그 출력
        Debug.Log($"[Player 피격] 받은 데미지: {damage} / 남은 체력: {data.currentHealth}");

        if (data.currentHealth <= 0)
        {
            gameObject.SetActive(false);
            Debug.Log("[Player 사망] 플레이어가 사망했습니다."); // 플레이어 사망 처리
        }
    }

    void UpdateHealthUI() //HP UI 업데이트
    {
        if (healthSlider != null && data != null)
        {
            healthSlider.value = data.currentHealth / data.maxHealth;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.attackRange);
        }
    }
}