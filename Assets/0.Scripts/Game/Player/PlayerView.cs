using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates player-facing visuals without owning gameplay rules.</summary>
internal sealed class PlayerView
{
    private static readonly int ParryParameter = Animator.StringToHash("Parry");
    private static readonly int IsGuardingParameter = Animator.StringToHash("IsGuarding");
    private readonly Slider healthSlider;
    private readonly PlayerData data;
    private readonly Animator animator;
    private Vector2 facingDirection = Vector2.down;

    public PlayerView(SpriteRenderer spriteRenderer, Slider healthSlider, PlayerData data, Animator animator)
    {
        this.healthSlider = healthSlider;
        this.data = data;
        this.animator = animator;
        data.HealthChanged += OnHealthChanged;
        RefreshHealth();
    }

    public void PlayParryAnimation()
    {
        if (HasParameter(ParryParameter, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(ParryParameter);
    }

    public void SetGuardAnimation(bool isGuarding)
    {
        if (HasParameter(IsGuardingParameter, AnimatorControllerParameterType.Bool))
            animator.SetBool(IsGuardingParameter, isGuarding);
    }

    public void RefreshHealth()
    {
        if (healthSlider != null && data.maxHealth > 0f)
            healthSlider.value = Mathf.Clamp01(data.currentHealth / data.maxHealth);
    }

    public void SetMovementAnimation(Vector2 moveDirection, bool isMoving, bool isDashing, float dashDuration)
    {
        if (moveDirection.sqrMagnitude > 0.0001f)
            facingDirection = QuantizeDirection(moveDirection);

        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetFloat("FacingX", facingDirection.x);
        animator.SetFloat("FacingY", facingDirection.y);
        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsDashing", isDashing);
        animator.SetFloat("ChargeAnimationSpeed", isDashing ? (8f / 12f) / Mathf.Max(0.01f, dashDuration) : 1f);
    }

    public void Reset()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsDashing", false);
            if (HasParameter(IsGuardingParameter, AnimatorControllerParameterType.Bool))
                animator.SetBool(IsGuardingParameter, false);
            if (HasParameter(ParryParameter, AnimatorControllerParameterType.Trigger))
                animator.ResetTrigger(ParryParameter);
            animator.SetFloat("ChargeAnimationSpeed", 1f);
        }
    }

    public void Dispose()
    {
        if (data != null) data.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float current, float maximum) => RefreshHealth();

    private static Vector2 QuantizeDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x);
        float quantizedAngle = Mathf.Round(angle / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
        return new Vector2(Mathf.Cos(quantizedAngle), Mathf.Sin(quantizedAngle));
    }

    private bool HasParameter(int parameterHash, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == parameterType)
                return true;
        }

        return false;
    }
}
