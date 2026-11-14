using UnityEngine;

/// <summary>기본 Bullet의 그림만 교체합니다. 루트의 이동·크기·피해 판정은 변경하지 않습니다.</summary>
[DisallowMultipleComponent]
public sealed class BasicBulletVisual : MonoBehaviour
{
    private LightSkillVisuals.BakedClip clip;
    private SpriteRenderer flightRenderer;
    private MeshRenderer legacyRenderer;
    private bool legacyWasEnabled;
    private bool cachedLegacy;
    private bool running;
    private float age;

    public void Play(Vector2 direction)
    {
        clip = LightSkillVisuals.Baked("BasicLightBolt");
        if (clip == null || clip.Frames.Length == 0) { Stop(); return; }
        if (!cachedLegacy)
        {
            legacyRenderer = GetComponent<MeshRenderer>();
            legacyWasEnabled = legacyRenderer != null && legacyRenderer.enabled;
            cachedLegacy = true;
        }
        if (flightRenderer == null)
        {
            var child = new GameObject("Basic Light Bolt");
            child.transform.SetParent(transform, false);
            flightRenderer = child.AddComponent<SpriteRenderer>();
            flightRenderer.sharedMaterial = LightSkillVisuals.BlazeMaterial();
            flightRenderer.sortingLayerID = SortingLayer.NameToID("Entity");
            flightRenderer.sortingOrder = 30;
        }
        Vector3 parentScale = transform.lossyScale;
        flightRenderer.transform.localScale = new Vector3(InverseScale(parentScale.x), InverseScale(parentScale.y), 1f);
        Vector2 aim = direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.right;
        flightRenderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
        flightRenderer.sprite = clip.Frames[0];
        flightRenderer.color = Color.white;
        flightRenderer.enabled = true;
        if (legacyRenderer != null) legacyRenderer.enabled = false;
        age = 0f;
        running = true;
    }

    private static float InverseScale(float scale) => Mathf.Abs(scale) > .0001f ? 1f / scale : 1f;

    public void ShowHit()
    {
        if (running) BasicBulletHitFx.Spawn(transform.position);
    }

    public void Stop()
    {
        running = false;
        age = 0f;
        if (flightRenderer != null) flightRenderer.enabled = false;
        if (legacyRenderer != null) legacyRenderer.enabled = legacyWasEnabled;
    }

    private void Update() => Tick(Time.deltaTime);

    private void Tick(float deltaTime)
    {
        if (!running || deltaTime <= 0f) return;
        age = Mathf.Repeat(age + deltaTime, clip.Duration);
        // 반복 경계의 부동소수점 오차로 이전 프레임에 한 번 더 머무르지 않게 합니다.
        int frame = Mathf.Min(clip.Frames.Length - 1, Mathf.FloorToInt(age / clip.FrameDuration + .0001f));
        flightRenderer.sprite = clip.Frames[frame];
    }

    private void OnDisable() => Stop();
}
