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
