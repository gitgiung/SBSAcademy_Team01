using System.Collections;
using UnityEngine;

public class BossParryTest : MonoBehaviour
{
    [SerializeField] private MeshRenderer bossMeshRenderer;

    [Header("타이밍 설정")]
    [SerializeField] private float patternInterval = 5f;
    [SerializeField] private float yellowDuration = 0.5f;
    [SerializeField] private float orangeDuration = 0.3f;
    [SerializeField] private float redParryWindow = 0.133f; // 기획서 8프레임(약 0.13초) 반영

    private bool isParryable = false;
    private Coroutine attackRoutine;

    private void Start()
    {
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

            // 3. 빨간색 (패링 가능 타이밍 - 8프레임)
            isParryable = true;
            Set2DColor(Color.red);
            yield return new WaitForSeconds(redParryWindow);

            // 복귀
            isParryable = false;
            Set2DColor(Color.white);
        }
    }

    private void Set2DColor(Color color)
    {
        if (bossMeshRenderer != null && bossMeshRenderer.material != null)
        {
            bossMeshRenderer.material.SetColor("_Color", color);
        }
    }

    public bool TryParry()
    {
        if (isParryable)
        {
            isParryable = false;
            Set2DColor(Color.white);

            Debug.Log("[보스] 패링당함! (그로기 게이지 대량 감소 / 자세 무너짐)");

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }
            attackRoutine = StartCoroutine(AutoAttackLoop());

            return true;
        }
        return false;
    }
}