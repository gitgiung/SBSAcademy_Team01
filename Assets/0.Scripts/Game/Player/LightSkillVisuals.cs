using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>픽셀 이펙트 시트를 자르고 All In 1 Sprite Shader로 속성 색을 입힙니다.</summary>
public static class LightSkillVisuals
{
    public const float PixelsPerUnit = 32f;
    private static readonly Dictionary<string, Sprite[]> Clips = new Dictionary<string, Sprite[]>();
    private static readonly Dictionary<string, float> FrameDurations = new Dictionary<string, float>();
    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    private static Material template;

    public static string ClipName(LightSkillShape shape)
    {
        switch (shape)
        {
            case LightSkillShape.Circle: return "lightfireblast";
            case LightSkillShape.Line: return "lightthrust";
            case LightSkillShape.Cone:
            case LightSkillShape.Arc: return "lightslash";
            case LightSkillShape.Ring: return "lightflamering";
            default: return "lightfirebolt";
        }
    }

    public static Sprite[] Frames(LightSkillShape shape, out float frameDuration)
    {
        string name = ClipName(shape);
        if (!Clips.TryGetValue(name, out Sprite[] frames))
        {
            frames = Slice(name);
            Clips[name] = frames;
        }
        if (!FrameDurations.TryGetValue(name, out frameDuration)) frameDuration = 0.08f;
        return frames;
    }

    /// <summary>pixelfx로 구워 둔 작열 구체 비행 그림(plasma-shot)입니다.</summary>
    public const string BlazeOrbPrefix = "PlasmaOrb_";
    /// <summary>pixelfx로 구워 둔 작열 구체 불줄기 한 가닥(blaze-fork)입니다. 오른쪽을 향하고 피벗이 뿌리입니다.</summary>
    public const string BlazeForkPrefix = "BlazeFork_";
    /// <summary>불줄기 그림에서 뿌리부터 끝까지의 픽셀 길이입니다. 굽기 옵션 reach와 같아야 합니다.</summary>
    public const float BlazeForkReachPixels = 88f;
    /// <summary>pixelfx로 구워 둔 절멸의 불 균열(sundering-fire)입니다. 오른쪽을 향하고 피벗이 뿌리입니다.</summary>
    public const string SunderFirePrefix = "SunderFire_";
    /// <summary>균열 그림에서 뿌리부터 끝까지의 픽셀 길이입니다. 굽기 옵션 reach와 같아야 합니다.</summary>
    public const float SunderFireReachPixels = 256f;
    /// <summary>pixelfx로 구워 둔 화염 회오리(flame-whirl)입니다. 피벗이 바닥 고리의 가운데입니다.</summary>
    public const string FlameWhirlPrefix = "FlameWhirl_";
    /// <summary>회오리 그림에서 가운데부터 바깥 불 벽까지의 가로 픽셀 길이입니다. 굽기 옵션 reach와 같아야 합니다.</summary>
    public const float FlameWhirlReachPixels = 80f;
    /// <summary>pixelfx로 구워 둔 태양 탄막의 태양 문양(sun-crest)입니다. 피벗이 가운데입니다.</summary>
    public const string SunCrestPrefix = "SunCrest_";
    /// <summary>태양 문양에서 반복 재생하는 구간의 시작 프레임입니다. 그 앞은 그려지는 구간입니다. 굽기 소스의 IN과 같아야 합니다.</summary>
    public const int SunCrestLoopStart = 7;
    /// <summary>반복 구간의 프레임 수입니다. 그 뒤는 흩어지는 구간입니다. 굽기 소스의 HOLD와 같아야 합니다.</summary>
    public const int SunCrestLoopLength = 12;
    /// <summary>미사일이 나가는 긴 가시의 개수입니다. 0번이 위쪽이고 시계 방향으로 돕니다. 굽기 소스의 긴 가시 수와 같아야 합니다.</summary>
    public const int SunCrestSpikes = 6;
    /// <summary>가운데에서 미사일이 나오는 긴 가시 끝까지의 픽셀 거리입니다. 굽기 옵션 radius + long보다 조금 안쪽입니다.</summary>
    public const float SunCrestTipPixels = 64f;
    /// <summary>pixelfx로 구워 둔 가시 끝의 발사 섬광(sun-flare)입니다. 오른쪽을 향하고 피벗이 가시 끝입니다.</summary>
    public const string SunFlarePrefix = "SunFlare_";
    /// <summary>pixelfx로 구워 둔 흰 불꽃 미사일(sun-missile)입니다. 오른쪽을 향하고 피벗이 탄두 가운데입니다.</summary>
    public const string SunMissilePrefix = "SunMissile_";
    public const float BlazeFrameDuration = 1f / 30f;

