using UnityEngine;

/// <summary>F1로 디버그 오버레이. 판정 구체/보스 상태.</summary>
[DisallowMultipleComponent]
public sealed class SoulArenaDebugOverlay : MonoBehaviour
{
    private bool show;
    private SoulBossApproachSlash boss;
    private SoulPipelineAvatar player;
    private SoulWeaponHitbox3D bossHit;

    private void Start()
    {
        boss = FindFirstObjectByType<SoulBossApproachSlash>();
        player = FindFirstObjectByType<SoulPipelineAvatar>();
        if (boss != null)
            bossHit = boss.GetComponentInChildren<SoulWeaponHitbox3D>(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) show = !show;
    }

    private void OnGUI()
    {
        if (!show) return;
        string bossState = "none";
        if (boss != null)
        {
            if (!boss.enabled) bossState = "held(prep)";
            else if (boss.IsWindup) bossState = $"WINDUP {boss.AttackNormalizedTime:0.00} (읽기/패링)";
            else if (boss.IsAttacking) bossState = $"SLASH {boss.AttackNormalizedTime:0.00}";
            else bossState = "chase/recovery";
        }

        int trash = FindObjectsByType<SoulArenaTrashMob>(FindObjectsSortMode.None).Length;
        string armed = bossHit != null && bossHit.IsArmed ? "ARMED" : "safe";
        string text =
            $"DEBUG F1\nPlayer HP {(player != null ? player.Health : 0):0}\nBoss {bossState} [{armed}]\nTrash active≈{trash}\nGizmo: 공격구/손히트";
        GUI.Box(new Rect(14, 90, 340, 110), text);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !show || player == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.transform.position + Vector3.up + player.transform.forward * 0.7f, 1.05f);
        if (bossHit != null)
        {
            Gizmos.color = bossHit.IsArmed ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(bossHit.transform.position, 0.32f);
        }
    }
}
