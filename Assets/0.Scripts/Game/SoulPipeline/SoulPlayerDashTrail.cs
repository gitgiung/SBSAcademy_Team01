using UnityEngine;

/// <summary>대시 중 짧은 잔상. 판정과 무관, 가독성만.</summary>
[DisallowMultipleComponent]
public sealed class SoulPlayerDashTrail : MonoBehaviour
{
    private SoulPipelineAvatar avatar;
    private float spawnTimer;

    private void Awake() => avatar = GetComponent<SoulPipelineAvatar>();

    private void Update()
    {
        if (avatar == null || !avatar.IsDashing) return;
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = 0.04f;
        SpawnGhost();
    }

    private void SpawnGhost()
    {
        var skin = GetComponent<SoulPlayerSpriteSkin>();
        if (skin != null && skin.BodyRenderer != null && skin.BodyRenderer.sprite != null)
        {
            SpawnSpriteGhost(skin.BodyRenderer);
            return;
        }

        var ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ghost.name = "DashGhost";
        Object.DestroyImmediate(ghost.GetComponent<Collider>());
        ghost.transform.position = transform.position + Vector3.up * 0.9f;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);
        var renderer = ghost.GetComponent<MeshRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        renderer.sharedMaterial = new Material(shader) { color = new Color(0.45f, 0.85f, 1f, 0.35f) };
        Object.Destroy(ghost, 0.22f);
    }

    private static void SpawnSpriteGhost(SpriteRenderer source)
    {
        var ghost = new GameObject("DashGhost");
        Transform src = source.transform;
        ghost.transform.SetPositionAndRotation(src.position, src.rotation);
        ghost.transform.localScale = src.lossyScale;
        var renderer = ghost.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.flipX = source.flipX;
        renderer.color = new Color(0.45f, 0.85f, 1f, 0.45f);
        Object.Destroy(ghost, 0.22f);
    }
}
