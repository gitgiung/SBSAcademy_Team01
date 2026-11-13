using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("지정하면 회차별 프리팹을 생성합니다. 비어 있으면 기존 단일 보스 설정을 사용합니다.")]
    [SerializeField] private BossEncounterSchedule schedule;
    [SerializeField] private Transform bossTarget;
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
    private readonly List<BossEncounterSchedule.Entry> rounds = new List<BossEncounterSchedule.Entry>();
    private int nextRound;
    private Transform preparationRoot;
    private GameObject spawnedBoss;
    private bool ownsCombatPause;

    private bool HasNextRound => schedule != null ? nextRound < rounds.Count : Phase == EncounterPhase.TimedCombat;
    private float NextAppearTime => schedule != null ? rounds[nextRound].AppearTime : Mathf.Max(.01f, bossAppearTime);

    private void Awake()
    {
        if (schedule == null && boss == null && hordeLord == null && zombieSlime == null)
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
        else if (zombieSlime != null)
        {
            encounterBoss = zombieSlime.transform;
            bossHealth = zombieSlime.Health;
        }

        if (bossSpawnAnchor != null && encounterBoss != null)
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

        if (schedule != null)
        {
            // 기존 씬 보스는 대기로 남기고, 일정의 프리팹으로만 전투합니다.
            bossHealth = null;
            encounterBoss = null;
            BuildRounds();
            if (rounds.Count == 0) Phase = EncounterPhase.AfterBoss;
            preparationRoot = new GameObject("Boss Preparation").transform;
            preparationRoot.SetParent(transform, false);
            preparationRoot.gameObject.SetActive(false);
        }
    }

    private void BuildRounds()
    {
        if (!schedule.TryValidateTimes(out string error))
        {
            Debug.LogError(error, this);
            return;
        }
        for (int i = 0; i < schedule.Entries.Count; i++)
        {
            var entry = schedule.Entries[i];
            string location = $"BossEncounter: {i + 1}회차 ({entry.AppearTime}초)";
            if (entry.BossPrefab == null)
            {
                Debug.LogWarning($"{location} 보스 프리팹 미지정: 이 회차를 건너뜁니다.", this);
                continue;
            }
            if (!BossEncounterSchedule.IsSupportedBoss(entry.BossPrefab))
            {
                Debug.LogError($"{location} 프리팹의 EnemyHealth·지원 보스 컴포넌트·야수 패턴 선택기를 확인하세요. 이 회차를 건너뜁니다.", this);
                continue;
            }
            if (spawnDirector != null && entry.AppearTime > spawnDirector.Settings.Duration)
            {
                Debug.LogError($"{location} 등장 시각이 Director의 진행 시간 한도를 넘습니다. 이 회차를 건너뜁니다.", this);
                continue;
            }
            // 공유 에셋의 Play 중 편집이 진행 중인 판의 정지 경계를 바꾸지 않게 복사합니다.
            rounds.Add(new BossEncounterSchedule.Entry(entry.AppearTime, entry.BossPrefab, entry.SpawnOffset));
        }
    }

    private void OnEnable()
    {
        if (bossHealth != null) bossHealth.Died += OnBossDied;
        if (spawnDirector != null) spawnDirector.TimeAdvanced += HandleTimeAdvanced;
        if (Phase == EncounterPhase.BossFight && (bossHealth == null || bossHealth.IsDead))
            OnBossDied();
        if (Phase == EncounterPhase.BossFight && encounterBoss != null)
            ShowBossHealthBar(encounterBoss.GetComponent<BossController>());
        ArmNextBoundary();
        TryBeginDueRound();
        RefreshDisplay(true);
    }

    private void ArmNextBoundary()
    {
        if (spawnDirector == null || Phase == EncounterPhase.BossFight) return;
        if (HasNextRound) spawnDirector.SetPauseBoundary(NextAppearTime);
        else spawnDirector.ClearPauseBoundary();
    }

    private void TryBeginDueRound()
    {
        // 비활성화 중 여러 경계가 지났거나 실행 시 설정 오류로 건너뛴 회차도
        // 모두 처리합니다. 과거 경계를 Director에 남기면 다음 Tick이 진행되지 않습니다.
        while (Phase == EncounterPhase.TimedCombat && HasNextRound && ElapsedTime >= NextAppearTime)
            BeginBossFight();
    }

    private void HandleTimeAdvanced(float elapsed)
    {
        TryBeginDueRound();
        RefreshDisplay();
    }

    private void Update()
    {
        if (Phase == EncounterPhase.BossFight) return;
        if (spawnDirector != null) { RefreshDisplay(); return; }
        standaloneElapsedTime += Time.deltaTime;
        if (Phase == EncounterPhase.TimedCombat && HasNextRound && ElapsedTime >= NextAppearTime)
        {
            standaloneElapsedTime = NextAppearTime;
            TryBeginDueRound();
        }
        RefreshDisplay();
    }

