using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 강화 카드 테두리를 타고 빛이 불규칙하게 지나가는 루프입니다.
/// 프레임은 pixelfx ui/card-border-shine을 구운 것입니다(MIT). Image 색으로 등급 색을 곱합니다.
/// </summary>
public sealed class LevelUpCardBorderShine : MonoBehaviour
{
    public const string ResourceFolder = "UI/LevelUp";
    public const string FramePrefix = "CardBorderShine_";
    public const float FrameDuration = 1f / 30f;

    private Image target;
    private Sprite[] frames = System.Array.Empty<Sprite>();
    private int frame;
    private float timer;
    private bool playing;
    private static Sprite[] sharedFrames;

    public bool Ready
    {
        get
        {
            EnsureFrames();
            return frames.Length > 0 && frames[0] != null;
        }
    }

    public void Bind(Image image)
    {
        target = image;
        EnsureFrames();
        if (target != null)
        {
            target.enabled = false;
            if (frames.Length > 0) target.sprite = frames[0];
        }
    }

    public void Play()
    {
        EnsureFrames();
        if (target == null || frames.Length == 0) return;
        playing = true;
        frame = 0;
        timer = 0f;
        target.sprite = frames[0];
        target.enabled = true;
        enabled = true;
    }

    public void Stop()
    {
        playing = false;
        if (target != null) target.enabled = false;
    }

    private void Update()
    {
        if (!playing || target == null || frames.Length == 0 || Time.unscaledDeltaTime <= 0f) return;
        // 레벨업 창은 timeScale=0이므로 비가속 시간을 씁니다.
        timer += Time.unscaledDeltaTime;
        while (timer >= FrameDuration)
        {
            timer -= FrameDuration;
            frame = (frame + 1) % frames.Length;
            target.sprite = frames[frame];
        }
    }

    private void OnDisable() => Stop();

    private void EnsureFrames()
    {
        if (frames.Length > 0 && frames[0] != null) return;
        if (sharedFrames != null && sharedFrames.Length > 0 && sharedFrames[0] != null)
        {
            frames = sharedFrames;
            return;
        }
        Sprite[] loaded = Resources.LoadAll<Sprite>(ResourceFolder);
        var list = new List<Sprite>();
        foreach (Sprite sprite in loaded)
            if (sprite != null && sprite.name.StartsWith(FramePrefix)) list.Add(sprite);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        sharedFrames = list.ToArray();
        frames = sharedFrames;
    }
}
