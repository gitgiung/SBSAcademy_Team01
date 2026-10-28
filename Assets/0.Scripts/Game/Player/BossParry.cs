using System.Collections;
using UnityEngine;

public class BossParry : MonoBehaviour
{
    [Header("렌더러 설정")]
    [SerializeField] private SpriteRenderer bossSpriteRenderer;

    [Header("타이밍 설정")]
    [SerializeField] private float patternInterval = 5f;     // 패턴 실행 간격 (5초)
    [SerializeField] private float yellowDuration = 0.5f;     // 1단계: 노란색 준비 시간 (0.5초)
    [SerializeField] private float orangeDuration = 0.3f;     // 2단계: 주황색 준비 시간 (0.3초)
    [SerializeField] private float redParryWindow = 0.133f;   // 3단계: 빨간색 패링 가능 유효 시간

    public GameObject PlayerObject;
    [SerializeField] private float attackDamage = 10f;       // 공격 데미지

    private bool isParryable = false;
    private Coroutine attackRoutine;

    private void Start()
    {
        if (bossSpriteRenderer == null)
            bossSpriteRenderer = GetComponent<SpriteRenderer>();

        attackRoutine = StartCoroutine(AutoAttackLoop());
    }

    private IEnumerator AutoAttackLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(patternInterval);

            // 1. 노란색 (준비 1단계)
            Set2DColor(Color.yellow);
            yield return new WaitForSeconds(yellowDuration);

            // 2. 주황색 (준비 2단계)
            Set2DColor(new Color(1f, 0.5f, 0f));
            yield return new WaitForSeconds(orangeDuration);

            // 3. 빨간색 (패링 유효 창 시작)
            isParryable = true;
            Set2DColor(Color.red);

            // 패링 유효 시간(8프레임) 동안 대기
            yield return new WaitForSeconds(redParryWindow);

            // 유효 시간이 지날 때까지 플레이어가 패링을 안 해서 여전히 isParryable이 true라면 공격 성공 (데미지 적용)
            if (isParryable)
            {
                if (PlayerObject != null && PlayerObject.TryGetComponent<Player>(out var playerScript))
                {
                    Debug.Log("[보스 공격 성공] 플레이어가 패링하지 못함!");
                    playerScript.TakeDamage(attackDamage);
                }
            }

            // 패턴 종료 후 기본 상태 복구
            isParryable = false;
            Set2DColor(Color.white);
        }
    }

    private void Set2DColor(Color color)
    {
        if (bossSpriteRenderer != null)
            bossSpriteRenderer.color = color;
    }

    /// <summary>
    /// 플레이어가 E키를 눌러 패링을 시도할 때 호출됨
    /// </summary>
    public bool TryParry()
    {
        if (isParryable)
        {
            isParryable = false;
            Set2DColor(Color.purple); // 보라색으로 변하며 패링 성공 피드백

            Debug.Log("[성공] 패링 성공!");

            // 패턴 코루틴을 중단하고 루프 재시작
            if (attackRoutine != null)
                StopCoroutine(attackRoutine);

            attackRoutine = StartCoroutine(AutoAttackLoop());

            return true;
        }
        return false;
    }
}