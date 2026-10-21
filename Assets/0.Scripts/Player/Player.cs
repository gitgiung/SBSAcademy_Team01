using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public PlayerData data;
    public GameObject bulletPrefab;
    public LayerMask enemyLayer;
    public Slider healthSlider; // 체력 UI (슬라이더)

    [Header("Dash Settings")]
    public float dashSpeed = 15f;       // 대쉬 속도
    public float dashDuration = 0.2f;    // 대쉬 지속 시간
    public float dashCooldown = 1f;      // 대쉬 쿨타임

    private bool isDashing = false;      // 대쉬 중 여부
    private bool canDash = true;         // 대쉬 가능 여부 (쿨타임 확인)
    private Vector3 dashDirection;      // 대쉬 방향

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
        if (data == null) return;

        // 대쉬 중일 때는 일반 이동 처리를 하지 않음 (DashRoutine이 위치를 이동시킴)
        if (isDashing) return;

        // 1. 방향키 입력 감지
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, v, 0).normalized;

        // 2. Shift 입력 처리 (방향키를 누르고 있고, 쿨타임이 끝났을 때만 실행)
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && dir != Vector3.zero)
        {
            StartCoroutine(DashRoutine(dir));
            return;
        }

        // 3. 일반 이동
        transform.position += dir * data.speed * Time.deltaTime;
    }

    // 대쉬 코루틴
    IEnumerator DashRoutine(Vector3 direction)
    {
        isDashing = true;
        canDash = false;
        dashDirection = direction;

        float elapsedTime = 0f;

        // 대쉬 지속 시간 동안 빠른 속도로 이동
        while (elapsedTime < dashDuration)
        {
            transform.position += dashDirection * dashSpeed * Time.deltaTime;
            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        isDashing = false;

        // 대쉬 쿨타임 대기
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
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