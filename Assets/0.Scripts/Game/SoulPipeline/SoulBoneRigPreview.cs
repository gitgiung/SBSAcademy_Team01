using UnityEngine;

/// <summary>
/// F3: 플레이어 양옆에 원본 프레임 애니메이션(왼쪽)과 2D 뼈대 리깅 시험판(오른쪽)을 세워 비교한다.
/// F4: 대기 → 걷기 → 공격 전환. 판정·규칙과 무관한 표시 전용이다.
/// </summary>
public sealed class SoulBoneRigPreview : MonoBehaviour
{
    static readonly int FacingXParam = Animator.StringToHash("FacingX");
    static readonly int FacingYParam = Animator.StringToHash("FacingY");
    static readonly int IsMovingParam = Animator.StringToHash("IsMoving");

    Transform originalRoot;
    Transform originalBody;
    SpriteRenderer originalRenderer;
    Animator originalAnimator;
    Material originalMaterial;
    float originalLift;
    Transform rigRoot;
    SoulPlayerBoneRig rig;
    SoulPipelineAvatar player;
    float attackTimer;
    GUIStyle style;

    public float SideOffset { get; set; } = 1.5f;
    public bool IsOpen => rigRoot != null;
    public SoulPlayerBoneRig Rig => rig;
    public Animator OriginalAnimator => originalAnimator;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3)) SetOpen(!IsOpen);
        if (IsOpen && Input.GetKeyDown(KeyCode.F4))
            SetMotion((SoulPlayerBoneRig.Motion)(((int)rig.CurrentMotion + 1) % 3));
    }

    public void SetOpen(bool open)
    {
        if (open == IsOpen) return;
        if (!open)
        {
            Destroy(rigRoot.gameObject);
            if (originalRoot != null) Destroy(originalRoot.gameObject);
            if (originalMaterial != null) Destroy(originalMaterial);
            rigRoot = null;
            rig = null;
            return;
        }

        var set = Resources.Load<SoulArenaSpriteSet>(SoulArenaSpriteSet.ResourcePath);
        if (set == null || set.RigPrototypeSprite == null)
        {
            Debug.LogWarning("[SoulPipeline] SoulArenaSpriteSet의 Rig Prototype Sprite가 비어 있어 뼈대 시험판을 열 수 없음", this);
            return;
        }

        rigRoot = new GameObject("BoneRigPreview_Rig").transform;
        rig = SoulPlayerBoneRig.Create(rigRoot, set.RigPrototypeSprite, SoulPlayerSpriteSkin.MainPlayerScale);
        SoulSpriteBillboard.CreateBlobShadow(rigRoot, new Vector2(0.9f, 0.5f), out _);

        var controller = Resources.Load<RuntimeAnimatorController>(SoulPlayerSpriteSkin.ControllerPath);
        if (controller != null)
        {
            originalRoot = new GameObject("BoneRigPreview_Original").transform;
            originalBody = SoulSpriteBillboard.CreateBody(originalRoot, "Body", SoulPlayerSpriteSkin.MainPlayerScale, controller,
                out originalRenderer, out originalAnimator, out originalMaterial);
            SoulSpriteBillboard.CreateBlobShadow(originalRoot, new Vector2(0.9f, 0.5f), out _);
            originalLift = 0f;
        }
        SetMotion(SoulPlayerBoneRig.Motion.Idle);
        LateUpdate();
    }

    public void SetMotion(SoulPlayerBoneRig.Motion motion)
    {
        if (rig == null) return;
        rig.CurrentMotion = motion;
        attackTimer = 0f;
        if (motion == SoulPlayerBoneRig.Motion.Attack) PlayOriginalAttack();
    }

    void LateUpdate()
    {
        if (!IsOpen) return;
        if (player == null) player = FindAnyObjectByType<SoulPipelineAvatar>();
        Vector3 center = player != null ? player.transform.position : Vector3.zero;
        Vector3 right = SoulSpriteBillboard.ViewRotation() * Vector3.right;
        right.y = 0f;
        right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
        rigRoot.position = center + right * SideOffset;

        if (originalAnimator == null) return;
        originalRoot.position = center - right * SideOffset;
        originalAnimator.SetFloat(FacingXParam, 0f);
        originalAnimator.SetFloat(FacingYParam, -1f);
        originalAnimator.SetBool(IsMovingParam, rig.CurrentMotion == SoulPlayerBoneRig.Motion.Walk);
        if (rig.CurrentMotion == SoulPlayerBoneRig.Motion.Attack)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= SoulPlayerBoneRig.AttackCycleSeconds)
            {
                attackTimer -= SoulPlayerBoneRig.AttackCycleSeconds;
                PlayOriginalAttack();
            }
        }
        SoulSpriteBillboard.Place(originalBody, originalRenderer, originalRoot.position, SoulPlayerSpriteSkin.MainPlayerScale, ref originalLift);
    }

    void PlayOriginalAttack()
    {
        if (originalAnimator == null) return;
        int state = Animator.StringToHash(originalAnimator.GetLayerName(0) + ".LightAttack");
        if (originalAnimator.HasState(0, state)) originalAnimator.Play(state, 0, 0f);
    }

    void OnGUI()
    {
        if (!IsOpen) return;
        style ??= new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
        string name = rig.CurrentMotion == SoulPlayerBoneRig.Motion.Idle ? "대기"
            : rig.CurrentMotion == SoulPlayerBoneRig.Motion.Walk ? "걷기" : "공격";
        string wand = rig.WandRoot != null ? $" · 지팡이소켓 제거{rig.ErasedWandPixels}px" : "";
        GUI.Box(new Rect(Screen.width * 0.5f - 360f, Screen.height - 46f, 720f, 32f),
            $"뼈대 시험판: 왼쪽 원본 | 오른쪽 뼈대+3D지팡이 — {name}{wand} (F4 전환, F3 닫기)", style);
    }

    void OnDestroy()
    {
        if (IsOpen) SetOpen(false);
    }
}
