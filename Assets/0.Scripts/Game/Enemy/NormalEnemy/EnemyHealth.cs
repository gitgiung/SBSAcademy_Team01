using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int maxHealth = 3;
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitColor = Color.white;

    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public event System.Action Died;

    private SpriteRenderer[] visuals;
    private Color[] originalColors;
    private Coroutine hitFlash;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        visuals = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
            originalColors[i] = visuals[i].color;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead || !isActiveAndEnabled)
            return;

        CurrentHealth = damage >= CurrentHealth ? 0 : CurrentHealth - damage;
        if (CurrentHealth == 0)
        {
            Die();
            return;
        }

        if (hitFlash != null) StopCoroutine(hitFlash);
        hitFlash = StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        foreach (var visual in visuals)
            if (visual != null) visual.color = hitColor;
        yield return new WaitForSeconds(Mathf.Max(0f, hitFlashDuration));
        RestoreColors();
        hitFlash = null;
    }

    private void Die()
    {
        IsDead = true;
        if (TryGetComponent<EnemyFollow>(out var follow)) follow.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var body)) body.simulated = false;
        Died?.Invoke();
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (hitFlash != null)
        {
            StopCoroutine(hitFlash);
            hitFlash = null;
        }
        RestoreColors();
    }

    private void RestoreColors()
    {
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i] != null) visuals[i].color = originalColors[i];
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        hitFlashDuration = Mathf.Max(0f, hitFlashDuration);
    }
}
