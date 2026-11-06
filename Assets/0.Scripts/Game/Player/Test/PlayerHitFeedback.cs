using System.Collections;
using UnityEngine;

/// <summary>피격 색 표시와 보스전 전용 히트스톱·밀림. 설정값은 보스전 종료 후에도 보존합니다.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHitFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField, Min(0.01f)] private float colorDuration = 0.25f;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.06f;
    [SerializeField, Min(0f)] private float recoilDistance = 0.25f;
    [SerializeField, Min(0.01f)] private float recoilDuration = 0.12f;

    private Player player;
    private Color originalColor;
    private Coroutine flashRoutine;
    private bool flashing;

    private void OnEnable()
    {
        player = GetComponent<Player>();
        if (player == null)
        {
            Debug.LogWarning("PlayerHitFeedback must be attached to a Player.", this);
            enabled = false;
            return;
        }
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>();
        player.Damaged += OnDamaged;
    }

    private void OnDamaged(float damage, GameObject source)
    {
        RestoreColor();
        if (targetRenderer != null)
        {
            originalColor = targetRenderer.color;
            Color tint = hitColor;
            tint.a = originalColor.a;
            targetRenderer.color = tint;
            flashing = true;
            flashRoutine = StartCoroutine(Flash());
        }
        // 일반 전투는 유효 값이 모두 0인 것과 같으며, 이동 정지/코루틴도 시작하지 않습니다.
        // 매 피격 시 현재 단계를 조회하므로 보스 사망 후에는 즉시 일반 피격으로 돌아갑니다.
        if (IsBossFight())
            player.BeginHitFeedback(this, source, hitStopDuration, recoilDistance, recoilDuration);
    }

    private bool IsBossFight()
    {
        foreach (var encounter in FindObjectsByType<BossEncounterController>())
            if (encounter.isActiveAndEnabled && encounter.gameObject.scene == gameObject.scene
                && encounter.Phase == EncounterPhase.BossFight)
                return true;
        return false;
    }

    private IEnumerator Flash()
    {
        float duration = float.IsNaN(colorDuration) || float.IsInfinity(colorDuration)
            ? 0.25f : Mathf.Max(0.01f, colorDuration);
        yield return new WaitForSeconds(duration);
        RestoreColor();
    }

    private void RestoreColor()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = null;
        if (flashing && targetRenderer != null) targetRenderer.color = originalColor;
        flashing = false;
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.Damaged -= OnDamaged;
            player.CancelHitFeedback(this);
        }
        RestoreColor();
    }
}
