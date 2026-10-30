using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates player-facing visuals without owning gameplay rules.</summary>
internal sealed class PlayerView
{
    private static readonly Color DefaultColor = new Color(1f, 1f, 1f, 1f);
    private readonly SpriteRenderer spriteRenderer;
    private readonly Slider healthSlider;
    private readonly PlayerData data;

    public PlayerView(SpriteRenderer spriteRenderer, Slider healthSlider, PlayerData data)
    {
        this.spriteRenderer = spriteRenderer;
        this.healthSlider = healthSlider;
        this.data = data;
        data.HealthChanged += OnHealthChanged;
        RefreshHealth();
        SetDefaultColor();
    }

    public void SetParryColor() => SetColor(Color.blue); // Test용
    public void SetGuardColor() => SetColor(Color.red); // Test용
    public void SetDefaultColor() => SetColor(DefaultColor); // Test용

    public void RefreshHealth()
    {
        if (healthSlider != null && data.maxHealth > 0f)
            healthSlider.value = Mathf.Clamp01(data.currentHealth / data.maxHealth);
    }

    public void Reset() => SetDefaultColor();

    public void Dispose()
    {
        if (data != null) data.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float current, float maximum) => RefreshHealth();

    private void SetColor(Color color)
    {
        if (spriteRenderer != null) spriteRenderer.color = color;
    }
}
