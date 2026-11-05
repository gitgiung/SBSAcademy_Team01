using System.Collections;
using UnityEngine;

/// <summary>Optional damage reaction prototype. Add to Player to test; remove to disable.</summary>
[DisallowMultipleComponent]
public sealed class PlayerHitFeedbackTest : MonoBehaviour
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
            Debug.LogWarning("PlayerHitFeedbackTest must be attached to a Player.", this);
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
        player.BeginTestHitFeedback(this, source, hitStopDuration, recoilDistance, recoilDuration);
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
            player.CancelTestHitFeedback(this);
        }
        RestoreColor();
    }
}