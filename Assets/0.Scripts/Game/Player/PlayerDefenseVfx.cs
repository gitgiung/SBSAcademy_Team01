using System.Collections;
using UnityEngine;

/// <summary>Plays the player's parry and guard sprite effects.</summary>
internal sealed class PlayerDefenseVfx : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] parryFrames;
    private Sprite[] guardFrames;
    private SpriteRenderer playerRenderer;
    private Color originalPlayerColor;
    private int playerSortingOrder;
    private Coroutine playback;
    private bool guardActive;
    private bool playingParry;
    private const float FrameDuration = 0.06f;
    private const float ParryScale = 4f;
    private const float GuardScale = 1.875f;

    public void Initialize(Transform player, SpriteRenderer playerRenderer)
    {
        // VFX 자식과 프레임 준비
        var child = new GameObject("Defense VFX");
        child.transform.SetParent(player, false);
        child.transform.localScale = Vector3.one * ParryScale;
        spriteRenderer = child.AddComponent<SpriteRenderer>();
        this.playerRenderer = playerRenderer;
        if (playerRenderer != null) originalPlayerColor = playerRenderer.color;
        spriteRenderer.sortingLayerID = playerRenderer != null ? playerRenderer.sortingLayerID : 0;
        playerSortingOrder = playerRenderer != null ? playerRenderer.sortingOrder : 0;
        spriteRenderer.sortingOrder = playerSortingOrder + 100;
        spriteRenderer.enabled = false;
        parryFrames = LoadFrames("ParryFlash_");
        guardFrames = LoadFrames("GuardBarrier_");
    }

    public void PlayParry()
    {
        // 패링 섬광 1회 재생
        if (guardActive || parryFrames.Length == 0) return;
        StopPlayback();
        playingParry = true;
        spriteRenderer.transform.localScale = Vector3.one * ParryScale;
        spriteRenderer.sortingOrder = playerSortingOrder + 100;
        playback = StartCoroutine(PlayOnce(parryFrames));
    }

    public void SetGuard(bool active)
    {
        // 가드 이펙트 및 플레이어 색 전환
        if (!active)
        {
            if (!guardActive) return;
            guardActive = false;
            if (!playingParry) StopPlayback();
            if (spriteRenderer != null && !playingParry) spriteRenderer.enabled = false;
            RestorePlayerColor();
            return;
        }

        guardActive = active;
        StopPlayback();
        playingParry = false;
        if (guardFrames.Length == 0)
        {
            SetPlayerGuardColor(true);
            return;
        }
        spriteRenderer.transform.localScale = Vector3.one * GuardScale;
        spriteRenderer.sortingOrder = playerSortingOrder - 1;
        SetPlayerGuardColor(true);
        playback = StartCoroutine(PlayLoop(guardFrames));
    }

    public void ResetVfx()
    {
        // 재생과 색상 초기화
        guardActive = false;
        playingParry = false;
        StopPlayback();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            spriteRenderer.sortingOrder = playerSortingOrder + 100;
        }
        RestorePlayerColor();
    }

    private IEnumerator PlayOnce(Sprite[] frames)
    {
        // 프레임 순서대로 한 번 재생
        spriteRenderer.enabled = true;
        foreach (var frame in frames)
        {
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(FrameDuration);
        }
        spriteRenderer.enabled = false;
        playingParry = false;
        playback = null;
    }

    private IEnumerator PlayLoop(Sprite[] frames)
    {
        // 가드 중 반복 재생
        spriteRenderer.enabled = true;
        while (guardActive)
        {
            foreach (var frame in frames)
            {
                if (!guardActive) break;
                spriteRenderer.sprite = frame;
                yield return new WaitForSeconds(FrameDuration);
            }
        }
        spriteRenderer.enabled = false;
        playback = null;
    }

    private void StopPlayback()
    {
        if (playback == null) return;
        StopCoroutine(playback);
        playback = null;
    }

    private void SetPlayerGuardColor(bool active)
    {
        // 가드 중 어둡게 표시
        if (playerRenderer == null) return;
        playerRenderer.color = active
            ? new Color(originalPlayerColor.r * 0.72f, originalPlayerColor.g * 0.72f,
                originalPlayerColor.b * 0.72f, originalPlayerColor.a * 0.9f)
            : originalPlayerColor;
    }

    private void RestorePlayerColor() => SetPlayerGuardColor(false);

    private static Sprite[] LoadFrames(string prefix)
    {
        // 이름순 프레임 로드
        var loaded = Resources.LoadAll<Sprite>("PlayerDefenseVfx");
        var frames = new System.Collections.Generic.List<Sprite>();
        foreach (var sprite in loaded)
            if (sprite.name.StartsWith(prefix)) frames.Add(sprite);
        frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return frames.ToArray();
    }
}
