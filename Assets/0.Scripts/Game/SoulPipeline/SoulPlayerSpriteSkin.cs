using UnityEngine;

/// <summary>
/// SoulArena 플레이어에 MainScene 플레이어의 스프라이트·애니메이터를 빌보드로 입힌다.
/// 이동·패링·피격 판정은 SoulPipelineAvatar가 소유하고, 이 컴포넌트는 표시만 담당한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulPlayerSpriteSkin : MonoBehaviour
{
    public const string ControllerPath = "PlayerAnimator/Player";
    public const string BodyName = "MainPlayerSprite";
    // MainScene Player의 Transform 스케일(64px, PPU 100 → 약 1.92 유닛).
    public const float MainPlayerScale = 3f;

    private static readonly int FacingXParam = Animator.StringToHash("FacingX");
    private static readonly int FacingYParam = Animator.StringToHash("FacingY");
    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
    private static readonly int IsDashingParam = Animator.StringToHash("IsDashing");
    private static readonly int ChargeSpeedParam = Animator.StringToHash("ChargeAnimationSpeed");
    private static readonly int ParryParam = Animator.StringToHash("Parry");

    private SoulPipelineAvatar avatar;
    private Transform body;
    private SpriteRenderer bodyRenderer;
    private Animator bodyAnimator;
    private Transform shadow;
    private Material bodyMaterial;
    private Material shadowMaterial;
    private Vector2 facing = Vector2.down;
    private bool wasParrying;
    private float lift;

    public SpriteRenderer BodyRenderer => bodyRenderer;
    public Animator BodyAnimator => bodyAnimator;
    public Vector2 Facing => facing;
    public bool Applied => bodyAnimator != null && bodyAnimator.runtimeAnimatorController != null;

    /// <summary>플레이어에 스킨을 붙인다. 컨트롤러를 못 찾으면 기존 3D 몸을 유지하고 null.</summary>
    public static SoulPlayerSpriteSkin Apply(SoulPipelineAvatar target)
    {
        if (target == null) return null;
        var existing = target.GetComponent<SoulPlayerSpriteSkin>();
        if (existing != null) return existing;
        var skin = target.gameObject.AddComponent<SoulPlayerSpriteSkin>();
        return skin.Applied ? skin : null;
    }

    private void Awake()
    {
        avatar = GetComponent<SoulPipelineAvatar>();
        var controller = Resources.Load<RuntimeAnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogWarning($"[SoulPipeline] Resources/{ControllerPath} 없음 → 3D 몸 유지", this);
            Destroy(this);
            return;
        }

        body = SoulSpriteBillboard.CreateBody(transform, BodyName, MainPlayerScale, controller,
            out bodyRenderer, out bodyAnimator, out bodyMaterial);
        shadow = SoulSpriteBillboard.CreateBlobShadow(transform, new Vector2(1.1f, 0.6f), out shadowMaterial);

        if (avatar != null) avatar.LightSwung += OnLightSwung;
        GetComponent<SoulPlayerParryFlash>()?.Rebind();
        LateUpdate();
        bodyAnimator.Update(0f);
        LateUpdate();
    }

    private void OnDestroy()
    {
        if (avatar != null) avatar.LightSwung -= OnLightSwung;
        if (bodyMaterial != null) Destroy(bodyMaterial);
        if (shadowMaterial != null) Destroy(shadowMaterial);
    }

    private void LateUpdate()
    {
        if (body == null) return;
        SoulSpriteBillboard.Place(body, bodyRenderer, transform.position, MainPlayerScale, ref lift);

        Vector2 screenDir = SoulSpriteBillboard.ToScreenDirection(transform.forward, SoulSpriteBillboard.ViewRotation());
        if (screenDir.sqrMagnitude > 0.0001f) facing = SoulSpriteBillboard.Quantize8(screenDir);

        bool dashing = avatar != null && avatar.IsDashing;
        bodyAnimator.SetFloat(FacingXParam, facing.x);
        bodyAnimator.SetFloat(FacingYParam, facing.y);
        bodyAnimator.SetBool(IsMovingParam, avatar != null && avatar.IsMoving);
        bodyAnimator.SetBool(IsDashingParam, dashing);
        float dashSeconds = avatar != null ? avatar.DashSeconds : 0.18f;
        bodyAnimator.SetFloat(ChargeSpeedParam, dashing ? (8f / 12f) / Mathf.Max(0.01f, dashSeconds) : 1f);

        bool parrying = avatar != null && avatar.IsParrying;
        if (parrying && !wasParrying) bodyAnimator.SetTrigger(ParryParam);
        wasParrying = parrying;

        if (shadow != null) shadow.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void OnLightSwung()
    {
        if (bodyAnimator == null) return;
        LateUpdate();
        int state = Animator.StringToHash(bodyAnimator.GetLayerName(0) + ".LightAttack");
        if (bodyAnimator.HasState(0, state)) bodyAnimator.Play(state, 0, 0f);
    }
}