    private static Sprite[] blazeSprites;
    private static readonly Dictionary<string, Sprite[]> BlazeClips = new Dictionary<string, Sprite[]>();
    private static Material blazeMaterial;

    /// <summary>구워 둔 프레임을 이름순으로 돌려줍니다. 없으면 빈 배열이라 호출 쪽이 예전 그림으로 대신합니다.</summary>
    public static Sprite[] BlazeFrames(string prefix)
    {
        if (BlazeClips.TryGetValue(prefix, out Sprite[] cached) && cached != null && cached.Length > 0
            && cached[0] != null) return cached;
        if (blazeSprites == null || blazeSprites.Length == 0 || blazeSprites[0] == null)
            blazeSprites = Resources.LoadAll<Sprite>("LightSkills/Blaze");
        var list = new List<Sprite>();
        foreach (Sprite sprite in blazeSprites)
            if (sprite != null && sprite.name.StartsWith(prefix)) list.Add(sprite);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        Sprite[] frames = list.ToArray();
        BlazeClips[prefix] = frames;
        return frames;
    }

    /// <summary>스킬마다 pixelfx로 구워 둔 시트 한 장입니다. 그림은 Resources/LightSkills/Fx, 자르는 정보는 Fx/Frames에 있습니다.</summary>
    public sealed class BakedClip
    {
        public Sprite[] Frames;
        public float FrameDuration;
        /// <summary>범위 그림에서 원점부터 판정 끝까지의 픽셀 길이입니다. 탄환 그림은 0입니다.</summary>
        public float ReachPixels;
        /// <summary>탄환은 반복 재생하고, 범위 그림은 한 번 재생한 뒤 사라집니다.</summary>
        public bool Loop;
        public float Duration => Frames.Length * FrameDuration;
        public float ReachUnits => ReachPixels / PixelsPerUnit;
    }

    // tools/PixelFxBake/export-skills.mjs가 쓰는 JSON과 필드 이름이 같아야 합니다.
    [System.Serializable]
    private sealed class BakedSheet
    {
        public float fps;
        public bool loop;
        public float reach;
        public int frameWidth;
        public int frameHeight;
        public int columns;
        public int count;
        public float pivotX;
        public float pivotY;
    }

    private const string BakedFolder = "LightSkills/Fx/";
    /// <summary>화염구가 맞아 터질 때의 폭발 그림입니다. 가운데부터 폭발 반경까지가 reach입니다.</summary>
    public const string FireballBurstClip = "FireballBurst";
    private static readonly Dictionary<string, BakedClip> BakedClips = new Dictionary<string, BakedClip>();

    /// <summary>
    /// 스킬 ID에 맞춰 구워 둔 그림 이름입니다. 없으면 null이고 호출 쪽이 예전 시트로 대신합니다.
    /// 작열 구체·태양 탄막·절멸의 불·화염 고리는 전용 그림(Blaze)을 따로 쓰므로 여기에 없습니다.
    /// </summary>
    public static string BakedClipName(int skillId)
    {
        switch (skillId)
        {
            case 10001: return "FireBullet";
            case 10002: return "FireDart";
            case 10003: return "FlameSmash";
            case 10004: return "FireOrb";
            case 10005: return "FlameSpear";
            case 10006: return "FlareShot";
            case 10007: return "SunBlade";
            case 10008: return "HeatGust";
            case 10009: return "FlameArrow";
            case 10010: return "EmberPellet";
            case 10011: return "FlameFan";
            case 10012: return "LavaShards";
            case 10013: return "FireTracer";
            case 10018: return "IceShard";
            case 10019: return "WindArrow";
            case 10020: return "RockShot";
            case 10021: return "VoltArrow";
            case 20001: return "FlameHammer";
            case 20003: return "GreatExplosion";
            case 20004: return "FireStorm";
            case 20005: return "MeteorSword";
            case 20006: return "FlameSlash";
            case 20007: return "LavaEruption";
            case 20008: return "HeatWave";
            case 20009: return "FlamePillar";
            case 20010: return "SunSmash";
            case 20011: return "CrimsonLotus";
            case 20013: return "Hellfire";
            case 20014: return "Annihilation";
            case 20015: return "SunCollapse";
            default: return null;
        }
    }

