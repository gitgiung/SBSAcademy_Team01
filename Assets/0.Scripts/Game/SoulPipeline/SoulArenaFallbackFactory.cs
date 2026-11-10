using UnityEngine;

/// <summary>
/// Prefab/Humanoid 조립 전에도 SoulArena3D가 Play 되도록 임시 3D 몸 생성.
/// Tools > Soul Pipeline 2번 이후에는 Prefab이 우선한다.
/// </summary>
public static class SoulArenaFallbackFactory
{
    public static GameObject CreatePlayer(Vector3 position)
    {
        GameObject root = CreateBody("SoulPlayerFallback", position, new Color(0.35f, 0.75f, 1f), 1.8f);
        root.AddComponent<SoulPipelineAvatar>();
        Transform hand = CreateArm(root.transform, new Color(0.9f, 0.95f, 1f), addBossArmDriver: false);
        var arm = root.AddComponent<SoulFallbackPlayerArm>();
        arm.Bind(hand.parent); // shoulder
        return root;
    }

    public static GameObject CreateBoss(Vector3 position)
    {
        GameObject root = CreateBody("SoulBossFallback", position, new Color(0.85f, 0.25f, 0.2f), 2.1f);
        Transform hand = CreateArm(root.transform, new Color(1f, 0.85f, 0.35f), addBossArmDriver: true);
        var hitbox = hand.gameObject.AddComponent<SoulWeaponHitbox3D>();
        hitbox.Configure(18f, root);
        hand.gameObject.AddComponent<SoulWeaponArmedPulse>();
        root.AddComponent<SoulArenaDummyHealth>();
        root.AddComponent<SoulBossApproachSlash>().Bind(null, hitbox);
        return root;
    }

    private static GameObject CreateBody(string name, Vector3 position, Color color, float height)
    {
        var root = new GameObject(name);
        root.transform.position = position;

        var cc = root.AddComponent<CharacterController>();
        cc.height = height;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, height * 0.5f, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
        body.transform.localScale = new Vector3(0.7f, height * 0.5f, 0.7f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        ApplyColor(body, color);

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, height + 0.15f, 0f);
        head.transform.localScale = Vector3.one * 0.45f;
        Object.DestroyImmediate(head.GetComponent<Collider>());
        ApplyColor(head, Color.Lerp(color, Color.white, 0.25f));
        return root;
    }

    private static Transform CreateArm(Transform root, Color handColor, bool addBossArmDriver)
    {
        var shoulder = new GameObject("Shoulder").transform;
        shoulder.SetParent(root, false);
        shoulder.localPosition = new Vector3(0.2f, 1.35f, 0.1f);

        var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.SetParent(shoulder, false);
        arm.transform.localPosition = new Vector3(0.55f, 0f, 0f);
        arm.transform.localScale = new Vector3(1.1f, 0.18f, 0.18f);
        Object.DestroyImmediate(arm.GetComponent<Collider>());
        ApplyColor(arm, new Color(0.45f, 0.2f, 0.18f));

        var hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hand.name = "Hand";
        hand.transform.SetParent(shoulder, false);
        hand.transform.localPosition = new Vector3(1.15f, 0f, 0.35f);
        hand.transform.localScale = Vector3.one * 0.32f;
        Object.DestroyImmediate(hand.GetComponent<Collider>());
        ApplyColor(hand, handColor);

        if (addBossArmDriver)
        {
            var swing = root.gameObject.AddComponent<SoulFallbackArmDriver>();
            swing.Bind(shoulder);
        }

        return hand.transform;
    }

    private static void ApplyColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = color };
        renderer.sharedMaterial = mat;
    }
}

/// <summary>폴백 보스 팔 각도. Prefab 본 애니가 없을 때만 사용.</summary>
public sealed class SoulFallbackArmDriver : MonoBehaviour
{
    private Transform shoulder;
    private SoulBossApproachSlash boss;
    private float angle = 70f;

    public void Bind(Transform shoulderBone) => shoulder = shoulderBone;

    private void Awake() => boss = GetComponent<SoulBossApproachSlash>();

    private void LateUpdate()
    {
        if (shoulder == null) return;
        if (boss != null && boss.IsAttacking)
        {
            float t = boss.AttackNormalizedTime;
            // 윈드업(~0.35): 팔 올림, 이후 내려찍기
            angle = t < 0.35f
                ? Mathf.Lerp(-20f, -70f, t / 0.35f)
                : Mathf.Lerp(-70f, 80f, (t - 0.35f) / 0.65f);
        }
        else
            angle = Mathf.Lerp(angle, -25f, Time.deltaTime * 5f);

        // 전방 내려찍기 + 살짝 가로 스윙(Y)으로 측면 커밋과 맞춤.
        float yaw = 0f;
        if (boss != null && boss.IsAttacking)
        {
            float t = boss.AttackNormalizedTime;
            yaw = t < 0.35f ? Mathf.Lerp(15f, -10f, t / 0.35f) : Mathf.Lerp(-10f, 25f, (t - 0.35f) / 0.65f);
        }
        shoulder.localRotation = Quaternion.Euler(angle, yaw, 0f);
    }
}
