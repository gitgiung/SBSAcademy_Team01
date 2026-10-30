using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("플레이어 데이터 및 기본 설정")]
    public PlayerData data;
    public GameObject bulletPrefab;
    public Slider healthSlider;

    [Header("태그 설정")]
    public string enemyTag = "Enemy";

    [Header("렌더러 설정")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    private readonly Color defaultColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Header("패링 타이밍 설정 (초 단위)")]
    [SerializeField] private float parryStartup = 0.033f;   // 선행 시간
    [SerializeField] private float parryActive = 0.133f;    // 유효 시간
    [SerializeField] private float parryRecovery = 0.233f;  // 실패 후딜레이

    [Header("일반 가드 설정 (초 단위)")]
    [SerializeField] private float guardTransitionTime = 0.183f;
    [SerializeField] private float maxGuardDuration = 2.0f;
    [SerializeField] private float normalGuardCooldown = 0.5f;
    [SerializeField] private float breakGuardCooldown = 1.5f;

    [Header("이동 속도 조절")]
    public float parrySpeedRatio = 0.8f;

    private bool isParrying = false;
    private bool isGuarding = false;
    private bool canGuardOrParry = true;

    [SerializeField] private BossParry boss;

    private float attackTimer;
    private Coroutine guardParryCoroutine;
    private bool hasDied;
    public event System.Action Died;

    private void Awake()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (data == null)
            data = GetComponent<PlayerData>();
    }

    private void Start()
    {
        if (data != null)
        {
            data.InitHealth();
        }

        SetPlayerColor(defaultColor);
        UpdateHealthUI();
    }

    private void Update()
    {
        // Scaled time alone does not stop Update input or an attack whose cooldown is already ready.
        if (Time.timeScale <= 0f) return;
        Move();
        AutoAttack();

        if (Input.GetKeyDown(KeyCode.E) && canGuardOrParry)
        {
            if (guardParryCoroutine != null) StopCoroutine(guardParryCoroutine);
            guardParryCoroutine = StartCoroutine(GuardParryProcess());
        }
    }

    private IEnumerator GuardParryProcess()
    {
        canGuardOrParry = false;

        // [핵심] 키를 누르자마자 패링 무적 판정 시작
        isParrying = true;
        SetPlayerColor(Color.blue);

        // 선행 프레임 대기
        if (parryStartup > 0f)
            yield return new WaitForSeconds(parryStartup);

        // 유효 창 동안 보스 패링 지속 감지
        float activeTimer = 0f;
        bool isSuccess = false;

        while (activeTimer < parryActive)
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }
            if (boss != null && boss.TryParry())
            {
                isSuccess = true;
                break;
            }
            activeTimer += Time.deltaTime;
            yield return null;
        }

        // 패링 성공 시
        if (isSuccess)
        {
            Debug.Log("[패링 성공] 공격을 무효화했습니다!");
            yield return null; // 데미지 처리 프레임 동기화

            isParrying = false;
            SetPlayerColor(defaultColor);
            canGuardOrParry = true;
            yield break;
        }

        // 패링 실패 시
        isParrying = false;
        Debug.Log("[패링 실패] 판정 창 닫힘");

        if (Input.GetKey(KeyCode.E))
        {
            float extraWaitToGuard = guardTransitionTime - (parryStartup + parryActive);
            if (extraWaitToGuard > 0f)
            {
                yield return new WaitForSeconds(extraWaitToGuard);
            }

            isGuarding = true;
            SetPlayerColor(Color.red);

            float guardTimer = 0f;
            bool forceBroken = false;

            while (Time.timeScale <= 0f || Input.GetKey(KeyCode.E))
            {
                if (Time.timeScale <= 0f)
                {
                    yield return null;
                    continue;
                }
                guardTimer += Time.deltaTime;
                if (guardTimer >= maxGuardDuration)
                {
                    forceBroken = true;
                    break;
                }
                yield return null;
            }

            isGuarding = false;
            SetPlayerColor(defaultColor);

            float cooldown = forceBroken ? breakGuardCooldown : normalGuardCooldown;
            yield return new WaitForSeconds(cooldown);
        }
        else
        {
            SetPlayerColor(defaultColor);
            yield return new WaitForSeconds(parryRecovery);
        }

        canGuardOrParry = true;
        Debug.Log("[패링/가드 쿨타임 종료] 재사용 가능");
    }

    private void SetPlayerColor(Color color)
    {
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.color = color;
    }

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (data == null || hasDied) return;

        if (isParrying || isGuarding)
        {
            Debug.Log("[패링/가드 성공] 공격을 방어했습니다.");
            return;
        }

        data.currentHealth -= damage;
        UpdateHealthUI();

        Debug.Log($"[피격] 받은 데미지: {damage} / 남은 체력: {data.currentHealth}");

        if (data.currentHealth <= 0f)
        {
            hasDied = true;
            Died?.Invoke();
            Debug.Log("[Player 사망] 플레이어가 사망했습니다.");
            gameObject.SetActive(false);
        }
    }

    private void AutoAttack()
    {
        if (data == null) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, data.attackRange);

            Transform closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var col in hitColliders)
            {
                if (col.CompareTag(enemyTag))
                {
                    float dist = Vector2.Distance(transform.position, col.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestEnemy = col.transform;
                    }
                }
            }

            if (closestEnemy != null)
            {
                Shoot(closestEnemy);
                attackTimer = data.attackCooldown;
            }
        }
    }

    private void Shoot(Transform target)
    {
        if (bulletPrefab != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            bullet?.Init(target, data.attackDamage, data.bulletSpeed);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(enemyTag))
        {
            TakeDamage(10f, collision.gameObject);
        }
    }

    private void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, v, 0).normalized;

        if (data != null)
        {
            bool isSlowed = isParrying || isGuarding;
            float currentSpeed = isSlowed ? data.speed * parrySpeedRatio : data.speed;
            transform.position += dir * currentSpeed * Time.deltaTime;
        }
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null && data != null && data.maxHealth > 0f)
        {
            healthSlider.value = data.currentHealth / data.maxHealth;
        }
    }

    private void OnDisable()
    {
        isParrying = false;
        isGuarding = false;
        canGuardOrParry = true;
        SetPlayerColor(defaultColor);
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
