using UnityEngine;
using UnityEngine.UI; // Slider ����� ���� �߰�

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
    [SerializeField] private HordeLordBoss hordeLord;
    [SerializeField] private EnemySpawner[] spawners = new EnemySpawner[0];
    [SerializeField] private EnemySpawnDirector spawnDirector;
    [SerializeField, Min(0.01f)] private float bossAppearTime = 10f;

    // [�߰�] �ν����Ϳ��� ���� ü�¹� UI (�Ǵ� Slider) �Ҵ�
    [SerializeField] private GameObject bossHealthSlider;

    private float standaloneElapsedTime;
    public float ElapsedTime => spawnDirector != null ? spawnDirector.ElapsedTime : standaloneElapsedTime;
    public EncounterPhase Phase { get; private set; }
    public event System.Action DisplayChanged;

    private EnemyHealth bossHealth;
    private int displayedSecond = -1;
    private EncounterPhase displayedPhase;

    private void Awake()
    {
        if (boss == null && hordeLord == null)
        {
            Debug.LogWarning("BossEncounterController requires a boss reference.", this);
            enabled = false;
            return;
        }

        bossHealth = hordeLord != null ? hordeLord.Health : boss.GetComponent<EnemyHealth>();
        if (boss != null)
            boss.gameObject.SetActive(false);
        if (hordeLord != null)
            hordeLord.gameObject.SetActive(false);

        // [�߰�] ���� �� ü�¹� ����
        if (bossHealthSlider != null)
            bossHealthSlider.SetActive(false);
    }

    private void OnEnable()
    {
        if (bossHealth != null) bossHealth.Died += OnBossDied;
        if (Phase == EncounterPhase.BossFight && (bossHealth == null || bossHealth.IsDead))
            OnBossDied();
        if (spawnDirector != null)
        {
            spawnDirector.TimeAdvanced += HandleTimeAdvanced;
            if (Phase == EncounterPhase.TimedCombat) spawnDirector.SetPauseBoundary(Mathf.Max(.01f, bossAppearTime));
            if (Phase == EncounterPhase.TimedCombat && ElapsedTime >= bossAppearTime) BeginBossFight();
        }
        RefreshDisplay(true);
    }

    private void HandleTimeAdvanced(float elapsed)
    {
        if (Phase == EncounterPhase.TimedCombat && elapsed >= Mathf.Max(.01f, bossAppearTime)) BeginBossFight();
        RefreshDisplay();
    }

    private void Update()
    {
        if (Phase == EncounterPhase.BossFight) return;
        if (spawnDirector != null) { RefreshDisplay(); return; }
        standaloneElapsedTime += Time.deltaTime;
        if (Phase == EncounterPhase.TimedCombat && ElapsedTime >= Mathf.Max(0.01f, bossAppearTime))
        {
            standaloneElapsedTime = Mathf.Max(0.01f, bossAppearTime);
            BeginBossFight();
        }
        RefreshDisplay();
    }

    private void BeginBossFight()
    {
        Phase = EncounterPhase.BossFight;

        if (spawnDirector != null)
        {
            spawnDirector.SetPaused(true);
            if (bossHealth != null) bossHealth.SetExperienceDropper(spawnDirector.SpawnPool as IEnemyExperienceDropper);
        }
        else foreach (var spawner in spawners)
        {
            if (spawner != null)
                spawner.StopSpawning();
        }

        foreach (var enemy in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Include))
        {
            if (enemy.gameObject.scene != gameObject.scene ||
                enemy.GetComponent<BossController>() != null ||
                enemy.GetComponent<HordeLordBoss>() != null)
                continue;

            if (enemy.TryGetComponent<EnemyHealth>(out var health)) health.Despawn();
            else
            {
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
        }

        if (hordeLord != null)
            hordeLord.gameObject.SetActive(true);
        else
            boss.gameObject.SetActive(true);

        // [�߰�] ���� ���� �� �����̴� Ȱ��ȭ
        if (bossHealthSlider != null)
            bossHealthSlider.SetActive(true);

        RefreshDisplay(true);
    }

    private void OnBossDied()
    {
        if (Phase != EncounterPhase.BossFight) return;
        Phase = EncounterPhase.AfterBoss;
        if (bossHealthSlider != null) bossHealthSlider.SetActive(false);
        if (spawnDirector != null) { spawnDirector.ClearPauseBoundary(); spawnDirector.SetPaused(false); }
        else foreach (var spawner in spawners)
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
        if (spawnDirector != null)
        {
            spawnDirector.TimeAdvanced -= HandleTimeAdvanced;
            if (Phase == EncounterPhase.TimedCombat) spawnDirector.ClearPauseBoundary();
        }
    }
}