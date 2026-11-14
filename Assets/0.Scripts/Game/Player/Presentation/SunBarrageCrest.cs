using UnityEngine;

/// <summary>
/// 태양 탄막의 태양 문양입니다. 플레이어의 자식으로 따라다니며 그려짐 → 유지(반복) → 흩어짐 순으로 재생하고,
/// 미사일이 나갈 긴 가시 끝의 위치와 방향을 알려 줍니다. 그림만 담당하며 피해는 주지 않습니다.
/// 프레임은 pixelfx fire/sun-crest·fire/sun-flare를 구운 것입니다(MIT).
/// </summary>
internal sealed class SunBarrageCrest : MonoBehaviour
{
    /// <summary>마지막 미사일이 떠난 뒤 문양이 흩어지기 전까지 남는 시간입니다. 발사 섬광이 보일 만큼의 연출 값입니다.</summary>
    public const float LingerSeconds = 0.15f;
    private const int FlareSlots = 3;
    // 다른 약공격 그림(30)과 같은 정렬 레이어에서 미사일 바로 아래, 발사 섬광은 바로 위에 둡니다.
    private const int BodySortingOrder = 29;
    private const int FlareSortingOrder = 31;

    private SpriteRenderer body;
    private readonly SpriteRenderer[] flares = new SpriteRenderer[FlareSlots];
    private readonly int[] flareFrame = new int[FlareSlots];
    private readonly float[] flareTimer = new float[FlareSlots];
    private Sprite[] frames = System.Array.Empty<Sprite>();
    private Sprite[] flareFrames = System.Array.Empty<Sprite>();
    private int frame;
    private float frameTimer;
    private float holdLeft;
    private bool playing;
    private int nextFlare;

    private static int LoopEnd => LightSkillVisuals.SunCrestLoopStart + LightSkillVisuals.SunCrestLoopLength;
    private static float TipUnits => LightSkillVisuals.SunCrestTipPixels / LightSkillVisuals.PixelsPerUnit;

    /// <summary>구운 그림이 반복 구간까지 있어야 씁니다. 없으면 호출 쪽이 예전 탄환 연사로 대신합니다.</summary>
    public bool Ready => frames.Length >= LoopEnd && frames[0] != null;

    /// <summary>문양이 다 그려질 때까지의 시간입니다. 첫 미사일은 이만큼 기다렸다가 나갑니다.</summary>
    public float DrawSeconds => LightSkillVisuals.SunCrestLoopStart * LightSkillVisuals.BlazeFrameDuration;

    public static SunBarrageCrest Attach(Transform player)
    {
        var root = new GameObject("Sun Barrage Crest");
        root.transform.SetParent(player, false);
        // 플레이어가 확대되어 있어도 구운 픽셀 크기 그대로 보이게 합니다.
        Vector3 scale = player.lossyScale;
        root.transform.localScale = new Vector3(
            Mathf.Abs(scale.x) > 0.0001f ? 1f / scale.x : 1f,
            Mathf.Abs(scale.y) > 0.0001f ? 1f / scale.y : 1f, 1f);
        SunBarrageCrest crest = root.AddComponent<SunBarrageCrest>();
        crest.Build();
        return crest;
    }

    private void Build()
    {
        frames = LightSkillVisuals.BlazeFrames(LightSkillVisuals.SunCrestPrefix);
        flareFrames = LightSkillVisuals.BlazeFrames(LightSkillVisuals.SunFlarePrefix);
        Material material = LightSkillVisuals.BlazeMaterial();
        body = gameObject.AddComponent<SpriteRenderer>();
        body.sharedMaterial = material;
        body.sortingOrder = BodySortingOrder;
        body.enabled = false;
        for (int i = 0; i < flares.Length; i++)
        {
            var child = new GameObject("Sun Flare");
            child.transform.SetParent(transform, false);
            SpriteRenderer flare = child.AddComponent<SpriteRenderer>();
            flare.sharedMaterial = material;
            flare.sortingLayerID = body.sortingLayerID;
            flare.sortingOrder = FlareSortingOrder;
            flare.enabled = false;
            flares[i] = flare;
        }
    }

