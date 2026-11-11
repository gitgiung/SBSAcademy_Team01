using UnityEngine;

/// <summary>
/// SoulArena 플레이어가 화면 정면(아래)을 볼 때만 원본 프레임 애니메이션 대신 2D 뼈대 리깅(+손 소켓 지팡이)을 보여 준다.
/// 이동·패링·피격·대시 판정은 SoulPipelineAvatar가 그대로 소유하고 이 컴포넌트는 표시만 바꾼다.
/// 뼈대는 south_01 한 장으로 만들어서 다른 7방향·대시·패링은 원본 프레임 애니메이션이 그대로 담당한다.
/// F5: 뼈대/원본 표시 전환(비교용).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SoulPlayerSpriteSkin))]
public sealed class SoulPlayerBoneRigSkin : MonoBehaviour
{
    public const string RootName = "BoneRigRoot";
    public const KeyCode ToggleKey = KeyCode.F5;
    // 원본 LightAttack 클립 길이(12fps 9프레임 = 0.75초).
    public const float LightAttackSeconds = 0.75f;

    private SoulPlayerSpriteSkin skin;
    private SoulPipelineAvatar avatar;
    private Transform rigRoot;
    private SoulPlayerBoneRig rig;
    private bool userEnabled = true;
    private bool active;
    private float swingLeft;
    private bool motionSet;

    public SoulPlayerBoneRig Rig => rig;
    /// <summary>지금 뼈대가 화면에 보이는지(정면 + 대시·패링 아님 + 사용자 설정).</summary>
    public bool Active => active;
    public bool UserEnabled { get => userEnabled; set => userEnabled = value; }

    /// <summary>플레이어 스킨 위에 뼈대 표시를 얹는다. SpriteSet 지정이 없으면 원본 스프라이트만 쓰고 null.</summary>
    public static SoulPlayerBoneRigSkin Apply(SoulPlayerSpriteSkin target)
    {
        if (target == null) return null;
        var existing = target.GetComponent<SoulPlayerBoneRigSkin>();
        if (existing != null) return existing;
        var added = target.gameObject.AddComponent<SoulPlayerBoneRigSkin>();
        return added != null && added.rig != null ? added : null;
    }

    private void Awake()
    {
        skin = GetComponent<SoulPlayerSpriteSkin>();
        avatar = GetComponent<SoulPipelineAvatar>();
        var set = Resources.Load<SoulArenaSpriteSet>(SoulArenaSpriteSet.ResourcePath);
        if (skin == null || set == null || set.RigPrototypeSprite == null)
        {
            Debug.LogWarning("[SoulPipeline] 뼈대 플레이어 생략: SoulArenaSpriteSet의 Rig Prototype Sprite 또는 스프라이트 스킨 없음 → 원본 스프라이트 유지", this);
            Destroy(this);
            return;
        }

        rigRoot = new GameObject(RootName).transform;
        rigRoot.SetParent(transform, false);
        rig = SoulPlayerBoneRig.Create(rigRoot, set.RigPrototypeSprite, SoulPlayerSpriteSkin.MainPlayerScale);
        if (rig == null)
        {
            Destroy(rigRoot.gameObject);
            Destroy(this);
            return;
        }
        rig.AttackDuration = LightAttackSeconds;
        rigRoot.gameObject.SetActive(false);

        if (avatar != null) avatar.LightSwung += OnLightSwung;
        // 뼈대 렌더러도 패링·피격 틴트를 받도록 다시 수집한다.
        GetComponent<SoulPlayerParryFlash>()?.Rebind();
    }

    private void OnDestroy()
    {
        if (avatar != null) avatar.LightSwung -= OnLightSwung;
        if (skin != null && skin.BodyRenderer != null) skin.BodyRenderer.enabled = true;
        if (rigRoot != null) Destroy(rigRoot.gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(ToggleKey)) userEnabled = !userEnabled;
        if (swingLeft > 0f) swingLeft -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (rig == null || skin == null || skin.BodyRenderer == null) return;

        Vector2 facing = skin.Facing;
        bool south = facing.y < -0.7f && Mathf.Abs(facing.x) < 0.2f;
        bool dashing = avatar != null && avatar.IsDashing;
        bool parrying = avatar != null && avatar.IsParrying;
        bool wanted = userEnabled && south && !dashing && !parrying;

        if (wanted != active)
        {
            active = wanted;
            rigRoot.gameObject.SetActive(active);
            motionSet = false;
        }
        skin.BodyRenderer.enabled = !active;
        if (!active) return;

        Quaternion camRot = SoulSpriteBillboard.ViewRotation();
        rigRoot.SetPositionAndRotation(transform.position + camRot * Vector3.up * rig.BottomBelowRoot, camRot);

        var motion = swingLeft > 0f ? SoulPlayerBoneRig.Motion.Attack
            : avatar != null && avatar.IsMoving ? SoulPlayerBoneRig.Motion.Walk
            : SoulPlayerBoneRig.Motion.Idle;
        if (!motionSet || rig.CurrentMotion != motion)
        {
            rig.CurrentMotion = motion;
            motionSet = true;
        }
    }

    private void OnLightSwung()
    {
        swingLeft = LightAttackSeconds;
        if (rig != null && active)
        {
            rig.CurrentMotion = SoulPlayerBoneRig.Motion.Attack;
            motionSet = true;
        }
    }
}
