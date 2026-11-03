using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates player-facing visuals without owning gameplay rules.</summary>
internal sealed class PlayerView
{
    private static readonly int ParryParameter = Animator.StringToHash("Parry");
    private static readonly int IsGuardingParameter = Animator.StringToHash("IsGuarding");
    private readonly Slider healthSlider;
    private Slider mpSlider;
    private Slider expSlider;
    private readonly PlayerData data;
    private readonly Animator animator;
    private readonly PlayerDefenseVfx defenseVfx;
    private Vector2 facingDirection = Vector2.down;
    public Vector2 FacingDirection => facingDirection;

    public PlayerView(Transform player, SpriteRenderer spriteRenderer, Slider healthSlider, PlayerData data, Animator animator)
    {
        this.healthSlider = healthSlider;
        this.data = data;
        this.animator = animator;
        ConfigureDisplayBar(healthSlider);
        defenseVfx = player.gameObject.AddComponent<PlayerDefenseVfx>();
        defenseVfx.Initialize(player, spriteRenderer);
        data.HealthChanged += OnHealthChanged;
        data.MPChanged += OnMPChanged;
        data.EXPChanged += OnEXPChanged;
        RefreshHealth();
    }

    public void BindResourceSliders(Slider mp, Slider exp)
    {
        mpSlider = mp;
        expSlider = exp;
        ConfigureDisplayBar(mpSlider);
        ConfigureDisplayBar(expSlider);
        RefreshMP();
        RefreshEXP();
    }

    private static void ConfigureDisplayBar(Slider slider)
    {
        if (slider == null) return;
        // HP 등을 선택한 뒤 좌우 UI 입력으로 값이 바뀌거나 비활성 색으로 흐려지지 않게 합니다.
        slider.transition = UnityEngine.UI.Selectable.Transition.None;
        slider.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        slider.interactable = false;
    }

    private static void UpdateBar(Slider slider, float current, float maximum)
    {
        if (slider == null) return;
        float ratio = maximum > 0f && !float.IsNaN(maximum) && !float.IsInfinity(maximum)
            && !float.IsNaN(current) && !float.IsInfinity(current)
            ? Mathf.Clamp01(current / maximum) : 0f;
        slider.SetValueWithoutNotify(Mathf.Lerp(slider.minValue, slider.maxValue, ratio));
    }

    public void PlayParryAnimation()
    {
        if (HasParameter(ParryParameter, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(ParryParameter);
    }

    public void PlayParryVfx() => defenseVfx.PlayParry();

    public void SetGuardAnimation(bool isGuarding)
    {
        defenseVfx.SetGuard(isGuarding);
        if (HasParameter(IsGuardingParameter, AnimatorControllerParameterType.Bool))
            animator.SetBool(IsGuardingParameter, isGuarding);
    }

    public void RefreshHealth()
    {
        UpdateBar(healthSlider, data.currentHealth, data.maxHealth);
    }

    public void RefreshMP() => UpdateBar(mpSlider, data.CurrentMP, data.maxMP);
    public void RefreshEXP() => UpdateBar(expSlider, data.CurrentEXP, data.maxEXP);

    public void RefreshResources()
    {
        RefreshHealth();
        RefreshMP();
        RefreshEXP();
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
        defenseVfx.ResetVfx();
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
        if (data == null) return;
        data.HealthChanged -= OnHealthChanged;
        data.MPChanged -= OnMPChanged;
        data.EXPChanged -= OnEXPChanged;
    }

    private void OnHealthChanged(float current, float maximum) => RefreshHealth();
    private void OnMPChanged(float current, float maximum) => RefreshMP();
    private void OnEXPChanged(float current, float maximum) => RefreshEXP();

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
