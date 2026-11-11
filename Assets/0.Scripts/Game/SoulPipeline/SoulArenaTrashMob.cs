using UnityEngine;

/// <summary>평탄 3D용 단순 잡몹. 본 애니 없이 접근 접촉만. 보스 정밀 판정과 구분.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaTrashMob : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float contactDamage = 6f;
    [SerializeField] private float contactCooldown = 0.7f;
    [SerializeField] private float aggroRadius = 14f;
    [SerializeField] private float separationRadius = 1.4f;

    private Transform target;
    private CharacterController character;
    private float cooldown;
    private float flankSign = 1f;
    private SoulBossApproachSlash cachedBoss;
    private float bossCacheTimer;
    private float separationTimer;
    private Vector3 cachedSeparation;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (character == null)
        {
            character = gameObject.AddComponent<CharacterController>();
            character.height = 0.9f;
            character.radius = 0.35f;
            character.center = new Vector3(0f, 0.45f, 0f);
        }

        if (GetComponent<SoulGroundGravity>() == null)
            gameObject.AddComponent<SoulGroundGravity>();
        var hp = GetComponent<SoulArenaDummyHealth>();
        if (hp == null) hp = gameObject.AddComponent<SoulArenaDummyHealth>();
        hp.Configure(40f);
        flankSign = Random.value < 0.5f ? -1f : 1f;
    }

    private void Update()
    {
        var health = GetComponent<SoulArenaDummyHealth>();
        if (health != null && health.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        if (cooldown > 0f) cooldown -= Time.deltaTime;
        if (target == null)
        {
            var avatar = FindAnyObjectByType<SoulPipelineAvatar>();
            if (avatar == null || avatar.IsDead) return;
            target = avatar.transform;
        }
        else
        {
            var avatar = target.GetComponent<SoulPipelineAvatar>();
            if (avatar != null && avatar.IsDead) return;
        }

        Vector3 to = target.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist > aggroRadius || dist < 0.0001f) return;
        Vector3 dir = to / dist;

        // 보스 윈드업/슬래시 중엔 잡몹이 덜 몰려 본 패턴이 보이게.
        float speedMul = 1f;
        bossCacheTimer -= Time.deltaTime;
        if (cachedBoss == null || bossCacheTimer <= 0f)
        {
            cachedBoss = FindAnyObjectByType<SoulBossApproachSlash>();
            bossCacheTimer = 0.75f;
        }
        if (cachedBoss != null && cachedBoss.enabled && cachedBoss.IsAttacking)
            speedMul = 0.35f;

        // 측면 오프셋 + 분리로 한 점에 겹치지 않게.
        Vector3 side = Vector3.Cross(Vector3.up, dir) * flankSign;
        separationTimer -= Time.deltaTime;
        if (separationTimer <= 0f)
        {
            cachedSeparation = Separation();
            separationTimer = 0.2f;
        }
        Vector3 desired = (dir * 0.75f + side * 0.35f + cachedSeparation).normalized;
        float speed = (dist > 4f ? moveSpeed : moveSpeed * 0.75f) * speedMul;
        character.Move(desired * (speed * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);

        if (cooldown <= 0f && dist <= 1.1f)
        {
            var receiver = target.GetComponent<IContactDamageReceiver>();
            receiver?.TakeContactDamage(contactDamage, gameObject);
            cooldown = contactCooldown;
        }
    }

    private Vector3 Separation()
    {
        Vector3 push = Vector3.zero;
        var others = FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Exclude);
        for (int i = 0; i < others.Length; i++)
        {
            SoulArenaTrashMob o = others[i];
            if (o == null || o == this) continue;
            Vector3 d = transform.position - o.transform.position;
            d.y = 0f;
            float dist = d.magnitude;
            if (dist < 0.01f || dist > separationRadius) continue;
            push += d / dist * (1f - dist / separationRadius);
        }
        return push;
    }

    public static GameObject Spawn(Vector3 position, Color color)
    {
        var root = new GameObject("SoulTrashMob");
        root.transform.position = position;
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        body.transform.localScale = Vector3.one * 0.8f;
        Object.DestroyImmediate(body.GetComponent<Collider>());
        var renderer = body.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        renderer.sharedMaterial = new Material(shader) { color = color };
        var mob = root.AddComponent<SoulArenaTrashMob>();
        if (SoulTrashMobSpriteSkin.UseMainZombieSprite)
            SoulTrashMobSpriteSkin.Apply(mob);
        return root;
    }
}
