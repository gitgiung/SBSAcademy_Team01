using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 모션 읽기 보스 샌드박스를 런타임으로 구성한다.
/// Mixamo 최종 모델 전 임시 몸(몸·어깨·팔·손)으로 1패턴만 검증한다.
/// 현재 프로젝트 URP 2D Renderer 기준으로 임시 스프라이트 몸을 쓴다. 본/Mixamo 교체 지점은 Shoulder·Hand 계층이다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MotionBossPrototypeBootstrap : MonoBehaviour
{
    private MotionBossPrototypeAvatar avatar;
    private GUIStyle hudStyle;

    private void Awake()
    {
        Time.timeScale = 1f;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        BuildWorld();
    }

    private void BuildWorld()
    {
        EnsureCamera();
        EnsureGlobalLight2D();
        CreateGround();

        Transform player = CreateTempBody(
            "PrototypePlayer",
            new Vector3(-3f, 0f, 0f),
            new Color(0.35f, 0.75f, 1f, 1f),
            bodyHeight: 1.6f,
            withArm: false,
            out _,
            out _);

        avatar = player.gameObject.AddComponent<MotionBossPrototypeAvatar>();
        var playerView = player.Find("Body")?.GetComponent<SpriteRenderer>();
        if (playerView != null)
            avatar.BindView(playerView, playerView.color);

        Transform boss = CreateTempBody(
            "PrototypeMotionBoss",
            new Vector3(4.5f, 0f, 0f),
            new Color(0.75f, 0.28f, 0.22f, 1f),
            bodyHeight: 2.4f,
            withArm: true,
            out Transform shoulder,
            out Transform hand);

        var hitbox = hand.gameObject.AddComponent<MotionBossWeaponHitbox>();
        var circle = hand.GetComponent<CircleCollider2D>();
        if (circle == null) circle = hand.gameObject.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.35f;
        circle.enabled = false;

        var swing = boss.gameObject.AddComponent<MotionBossApproachSwing>();
        swing.Bind(player, shoulder, hitbox);

        // 바닥 텔레그래프 오브젝트를 만들지 않는다.
    }

    private static void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void EnsureGlobalLight2D()
    {
        if (Object.FindFirstObjectByType<Light2D>() != null) return;
        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static void CreateGround()
    {
        var ground = new GameObject("FlatGround");
        ground.transform.position = Vector3.zero;
        var sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = CreateUnitSprite();
        sr.color = new Color(0.22f, 0.24f, 0.28f, 1f);
        sr.sortingOrder = -20;
        ground.transform.localScale = new Vector3(28f, 16f, 1f);
    }

    private static Transform CreateTempBody(
        string name,
        Vector3 position,
        Color color,
        float bodyHeight,
        bool withArm,
        out Transform shoulder,
        out Transform hand)
    {
        shoulder = null;
        hand = null;

        var root = new GameObject(name);
        root.transform.position = position;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (withArm)
            rb.bodyType = RigidbodyType2D.Kinematic;

        var body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, bodyHeight * 0.15f, 0f);
        body.transform.localScale = new Vector3(0.9f, bodyHeight, 1f);
        var bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sprite = CreateUnitSprite();
        bodySr.color = color;
        bodySr.sortingOrder = 10;

        var head = new GameObject("Head");
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, bodyHeight * 0.55f + 0.35f, 0f);
        head.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        var headSr = head.AddComponent<SpriteRenderer>();
        headSr.sprite = CreateUnitSprite();
        headSr.color = Color.Lerp(color, Color.white, 0.25f);
        headSr.sortingOrder = 11;

        if (!withArm)
        {
            var playerCol = root.AddComponent<CircleCollider2D>();
            playerCol.radius = 0.45f;
            return root.transform;
        }

        shoulder = new GameObject("Shoulder").transform;
        shoulder.SetParent(root.transform, false);
        shoulder.localPosition = new Vector3(0.15f, bodyHeight * 0.25f, 0f);

        var arm = new GameObject("Arm");
        arm.transform.SetParent(shoulder, false);
        arm.transform.localPosition = new Vector3(0.55f, 0f, 0f);
        arm.transform.localScale = new Vector3(1.1f, 0.28f, 1f);
        var armSr = arm.AddComponent<SpriteRenderer>();
        armSr.sprite = CreateUnitSprite();
        armSr.color = Color.Lerp(color, Color.black, 0.2f);
        armSr.sortingOrder = 12;

        hand = new GameObject("Hand").transform;
        hand.SetParent(shoulder, false);
        hand.localPosition = new Vector3(1.15f, 0f, 0f);
        hand.localScale = new Vector3(0.45f, 0.45f, 1f);
        var handSr = hand.gameObject.AddComponent<SpriteRenderer>();
        handSr.sprite = CreateUnitSprite();
        handSr.color = new Color(1f, 0.85f, 0.35f, 1f);
        handSr.sortingOrder = 13;

        return root.transform;
    }

    private static Sprite CreateUnitSprite()
    {
        const int size = 8;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "MotionBossPrototypeUnit"
        };
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void OnGUI()
    {
        if (avatar == null) return;
        hudStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };
        // 최소 보조 HUD. 공격 예고 UI가 아니다.
        GUI.Label(new Rect(16f, 12f, 640f, 28f),
            $"Motion Boss Prototype | HP {avatar.Health:0}/{avatar.MaxHealth:0} | WASD 이동 | E 패링",
            hudStyle);
        GUI.Label(new Rect(16f, 36f, 720f, 28f),
            "성공 기준: 바닥 경고 없이, 보스가 다가오며 노란 손이 휘두르는 모션만 보고 피하거나 패링",
            hudStyle);
    }
}
