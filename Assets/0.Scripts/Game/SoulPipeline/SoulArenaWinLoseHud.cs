using UnityEngine;

/// <summary>SoulArena 승패 간단 표시. Main 팝업 시스템과 분리.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaWinLoseHud : MonoBehaviour
{
    private SoulPipelineAvatar player;
    private SoulArenaDummyHealth boss;
    private string banner;
    private float bannerTime;
    private bool signaled;

    private void Start()
    {
        player = FindAnyObjectByType<SoulPipelineAvatar>();
        var bossAi = FindAnyObjectByType<SoulBossApproachSlash>();
        if (bossAi != null) boss = bossAi.GetComponent<SoulArenaDummyHealth>();
    }

    private void Update()
    {
        if (bannerTime > 0f) bannerTime -= Time.unscaledDeltaTime;
        if (signaled) return;

        if (player != null && player.IsDead)
        {
            banner = "패배 ? R 재시작 / Esc 로비";
            bannerTime = 999f;
            signaled = true;
            SoulCameraShake.Punch(0.7f);
            SoulArenaKillFeed.Push("패배");
        }
        else if (boss != null && boss.IsDead)
        {
            banner = "보스 처치 ? 파이프라인 검증 통과 후보 / R 재시작";
            bannerTime = 999f;
            signaled = true;
            SoulCameraShake.Punch(0.55f);
            SoulArenaKillFeed.Push("승리");
            var bossAi = boss.GetComponent<SoulBossApproachSlash>();
            if (bossAi != null) bossAi.enabled = false;
        }
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(banner) || bannerTime <= 0f) return;
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Box(new Rect(Screen.width * 0.2f, Screen.height * 0.4f, Screen.width * 0.6f, 64f), banner, style);
    }
}
