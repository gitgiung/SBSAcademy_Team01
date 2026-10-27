using UnityEngine;

public enum EncounterPhase
{
    TimedCombat,
    BossFight,
    AfterBoss
}

[DisallowMultipleComponent]
public sealed class BossEncounterController : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private EnemySpawner[] spawners = new EnemySpawner[0];
    [SerializeField, Min(0.01f)] private float bossAppearTime = 10f;

    public float ElapsedTime { get; private set; }
    public EncounterPhase Phase { get; private set; }
    public event System.Action DisplayChanged;

    private EnemyHealth bossHealth;
    private int displayedSecond = -1;
    private EncounterPhase displayedPhase;

    private void Awake()
    {
        if (boss == null)
        {
            Debug.LogWarning("BossEncounterController requires a boss reference.", this);
            enabled = false;
            return;
        }
        bossHealth = boss.GetComponent<EnemyHealth>();
        boss.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (bossHealth != null) bossHealth.Died += OnBossDied;
        if (Phase == EncounterPhase.BossFight && (bossHealth == null || bossHealth.IsDead))
            OnBossDied();
        RefreshDisplay(true);
    }

    private void Update()
    {
        if (Phase == EncounterPhase.BossFight) return;
        ElapsedTime += Time.deltaTime;
        if (Phase == EncounterPhase.TimedCombat && ElapsedTime >= Mathf.Max(0.01f, bossAppearTime))
        {
            ElapsedTime = Mathf.Max(0.01f, bossAppearTime);
            BeginBossFight();
        }
        RefreshDisplay();
    }

    private void BeginBossFight()
    {
        Phase = EncounterPhase.BossFight;

        foreach (var spawner in spawners)
        {
            if (spawner != null)
                spawner.StopSpawning();
        }

        // 현재 Encounter와 같은 Scene에 있는 일반 몬스터 제거
        // 비활성화된 몬스터도 검색에 포함
        foreach (var enemy in FindObjectsByType<EnemyFollow>(
            FindObjectsInactive.Include))
        {
            if (enemy.gameObject.scene != gameObject.scene ||
                enemy.GetComponent<BossController>() != null)
                continue;

            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }

        boss.gameObject.SetActive(true);
        RefreshDisplay(true);
    }

    private void OnBossDied()
    {
        if (Phase != EncounterPhase.BossFight) return;
        Phase = EncounterPhase.AfterBoss;
        foreach (var spawner in spawners)
            if (spawner != null) spawner.RestartSpawning();
        RefreshDisplay(true);
    }

    private void RefreshDisplay(bool force = false)
    {
        int seconds = Mathf.FloorToInt(ElapsedTime);
        if (!force && displayedSecond == seconds && displayedPhase == Phase) return;
        displayedSecond = seconds;
        displayedPhase = Phase;
        DisplayChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (bossHealth != null) bossHealth.Died -= OnBossDied;
    }
}
