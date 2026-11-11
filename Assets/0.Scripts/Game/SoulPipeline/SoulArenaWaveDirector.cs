using UnityEngine;

/// <summary>
/// 짧은 잡몹 구간 후 보스 활성화. 뱀서 껍데기 → 본편 보스 감각의 최소 버전.
/// 이완 중에는 잡몹을 가끔 보충한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulArenaWaveDirector : MonoBehaviour
{
    [SerializeField] private float prepSeconds = 22f;
    [SerializeField] private float trashRespawnInterval = 4.0f;
    [SerializeField] private int maxTrash = 8;
    [SerializeField] private int minTrashAfterMid = 3;
    [SerializeField] private bool holdBossUntilPrepEnds = true;

    private SoulBossApproachSlash boss;
    private float elapsed;
    private float respawnTimer;
    private bool bossReleased;
    private bool midWarned;
    private GUIStyle style;

    private void Start()
    {
        boss = FindAnyObjectByType<SoulBossApproachSlash>();
        if (holdBossUntilPrepEnds && boss != null)
            boss.enabled = false;
        respawnTimer = trashRespawnInterval * 0.5f;
        SoulArenaKillFeed.Push("이완 시작 ? 잡몹 정리 또는 대기");
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (bossReleased) return;

        MaybeRespawnTrash();

        if (!midWarned && elapsed >= prepSeconds * 0.55f)
        {
            midWarned = true;
            SoulArenaKillFeed.Push("보스 임박");
            SoulCameraShake.Punch(0.2f);
        }

        if (boss == null) return;

        int aliveTrash = CountAliveTrash();
        bool trashCleared = aliveTrash == 0 && elapsed > 3f;
        if (elapsed >= prepSeconds || trashCleared)
            ReleaseBoss();
    }

    private void MaybeRespawnTrash()
    {
        respawnTimer -= Time.deltaTime;
        if (respawnTimer > 0f) return;
        respawnTimer = trashRespawnInterval;

        int alive = CountAliveTrash();
        int softCap = elapsed > prepSeconds * 0.5f
            ? Mathf.Max(minTrashAfterMid, maxTrash - 2)
            : maxTrash;
        if (alive >= softCap) return;

        float ang = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(6.5f, 9.5f);
        Vector3 pos = new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
        Color c = Color.Lerp(new Color(0.35f, 0.8f, 0.4f), new Color(0.7f, 0.85f, 0.25f), Random.value);
        SoulArenaTrashMob.Spawn(pos, c);
        SoulArenaKillFeed.Push("잡몹 보충");
    }

    private static int CountAliveTrash()
    {
        int count = 0;
        var trash = FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Exclude);
        for (int i = 0; i < trash.Length; i++)
        {
            if (trash[i] == null) continue;
            var hp = trash[i].GetComponent<SoulArenaDummyHealth>();
            if (hp != null && hp.IsDead) continue;
            count++;
        }
        return count;
    }

    private void ReleaseBoss()
    {
        if (bossReleased) return;
        bossReleased = true;
        if (boss != null) boss.enabled = true;
        SoulArenaKillFeed.Push("보스 등장");
        SoulCameraShake.Punch(0.55f);
        Debug.Log("[SoulPipeline] 보스 구간 시작 (잡몹 이완 종료)", this);
    }

    private void OnGUI()
    {
        if (bossReleased) return;
        style ??= new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = new Color(1f, 0.85f, 0.4f) } };
        float left = Mathf.Max(0f, prepSeconds - elapsed);
        GUI.Label(new Rect(14, 56, 720, 24),
            $"이완 {left:0.0}s · 잡몹 {CountAliveTrash()} (정리하거나 시간 후 보스)", style);
    }
}
