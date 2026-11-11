using UnityEngine;

/// <summary>
/// SoulArena 잡몹에 MainScene 일반 좀비(Enemy 프리팹) 스프라이트·애니메이터를 빌보드로 입힌다.
/// 추적·접촉 피해는 SoulArenaTrashMob이 소유하고, 이 컴포넌트는 표시만 담당한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulTrashMobSpriteSkin : MonoBehaviour
{
    public const string BodyName = "MainZombieSprite";

    /// <summary>SoulArenaBootstrap이 설정. 끄면 새로 생기는 잡몹은 구체로 남는다.</summary>
    public static bool UseMainZombieSprite = true;

    private static readonly int FacingXParam = Animator.StringToHash("FacingX");
    private static readonly int FacingYParam = Animator.StringToHash("FacingY");
    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
    private static SoulArenaSpriteSet cachedSet;

    private Transform body;
    private SpriteRenderer bodyRenderer;
    private Animator bodyAnimator;
    private Transform shadow;
    private Material bodyMaterial;
    private Material shadowMaterial;
    private float scale;
    private float lift;
    private Vector2 facing = Vector2.down;
    private Vector3 lastPosition;
    private Transform player;

    public SpriteRenderer BodyRenderer => bodyRenderer;
    public Animator BodyAnimator => bodyAnimator;
    public Vector2 Facing => facing;
    public float Scale => scale;
    public bool Applied => bodyAnimator != null && bodyAnimator.runtimeAnimatorController != null;

    public static SoulTrashMobSpriteSkin Apply(SoulArenaTrashMob target)
    {
        if (target == null) return null;
        var existing = target.GetComponent<SoulTrashMobSpriteSkin>();
        if (existing != null) return existing;
        var skin = target.gameObject.AddComponent<SoulTrashMobSpriteSkin>();
        return skin.Applied ? skin : null;
    }

    private void Awake()
    {
        if (cachedSet == null) cachedSet = Resources.Load<SoulArenaSpriteSet>(SoulArenaSpriteSet.ResourcePath);
        if (cachedSet == null || cachedSet.ZombieController == null)
        {
            Debug.LogWarning($"[SoulPipeline] Resources/{SoulArenaSpriteSet.ResourcePath} 좀비 컨트롤러 없음 → 구체 유지", this);
            Destroy(this);
            return;
        }

        scale = cachedSet.ZombieScale;
        body = SoulSpriteBillboard.CreateBody(transform, BodyName, scale, cachedSet.ZombieController,
            out bodyRenderer, out bodyAnimator, out bodyMaterial);
        shadow = SoulSpriteBillboard.CreateBlobShadow(transform, new Vector2(1.0f, 0.55f), out shadowMaterial);
        // 같은 프레임에 몰려 생겨도 걸음이 맞지 않게 시작 위치를 흩뜨린다.
        bodyAnimator.Update(Random.value);
        lastPosition = transform.position;
        LateUpdate();
    }

    private void OnDestroy()
    {
        if (bodyMaterial != null) Destroy(bodyMaterial);
        if (shadowMaterial != null) Destroy(shadowMaterial);
    }

    private void LateUpdate()
    {
        if (body == null) return;
        SoulSpriteBillboard.Place(body, bodyRenderer, transform.position, scale, ref lift);
        if (shadow != null) shadow.rotation = Quaternion.Euler(90f, 0f, 0f);

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        lastPosition = transform.position;

        if (player == null)
        {
            var avatar = FindAnyObjectByType<SoulPipelineAvatar>();
            if (avatar != null) player = avatar.transform;
        }
        Vector3 world = player != null ? player.position - transform.position : transform.forward;
        Vector2 screenDir = SoulSpriteBillboard.ToScreenDirection(world, SoulSpriteBillboard.ViewRotation());
        if (screenDir.sqrMagnitude > 0.0001f) facing = SoulSpriteBillboard.Quantize8(screenDir);

        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        bodyAnimator.SetFloat(FacingXParam, facing.x);
        bodyAnimator.SetFloat(FacingYParam, facing.y);
        bodyAnimator.SetBool(IsMovingParam, speed > 0.3f);
    }
}
