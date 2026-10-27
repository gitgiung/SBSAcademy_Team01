using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public PlayerData data;
    public GameObject bulletPrefab;
    public LayerMask enemyLayer;
    public Slider healthSlider;

    [Header("테스트용 패링 설정")]
    public float parryWindow = 0.25f;  // 패링 유효 시간 (초)
    public float parryCooldown = 0.5f; // 패링 쿨타임 (초)
    public float ParrySpeed = 0.8f; // 패링 중 이동속도 감소 비율 (기본 속도의 80%)
    private bool isParrying = false;   // 현재 패링 상태인가?
    private bool canParry = true;       // 패링 사용 가능한 상태인가?



    private float attackTimer;

    void Start()
    {
        UpdateHealthUI();
    }

    void Update()
    {
        Move();
        AutoAttack();

        // [테스트] E키 누르면 패링 발동 (쿨타임 검사)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (canParry && !isParrying)
            {
                StartCoroutine(ParryRoutine());
            }
        }
    }

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        canParry = false;
        Debug.Log("[패링 발동] 패링 상태 시작! (이동속도 20% 감소)");

        // 패링 유효 시간 동안 유지
        yield return new WaitForSeconds(parryWindow);

        isParrying = false;
        Debug.Log("[패링 종료] 패링 상태 끝");

        float remainingCooldown = parryCooldown - parryWindow;
        if (remainingCooldown > 0f)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }

        canParry = true;
        Debug.Log("[패링 쿨타임 종료] 패링 재사용 가능");
    }

    // 적과 부딪혔을 때
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            TakeDamage(10f, collision.gameObject);
        }
    }

    // 데미지 처리 및 패링 검사
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (data == null) return;

        // 패링 상태에서 데미지를 받는 경우
        if (isParrying)
        {
            Debug.Log("[패링 성공] 공격을 튕겨냈습니다.");
            isParrying = false; // 성공 즉시 상태 해제
            return; // 데미지 무효화
        }

        // 패링 상태가 아닐 때 (일반 피격)
        data.currentHealth -= damage;
        UpdateHealthUI();

        Debug.Log($"[데미지를 받았다] 받은 데미지: {damage} / 남은 체력: {data.currentHealth}");

        if (data.currentHealth <= 0)
        {
            gameObject.SetActive(false);
            Debug.Log("[Player 사망] 플레이어가 사망했습니다.");
        }
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, v, 0).normalized;

        if (data != null)
        {
            // 패링 중일 때는 이동속도 20% 감소 (기본 속도의 80% 적용)
            float currentSpeed = isParrying ? data.speed * ParrySpeed : data.speed;
            transform.position += dir * currentSpeed * Time.deltaTime;
        }
    }

    void AutoAttack()
    {
        if (data == null) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, data.attackRange, enemyLayer);

            if (enemies.Length > 0)
            {
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

    void Shoot(Transform target)
    {
        if (bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            bullet?.Init(target, data.attackDamage, data.bulletSpeed);
        }
    }

    void UpdateHealthUI()
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