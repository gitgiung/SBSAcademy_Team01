using System.Collections;
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
    [SerializeField] private ZombieSlimeBoss zombieSlime;
    [SerializeField] private EnemySpawner[] spawners = new EnemySpawner[0];
    [SerializeField] private EnemySpawnDirector spawnDirector;
    [SerializeField, Min(0.01f)] private float bossAppearTime = 10f;
    [SerializeField] private Transform bossSpawnAnchor;

    // [�߰�] �ν����Ϳ��� ���� ü�¹� UI (�Ǵ� Slider) �Ҵ�
    [SerializeField] private GameObject bossHealthSlider;
    [Tooltip("보스가 모습을 드러낸 뒤 HP바가 서서히 나타나는 시간(초). 0이면 즉시 표시합니다.")]
    [SerializeField, Min(0f)] private float bossHealthFadeDuration = 1f;

    private float standaloneElapsedTime;
    public float ElapsedTime => spawnDirector != null ? spawnDirector.ElapsedTime : standaloneElapsedTime;
    public EncounterPhase Phase { get; private set; }
    public event System.Action DisplayChanged;

    private EnemyHealth bossHealth;
    private Transform encounterBoss;
    private Vector3 bossSpawnOffset;
    private int displayedSecond = -1;
    private EncounterPhase displayedPhase;
    private CanvasGroup bossHealthCanvasGroup;
    private Coroutine bossHealthFadeRoutine;

    private void Awake()
    {
        if (boss == null && hordeLord == null && zombieSlime == null)
        {
            Debug.LogWarning("BossEncounterController requires a boss reference.", this);
            enabled = false;
            return;
        }

        if (hordeLord != null)
        {
            encounterBoss = hordeLord.transform;
            bossHealth = hordeLord.Health;
        }
        else if (boss != null)
        {
            encounterBoss = boss.transform;
            bossHealth = boss.GetComponent<EnemyHealth>();
        }
        else
        {
            encounterBoss = zombieSlime.transform;
            bossHealth = zombieSlime.Health;
        }

        if (bossSpawnAnchor != null)
            bossSpawnOffset = encounterBoss.position - bossSpawnAnchor.position;

        if (boss != null)
            boss.gameObject.SetActive(false);
        if (hordeLord != null)
            hordeLord.gameObject.SetActive(false);
        if (zombieSlime != null)
            zombieSlime.gameObject.SetActive(false);

        // [�߰�] ���� �� ü�¹� ����
        if (bossHealthSlider != null)
            bossHealthSlider.SetActive(false);
    }

    private void OnEnable()
    {
        if (bossHealth != null) bossHealth.Died += OnBossDied;
        if (Phase == EncounterPhase.BossFight && (bossHealth == null || bossHealth.IsDead))
            OnBossDied();
        if (Phase == EncounterPhase.BossFight && encounterBoss != null)
            ShowBossHealthBar(encounterBoss.GetComponent<BossController>());
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
                enemy.GetComponent<HordeLordBoss>() != null ||
                enemy.GetComponent<ZombieSlimeBoss>() != null)
                continue;

            if (enemy.TryGetComponent<EnemyHealth>(out var health)) health.Despawn();
            else
            {
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
        }

        if (bossSpawnAnchor != null)
            encounterBoss.position = bossSpawnAnchor.position + bossSpawnOffset;

        // 기존 원형 공격의 타격 순간에 모습을 드러내며 전투를 시작합니다.
        if (encounterBoss.TryGetComponent<BossController>(out var entranceBoss))
            entranceBoss.QueueEntrance();
        encounterBoss.gameObject.SetActive(true);

        ShowBossHealthBar(entranceBoss);

        RefreshDisplay(true);
    }

    private void OnBossDied()
    {
        if (Phase != EncounterPhase.BossFight) return;
        Phase = EncounterPhase.AfterBoss;
        HideBossHealthBar();
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

    private void ShowBossHealthBar(BossController entranceBoss)
    {
        if (bossHealthSlider == null) return;
        HideBossHealthBar();
        if (bossHealthCanvasGroup == null)
        {
            bossHealthCanvasGroup = bossHealthSlider.GetComponent<CanvasGroup>();
            if (bossHealthCanvasGroup == null)
                bossHealthCanvasGroup = bossHealthSlider.AddComponent<CanvasGroup>();
        }
        bossHealthCanvasGroup.alpha = 0f;
        bossHealthSlider.SetActive(true);
        bossHealthFadeRoutine = StartCoroutine(FadeBossHealthBar(entranceBoss));
    }

    private IEnumerator FadeBossHealthBar(BossController entranceBoss)
    {
        // 원형 패턴이 터져 보스가 모습을 드러낼 때까지 HP바도 숨깁니다.
        while (entranceBoss != null && entranceBoss.IsEntering && Phase == EncounterPhase.BossFight)
            yield return null;

        float elapsed = 0f;
        float duration = Mathf.Max(0f, bossHealthFadeDuration);
        while (Phase == EncounterPhase.BossFight && bossHealthCanvasGroup != null)
        {
            elapsed += Time.deltaTime;
            bossHealthCanvasGroup.alpha = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            if (bossHealthCanvasGroup.alpha >= 1f) break;
            yield return null;
        }
        bossHealthFadeRoutine = null;
    }

    public void KeepBossHealthVisible()
    {
        if (Phase != EncounterPhase.BossFight || bossHealthSlider == null) return;
        if (bossHealthFadeRoutine != null) StopCoroutine(bossHealthFadeRoutine);
        bossHealthFadeRoutine = null;
        if (bossHealthCanvasGroup == null)
            bossHealthCanvasGroup = bossHealthSlider.GetComponent<CanvasGroup>();
        if (bossHealthCanvasGroup != null) bossHealthCanvasGroup.alpha = 1f;
        bossHealthSlider.SetActive(true);
    }

    private void HideBossHealthBar()
    {
        if (bossHealthFadeRoutine != null) StopCoroutine(bossHealthFadeRoutine);
        bossHealthFadeRoutine = null;
        if (bossHealthCanvasGroup != null) bossHealthCanvasGroup.alpha = 0f;
        if (bossHealthSlider != null) bossHealthSlider.SetActive(false);
    }

    private void OnDisable()
    {
        HideBossHealthBar();
        if (bossHealth != null) bossHealth.Died -= OnBossDied;
        if (spawnDirector != null)
        {
            spawnDirector.TimeAdvanced -= HandleTimeAdvanced;
            if (Phase == EncounterPhase.TimedCombat) spawnDirector.ClearPauseBoundary();
        }
    }
}