#if UNITY_EDITOR
    [ContextMenu("테스트/다음 보스 즉시 소환")]
    private void SpawnNextBossForTest()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || Phase != EncounterPhase.TimedCombat ||
            !HasNextRound || Time.timeScale <= 0f)
            return;

        BeginBossFight();
    }
#endif

    private void BeginBossFight()
    {
        if (schedule != null && !PrepareScheduledBoss())
        {
            nextRound++;
            Phase = HasNextRound ? EncounterPhase.TimedCombat : EncounterPhase.AfterBoss;
            ArmNextBoundary();
            RefreshDisplay(true);
            return;
        }
        Phase = EncounterPhase.BossFight;
        ownsCombatPause = true;

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

        if (schedule == null && bossSpawnAnchor != null)
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
        HideBossHealthBar();
        if (bossHealth != null)
        {
            bossHealth.Died -= OnBossDied;
            if (schedule != null) bossHealth.SetHealthSlider(null);
        }
        if (schedule != null)
        {
            bossHealth = null;
            encounterBoss = null;
            spawnedBoss = null;
            nextRound++;
        }
        Phase = schedule != null && HasNextRound ? EncounterPhase.TimedCombat : EncounterPhase.AfterBoss;
        ArmNextBoundary();
        ResumeCombat();
        TryBeginDueRound();
        RefreshDisplay(true);
    }

    private bool PrepareScheduledBoss()
    {
        var entry = rounds[nextRound];
        if (!BossEncounterSchedule.IsSupportedBoss(entry.BossPrefab))
        {
            Debug.LogError($"BossEncounter: {entry.AppearTime}초 보스 프리팹을 사용할 수 없어 건너뜁니다.", this);
            return false;
        }
        Transform target = bossTarget != null ? bossTarget : boss != null ? boss.Target : null;
        if (target == null)
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>())
                if (behaviour is IContactDamageReceiver && behaviour.gameObject.scene == gameObject.scene)
                { target = behaviour.transform; break; }
        if (target == null)
        {
            Debug.LogError($"BossEncounter: {entry.AppearTime}초 보스의 Boss Target을 찾을 수 없어 건너뜁니다.", this);
            return false;
        }

        // 활성 프리팹도 OnEnable 전에 대상·체력바·등장 대기를 주입합니다.
        spawnedBoss = Instantiate(entry.BossPrefab, preparationRoot);
        spawnedBoss.SetActive(false);
        spawnedBoss.transform.SetParent(transform, true);
        spawnedBoss.transform.position = (bossSpawnAnchor != null ? bossSpawnAnchor.position : transform.position) + entry.SpawnOffset;
        encounterBoss = spawnedBoss.transform;
        bossHealth = spawnedBoss.GetComponent<EnemyHealth>();
        Slider slider = bossHealthSlider != null ? bossHealthSlider.GetComponentInChildren<Slider>(true) : null;
        bossHealth.SetHealthSlider(slider);
        bossHealth.Died += OnBossDied;
        if (spawnedBoss.TryGetComponent<BossController>(out var beast)) beast.SetTarget(target);
        if (spawnedBoss.TryGetComponent<HordeLordBoss>(out var horde)) horde.SetTarget(target);
        if (spawnedBoss.TryGetComponent<ZombieSlimeBoss>(out var slime)) slime.SetTarget(target);
        return true;
    }

    private void ResumeCombat()
    {
        if (!ownsCombatPause) return;
        ownsCombatPause = false;
        if (spawnDirector != null) spawnDirector.SetPaused(false);
        else foreach (var spawner in spawners)
            if (spawner != null) spawner.RestartSpawning();
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

    private void OnDestroy()
    {
        if (spawnedBoss != null)
        {
            spawnedBoss.SetActive(false);
            Destroy(spawnedBoss);
        }
        if (ownsCombatPause && spawnDirector != null) spawnDirector.ClearPauseBoundary();
        ResumeCombat();
    }
}