    /// <summary>구워 둔 시트를 처음 쓸 때 한 번만 읽어 프레임으로 자릅니다. 그림이나 정보가 없으면 null입니다.</summary>
    public static BakedClip Baked(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (BakedClips.TryGetValue(name, out BakedClip cached)
            && (cached == null || (cached.Frames.Length > 0 && cached.Frames[0] != null))) return cached;
        BakedClip clip = LoadBaked(name);
        BakedClips[name] = clip;
        return clip;
    }

    private static BakedClip LoadBaked(string name)
    {
        Texture2D texture = Resources.Load<Texture2D>(BakedFolder + name);
        TextAsset json = Resources.Load<TextAsset>(BakedFolder + "Frames/" + name);
        if (texture == null || json == null) return null;
        BakedSheet sheet = JsonUtility.FromJson<BakedSheet>(json.text);
        if (sheet == null || sheet.count <= 0 || sheet.columns <= 0 || sheet.frameWidth <= 0 || sheet.frameHeight <= 0)
            return null;
        int rows = (sheet.count + sheet.columns - 1) / sheet.columns;
        // 가져오기 설정으로 그림이 줄어들었으면 자르는 자리가 어긋나므로 쓰지 않습니다.
        if (sheet.columns * sheet.frameWidth > texture.width || rows * sheet.frameHeight > texture.height)
        {
            Debug.LogWarning("[LightSkillVisuals] 구운 시트 크기가 정보와 다릅니다: " + name);
            return null;
        }

        var pivot = new Vector2(sheet.pivotX, sheet.pivotY);
        var sprites = new Sprite[sheet.count];
        for (int i = 0; i < sheet.count; i++)
        {
            int x = (i % sheet.columns) * sheet.frameWidth;
            // 시트는 위에서부터 채우고 Unity는 아래에서부터 셉니다.
            int y = texture.height - (i / sheet.columns + 1) * sheet.frameHeight;
            sprites[i] = Sprite.Create(texture, new Rect(x, y, sheet.frameWidth, sheet.frameHeight), pivot,
                PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprites[i].name = name + " " + i;
        }
        return new BakedClip
        {
            Frames = sprites,
            FrameDuration = sheet.fps > 0f ? 1f / sheet.fps : BlazeFrameDuration,
            ReachPixels = sheet.reach,
            Loop = sheet.loop
        };
    }

    /// <summary>구운 그림의 색을 그대로 보여 주는 재질입니다. 속성 색조 이동을 적용하지 않습니다.</summary>
    public static Material BlazeMaterial()
    {
        if (blazeMaterial != null) return blazeMaterial;
        blazeMaterial = new Material(Shader.Find("Sprites/Default")) { name = "LightSkill Blaze" };
        return blazeMaterial;
    }

    public static Material MaterialFor(string element, LightSkillShape shape)
    {
        bool fireArt = shape == LightSkillShape.Projectile || shape == LightSkillShape.Circle || shape == LightSkillShape.Ring;
        string key = (element ?? "ELEM_FIRE") + (fireArt ? ":fire" : ":tint");
        if (Materials.TryGetValue(key, out Material cached) && cached != null) return cached;

        Material source = Template();
        Material material = source != null ? new Material(source) : new Material(Shader.Find("Sprites/Default"));
        material.name = "LightSkill " + key;
        Color glow = Glow(element);
        if (material.HasProperty("_GlowColor")) material.SetColor("_GlowColor", glow);
        if (material.HasProperty("_Color")) material.SetColor("_Color", fireArt ? Color.white : glow);
        if (material.HasProperty("_HsvShift")) material.SetFloat("_HsvShift", fireArt ? Hue(element) : 0f);
        if (material.HasProperty("_HsvSaturation")) material.SetFloat("_HsvSaturation", 1f);
        if (material.HasProperty("_HsvBright")) material.SetFloat("_HsvBright", 1.15f);
        if (material.HasProperty("_Glow")) material.SetFloat("_Glow", 8f);
        material.EnableKeyword("GLOW_ON");
        material.EnableKeyword("HSV_ON");
        Materials[key] = material;
        return material;
    }

    private static Material Template()
    {
        if (template != null) return template;
        template = Resources.Load<Material>("LightSkills/LightSkillGlow");
        if (template == null)
        {
            Shader shader = Shader.Find("AllIn1SpriteShader/AllIn1Urp2dRenderer");
            if (shader == null) shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");
            if (shader != null) template = new Material(shader);
        }
        return template;
    }

    private static Sprite[] Slice(string resourceName)
    {
        Texture2D texture = Resources.Load<Texture2D>("LightSkills/" + resourceName);
        TextAsset json = Resources.Load<TextAsset>("LightSkills/Frames/" + resourceName);
        if (texture == null || json == null) return System.Array.Empty<Sprite>();

        MatchCollection frames = Regex.Matches(json.text, "\"frame\"\\s*:\\s*\\{\\s*\"x\"\\s*:\\s*(\\d+)\\s*,\\s*\"y\"\\s*:\\s*(\\d+)\\s*,\\s*\"w\"\\s*:\\s*(\\d+)\\s*,\\s*\"h\"\\s*:\\s*(\\d+)");
        Match pivot = Regex.Match(json.text, "\"pivot\"\\s*:\\s*\\{\\s*\"x\"\\s*:\\s*([0-9.]+)\\s*,\\s*\"y\"\\s*:\\s*([0-9.]+)");
        Match duration = Regex.Match(json.text, "\"duration\"\\s*:\\s*(\\d+)");
        Vector2 pivotPoint = pivot.Success
            ? new Vector2(float.Parse(pivot.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(pivot.Groups[2].Value, CultureInfo.InvariantCulture))
            : new Vector2(0.5f, 0.5f);
        if (duration.Success && int.TryParse(duration.Groups[1].Value, out int milliseconds) && milliseconds > 0)
            FrameDurations[resourceName] = milliseconds / 1000f;

        var sprites = new Sprite[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            int x = int.Parse(frames[i].Groups[1].Value);
            int y = int.Parse(frames[i].Groups[2].Value);
            int w = int.Parse(frames[i].Groups[3].Value);
            int h = int.Parse(frames[i].Groups[4].Value);
            int unityY = texture.height - y - h;
            sprites[i] = Sprite.Create(texture, new Rect(x, unityY, w, h), pivotPoint, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprites[i].name = resourceName + " " + i;
        }
        return sprites;
    }

    private static float Hue(string element)
    {
        switch (element)
        {
            case "ELEM_ICE": return 190f;
            case "ELEM_WIND": return 110f;
            case "ELEM_EARTH": return 28f;
            case "ELEM_LIGHTNING": return 48f;
            case "ELEM_WATER": return 210f;
            case "ELEM_ENERGY": return 270f;
            default: return 0f;
        }
    }

    private static Color Glow(string element)
    {
        switch (element)
        {
            case "ELEM_ICE": return new Color(0.55f, 0.86f, 1f);
            case "ELEM_WIND": return new Color(0.72f, 0.95f, 0.7f);
            case "ELEM_EARTH": return new Color(0.72f, 0.55f, 0.32f);
            case "ELEM_LIGHTNING": return new Color(1f, 0.92f, 0.35f);
            case "ELEM_WATER": return new Color(0.28f, 0.6f, 1f);
            case "ELEM_ENERGY": return new Color(0.72f, 0.4f, 1f);
            default: return new Color(1f, 0.45f, 0.12f);
        }
    }
}
