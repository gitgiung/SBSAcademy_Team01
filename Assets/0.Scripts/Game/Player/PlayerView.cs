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
    private TMPro.TextMeshProUGUI healthValueText;
    private TMPro.TextMeshProUGUI mpValueText;
    private TMPro.TextMeshProUGUI expValueText;
    private readonly PlayerData data;
    private readonly Animator animator;
    private readonly PlayerDefenseVfx defenseVfx;
    private readonly Player player;
    private readonly Transform owner;
    private readonly SpriteRenderer playerRenderer;
    private LineRenderer heavyChargeIndicator;
    private Material heavyChargeMaterial;
    private Vector2 facingDirection = Vector2.down;
    public Vector2 FacingDirection => facingDirection;
    public RuntimeAnimatorController AnimationController => animator != null ? animator.runtimeAnimatorController : null;
    public bool AnimationAvailable => animator != null && animator.isActiveAndEnabled && AnimationController != null;

    private int AnimationHash(string state) => Animator.StringToHash(animator.GetLayerName(0) + "." + state);
    public bool CanPlayLightAttack(string state) => AnimationAvailable && !string.IsNullOrEmpty(state)
        && animator.HasState(0, AnimationHash(state)) && animator.HasState(0, AnimationHash("Idle"));

    public void PlayLightAttack(string state, Vector2 direction)
    {
        SetMovementAnimation(direction, false, false, 1f);
        // 중단했던 같은 상태를 다시 실행해도 새로운 OnStateEnter/실행 ID를 받도록 경계를 확정합니다.
        animator.Play(AnimationHash("Idle"), 0, 0f);
        animator.Update(0f);
        animator.Play(AnimationHash(state), 0, 0f);
        animator.Update(0f);
    }

    public PlayerView(Transform player, SpriteRenderer spriteRenderer, Slider healthSlider, PlayerData data, Animator animator)
    {
        this.player = player.GetComponent<Player>();
        owner = player;
        playerRenderer = spriteRenderer;
        this.healthSlider = healthSlider;
        this.data = data;
        this.animator = animator;
        ConfigureDisplayBar(healthSlider);
        healthValueText = CreateResourceValueText(healthSlider);
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
        mpValueText = CreateResourceValueText(mpSlider);
        expValueText = CreateResourceValueText(expSlider);
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

    private static TMPro.TextMeshProUGUI CreateResourceValueText(UnityEngine.UI.Slider slider)
    {
        if (slider == null) return null;
        Transform existing = slider.transform.Find("ResourceValueText");
        if (existing != null) return existing.GetComponent<TMPro.TextMeshProUGUI>();
        var child = new GameObject("ResourceValueText", typeof(RectTransform));
        child.layer = slider.gameObject.layer;
        var rect = (RectTransform)child.transform;
        rect.SetParent(slider.transform, false);
        rect.anchorMin = new Vector2(0f, 0.25f);
        rect.anchorMax = new Vector2(1f, 0.75f);
        rect.offsetMin = new Vector2(8f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);
        var label = child.AddComponent<TMPro.TextMeshProUGUI>();
        label.font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        label.fontSize = 22f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = 22f;
        label.fontStyle = TMPro.FontStyles.Bold;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static void UpdateResourceValueText(TMPro.TMP_Text label, float current, float maximum)
    {
        if (label != null) label.text = $"{current:0.#} / {maximum:0.#}";
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

    public void PlayParryVfx() =>
        defenseVfx.PlayParry(player != null ? player.ParryVfxScale : Player.DefaultParryVfxScale);

    public void SetGuardAnimation(bool isGuarding)
    {
        defenseVfx.SetGuard(isGuarding);
        if (HasParameter(IsGuardingParameter, AnimatorControllerParameterType.Bool))
            animator.SetBool(IsGuardingParameter, isGuarding);
    }

    public void RefreshHealth()
    {
        UpdateBar(healthSlider, data.currentHealth, data.maxHealth);
        UpdateResourceValueText(healthValueText, data.currentHealth, data.maxHealth);
    }

    public void RefreshMP()
    {
        UpdateBar(mpSlider, data.CurrentMP, data.maxMP);
        UpdateResourceValueText(mpValueText, data.CurrentMP, data.maxMP);
    }

    public void RefreshEXP()
    {
        UpdateBar(expSlider, data.CurrentEXP, data.maxEXP);
        UpdateResourceValueText(expValueText, data.CurrentEXP, data.maxEXP);
    }

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

    // 임시 조준 표시입니다. 선의 길이는 공격 사거리나 피해 범위가 아닙니다.
    public void SetHeavyChargeIndicator(bool visible, Vector2 direction, float progress, int stage)
    {
        if (!visible)
        {
            if (heavyChargeIndicator != null) heavyChargeIndicator.enabled = false;
            return;
        }
        if (heavyChargeIndicator == null)
        {
            var child = new GameObject("Heavy Charge Aim");
            child.transform.SetParent(owner, false);
            heavyChargeIndicator = child.AddComponent<LineRenderer>();
            heavyChargeIndicator.useWorldSpace = false;
            heavyChargeIndicator.positionCount = 5;
            heavyChargeIndicator.startWidth = .07f;
            heavyChargeIndicator.endWidth = .07f;
            heavyChargeIndicator.numCornerVertices = 2;
            heavyChargeMaterial = new Material(Shader.Find("Sprites/Default"));
            heavyChargeIndicator.sharedMaterial = heavyChargeMaterial;
            heavyChargeIndicator.sortingLayerID = playerRenderer != null ? playerRenderer.sortingLayerID : 0;
            heavyChargeIndicator.sortingOrder = (playerRenderer != null ? playerRenderer.sortingOrder : 0) + 101;
        }
        Vector2 forward = direction.sqrMagnitude > .0001f ? direction.normalized : facingDirection;
        Vector2 side = new Vector2(-forward.y, forward.x);
        Vector2 tip = forward * Mathf.Lerp(.8f, 1.5f, Mathf.Clamp01(progress));
        heavyChargeIndicator.SetPosition(0, forward * .35f);
        heavyChargeIndicator.SetPosition(1, tip);
        heavyChargeIndicator.SetPosition(2, tip - forward * .25f + side * .18f);
        heavyChargeIndicator.SetPosition(3, tip);
        heavyChargeIndicator.SetPosition(4, tip - forward * .25f - side * .18f);
        Color color = stage >= 3 ? new Color(1f, .35f, .15f) : stage == 2 ? Color.yellow : Color.cyan;
        heavyChargeIndicator.startColor = color;
        heavyChargeIndicator.endColor = color;
        heavyChargeIndicator.enabled = true;
    }

    public void Reset()
    {
        SetHeavyChargeIndicator(false, Vector2.zero, 0f, 0);
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
        if (heavyChargeIndicator != null) Object.Destroy(heavyChargeIndicator.gameObject);
        if (heavyChargeMaterial != null) Object.Destroy(heavyChargeMaterial);
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
