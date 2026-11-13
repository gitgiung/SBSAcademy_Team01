using UnityEngine;

/// <summary>기본 광탄의 명중 그림입니다. 콜라이더나 피해 처리는 없습니다.</summary>
[DisallowMultipleComponent]
public sealed class BasicBulletHitFx : MonoBehaviour, IPoolable
{
    private static GameObject prototype;
    private SpriteRenderer spriteRenderer;
    private LightSkillVisuals.BakedClip clip;
    private float age;
    private bool running;

    public static void Spawn(Vector3 position)
    {
        var frames = LightSkillVisuals.Baked("BasicLightHit");
        if (frames == null || frames.Frames.Length == 0) return;
        if (Managers.Initialized && Managers.Pool != null)
        {
            if (prototype == null)
            {
                prototype = new GameObject("Basic Light Hit Prototype");
                prototype.SetActive(false);
                prototype.AddComponent<SpriteRenderer>();
                prototype.AddComponent<PooledObject>();
                prototype.AddComponent<BasicBulletHitFx>();
            }
            Managers.Pool.Pop(prototype, position, Quaternion.identity,
                initialize: go => go.GetComponent<BasicBulletHitFx>().Play(frames));
        }
        else
        {
            var root = new GameObject("Basic Light Hit", typeof(SpriteRenderer));
            root.transform.position = position;
            root.AddComponent<BasicBulletHitFx>().Play(frames);
        }
    }

    private void Play(LightSkillVisuals.BakedClip frames)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        clip = frames;
        age = 0f;
        running = true;
        spriteRenderer.sharedMaterial = LightSkillVisuals.BlazeMaterial();
        spriteRenderer.sortingLayerID = SortingLayer.NameToID("Entity");
        spriteRenderer.sortingOrder = 31;
        spriteRenderer.color = Color.white;
        spriteRenderer.sprite = clip.Frames[0];
        spriteRenderer.enabled = true;
    }

    private void Update() => Tick(Time.deltaTime);

    private void Tick(float deltaTime)
    {
        if (!running || deltaTime <= 0f) return;
        age += deltaTime;
        if (age >= clip.Duration)
        {
            running = false;
            var pooled = GetComponent<PooledObject>();
            if (pooled == null || !pooled.ReturnToPool()) Destroy(gameObject);
            return;
        }
        spriteRenderer.sprite = clip.Frames[Mathf.Min(clip.Frames.Length - 1, Mathf.FloorToInt(age / clip.FrameDuration))];
    }

    public void OnPoolRent() => OnPoolReturn();

    public void OnPoolReturn()
    {
        running = false;
        age = 0f;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    private void OnDisable() => OnPoolReturn();
}