    /// <summary>문양을 그리고 seconds 동안 유지한 뒤 흩어지게 합니다. 이미 떠 있으면 유지 시간만 늘립니다.</summary>
    public void Play(float seconds)
    {
        if (!Ready) return;
        bool held = playing && frame < LoopEnd;
        holdLeft = held ? Mathf.Max(holdLeft, seconds) : seconds;
        if (held) return;
        frame = 0;
        frameTimer = 0f;
        playing = true;
        body.sprite = frames[0];
        body.enabled = true;
    }

    /// <summary>재생을 바로 끝내고 숨깁니다.</summary>
    public void Cancel()
    {
        playing = false;
        holdLeft = 0f;
        if (body != null) body.enabled = false;
        for (int i = 0; i < flares.Length; i++)
            if (flares[i] != null) flares[i].enabled = false;
    }

    /// <summary>aim 방향에 가장 가까운 긴 가시의 번호입니다. 첫 미사일을 그 가시에서 쏘기 위해 씁니다.</summary>
    public int NearestSpike(Vector2 aim)
    {
        if (aim.sqrMagnitude < 0.0001f) return 0;
        int best = 0;
        float bestDot = float.NegativeInfinity;
        for (int i = 0; i < LightSkillVisuals.SunCrestSpikes; i++)
        {
            float dot = Vector2.Dot(SpikeDirection(i), aim);
            if (dot <= bestDot) continue;
            bestDot = dot;
            best = i;
        }
        return best;
    }

    /// <summary>긴 가시가 뻗은 월드 방향입니다. 번호가 가시 수를 넘으면 한 바퀴 돌아 다시 셉니다.</summary>
    public Vector2 SpikeDirection(int index)
    {
        Vector2 direction = (Vector2)(SpikeTip(index) - transform.position);
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
    }

    /// <summary>긴 가시 끝의 월드 위치입니다. 미사일이 여기서 나갑니다.</summary>
    public Vector3 SpikeTip(int index) => transform.TransformPoint(LocalSpike(index) * TipUnits);

    /// <summary>가시 끝에서 발사 섬광을 한 번 재생합니다.</summary>
    public void Flare(int index)
    {
        if (flareFrames.Length == 0) return;
        int slot = nextFlare;
        nextFlare = (nextFlare + 1) % flares.Length;
        SpriteRenderer flare = flares[slot];
        if (flare == null) return;
        Vector2 local = LocalSpike(index);
        flare.transform.localPosition = local * TipUnits;
        flare.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
        flareFrame[slot] = 0;
        flareTimer[slot] = 0f;
        flare.sprite = flareFrames[0];
        flare.enabled = true;
    }

    // 0번 가시가 위쪽이고 시계 방향으로 돕니다. 구운 그림과 같은 배치입니다.
    private static Vector2 LocalSpike(int index)
    {
        int count = LightSkillVisuals.SunCrestSpikes;
        float radians = ((index % count + count) % count) * (360f / count) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;
        TickFlares(deltaTime);
        if (!playing) return;
        holdLeft -= deltaTime;
        frameTimer += deltaTime;
        while (playing && frameTimer >= LightSkillVisuals.BlazeFrameDuration)
        {
            frameTimer -= LightSkillVisuals.BlazeFrameDuration;
            Advance();
        }
    }

    private void Advance()
    {
        int next = frame + 1;
        bool inLoop = frame >= LightSkillVisuals.SunCrestLoopStart && frame < LoopEnd;
        // 유지 시간이 남았으면 반복 구간을 돌고, 다 됐으면 어느 프레임에서든 바로 흩어지는 구간으로 넘어갑니다.
        if (inLoop && holdLeft <= 0f) next = LoopEnd;
        else if (inLoop && next >= LoopEnd) next = LightSkillVisuals.SunCrestLoopStart;
        if (next >= frames.Length)
        {
            playing = false;
            body.enabled = false;
            return;
        }
        frame = next;
        body.sprite = frames[frame];
    }

    private void TickFlares(float deltaTime)
    {
        for (int i = 0; i < flares.Length; i++)
        {
            SpriteRenderer flare = flares[i];
            if (flare == null || !flare.enabled) continue;
            flareTimer[i] += deltaTime;
            while (flareTimer[i] >= LightSkillVisuals.BlazeFrameDuration)
            {
                flareTimer[i] -= LightSkillVisuals.BlazeFrameDuration;
                flareFrame[i]++;
            }
            if (flareFrame[i] >= flareFrames.Length) flare.enabled = false;
            else flare.sprite = flareFrames[flareFrame[i]];
        }
    }

    private void OnDisable() => Cancel();
}
