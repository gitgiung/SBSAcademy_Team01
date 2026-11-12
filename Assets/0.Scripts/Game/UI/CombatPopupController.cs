using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>한 씬의 전투 팝업 연결과 팝업에 의한 전투 정지를 소유합니다.</summary>
[DisallowMultipleComponent]
public sealed class CombatPopupController : MonoBehaviour
{
    private const string PauseKey = "CombatPausePopup";
    private const string LevelUpKey = "CombatLevelUpPopup";

    [SerializeField] private Button pauseButton;
    [SerializeField] private UI_CombatPopup pausePopup;
    [SerializeField] private UI_CombatPopup levelUpPopup;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Player statusPlayer;
    private PlayerStatusPanel statusPanel;
    private LevelUpSelectionPanel levelUpPanel;
    private readonly System.Random levelUpRandom = new System.Random();
    private readonly List<LevelUpOffer> levelUpOffers = new List<LevelUpOffer>();
    private const int LevelUpRerollLimit = 10;
    private int levelUpRerollsRemaining = LevelUpRerollLimit;

    private const float ResumeHoldSeconds = 0.5f;
    private const float LevelUpSlowMotionScale = 0.01f;
    private const float LevelUpSlowMotionSeconds = 0.5f;

    private readonly HashSet<UI_CombatPopup> activePopups = new HashSet<UI_CombatPopup>();
    private float previousTimeScale;
    private bool ownsPause;
    private PlayerData growthSource;
    private bool automaticLevelUp;
    private bool completionPending;
    private Coroutine advanceLevelUp;
    private Coroutine resumeRoutine;

    public RectTransform LevelUpContentRoot => levelUpPopup != null ? levelUpPopup.ContentRoot : null;

    private void OnEnable()
    {
        if (pauseButton == null || pausePopup == null || levelUpPopup == null || pausePopup == levelUpPopup)
        {
            Debug.LogError("CombatPopupController requires a pause button and two distinct popup references.", this);
            enabled = false;
            return;
        }
        pausePopup.Bind(this);
        levelUpPopup.Bind(this);
        if (!Managers.UI.RegisterPopupUI(PauseKey, pausePopup)
            || !Managers.UI.RegisterPopupUI(LevelUpKey, levelUpPopup))
        {
            enabled = false;
            return;
        }
        pauseButton.onClick.AddListener(OnPauseClicked);
        if (gameManager != null)
        {
            gameManager.StateChanged += HandleStateChanged;
        }
        BindGrowthSource();
        // 비활성 중 승패가 결정됐다면 재연결한 플레이어의 대기도 정리합니다.
        if (gameManager != null) HandleStateChanged(gameManager.state);
    }

    // Player.Awake와 씬 UI의 OnEnable 순서에 의존하지 않습니다.
    private void Start() { BindGrowthSource(); TryShowPendingLevelUp(); }

    private void BindGrowthSource()
    {
        if (growthSource != null) return;
        ResolveStatusPlayer();
        if (statusPlayer == null) return;
        growthSource = statusPlayer.data != null ? statusPlayer.data : statusPlayer.GetComponent<PlayerData>();
        if (growthSource == null) return;
        growthSource.EXPChanged += HandleExperienceChanged;
        growthSource.PlayerDeath += HandleGrowthPlayerDeath;
        TryShowPendingLevelUp();
    }

    private void HandleExperienceChanged(float current, float maximum)
    {
        if (automaticLevelUp && !completionPending && growthSource.PendingLevelUps == 0)
        {
            CancelLevelUpAdvance();
            automaticLevelUp = false;
            HideLevelUpPopup();
        }
        TryShowPendingLevelUp();
    }

    private void TryShowPendingLevelUp()
    {
        if (!CanShow() || growthSource == null || growthSource.PendingLevelUps == 0
            || completionPending || levelUpPopup == null || levelUpPopup.gameObject.activeSelf) return;
        automaticLevelUp = true;
        if (ShowLevelUp() == null) automaticLevelUp = false;
    }

    private void HandleGrowthPlayerDeath()
    {
        growthSource.CancelPendingLevelUps();
        CloseOwnedPopups();
    }

    private void OnPauseClicked() { ShowPause(); }

    /// <summary>전화 수신 등 OS 중단 시 전투 일시정지 창을 강제로 엽니다.</summary>
    private void OnApplicationPause(bool paused)
    {
        if (!paused) return;
        ForcePauseForInterruption();
    }

    private void ForcePauseForInterruption()
    {
        if (!CanShow() || pausePopup == null) return;
        // 레벨업 등으로 이미 멈춰 있으면 창을 겹치지 않습니다.
        if (pausePopup.gameObject.activeSelf) return;
        if (levelUpPopup != null && levelUpPopup.gameObject.activeSelf) return;
        ShowPause();
    }

    public UI_CombatPopup ShowPause()
    {
        if (!CanShow() || pausePopup == null) return null;
        if (pausePopup.ContentRoot != null && pausePopup.CloseButton != null)
        {
            ResolveStatusPlayer();
            if (statusPanel == null)
                statusPanel = Util.GetOrAddComponent<PlayerStatusPanel>(pausePopup.gameObject);
            TMPro.TMP_FontAsset font = null;
            foreach (var label in pausePopup.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if (label.font != null) { font = label.font; break; }
            statusPanel.Initialize(pausePopup.ContentRoot, pausePopup.CloseButton, statusPlayer != null ? statusPlayer.data : null, font);
        }
        UI_CombatPopup popup = Managers.UI.ShowPopupUI<UI_CombatPopup>(PauseKey);
        if (popup != null) popup.Focus();
        return popup;
    }

    /// <summary>보유하지 않은 약공격을 추첨하고 선택 시 보유 목록에 등록합니다.</summary>
    public UI_CombatPopup ShowLevelUp()
    {
        if (!CanShow() || levelUpPopup == null) return null;
        EnsureLevelUpPanel();
        if (levelUpPanel == null) return null;
        // 중복 열기나 겹친 일시정지창에서 복귀할 때 선택지가 바뀌지 않게 합니다.
        if (!levelUpPopup.gameObject.activeSelf && !PrepareLevelUpChoices()) return null;
        return OpenLevelUpPanel();
    }

    private bool PrepareLevelUpChoices()
    {
        ResolveStatusPlayer();
        if (statusPlayer == null || statusPlayer.data == null)
        {
            Debug.LogError("레벨업 스킬을 등록할 Player/PlayerData가 없습니다.", this);
            return false;
        }
        PlayerData playerData = statusPlayer.data;
        PlayerLoadout loadout = playerData.Loadout;
        CombatSkillTableFile table = CombatSkillTable.CreateSnapshot();
        HashSet<int> tacticalExcluded = CollectTacticalExcluded(loadout, table);
        const int count = LevelUpMixedDraft.DefaultCount;
        var stats = playerData.StatCards.Draw(PlayerStatTable.LoadCards(), count, levelUpRandom.NextDouble);
        var attacks = LevelUpSkillReward.DrawChoices(table, loadout, count, levelUpRandom.NextDouble);
        var tacticals = loadout.HasFreeTacticalSlot
            ? LevelUpSkillDraft.Draw(table, count, 30000, 40000, levelUpRandom.NextDouble, tacticalExcluded)
            : new List<CombatSkillRecord>();
        var skills = DrawLevelUpMix(attacks, tacticals, count);
        levelUpOffers.Clear();
        if (skills.Count == 0 && stats.Count == 0)
        {
            if (!loadout.HasFreeAttackSlot) ShowAttackSlotsFull();
            else levelUpPanel.ShowMessage("카드 없음", "카드가 소진되었습니다.", CompleteLevelUp);
            return true;
        }
        int statIndex = 0;
        int skillIndex = 0;
        while (levelUpOffers.Count < count && (statIndex < stats.Count || skillIndex < skills.Count))
        {
            if (statIndex < stats.Count && (skillIndex >= skills.Count || levelUpRandom.NextDouble() < LevelUpMixedDraft.DefaultStatShare))
                levelUpOffers.Add(CreateStatOffer(playerData, stats[statIndex++]));
            else
                levelUpOffers.Add(CreateSkillOffer(loadout, skills[skillIndex++], table));
        }
        PresentLevelUpOffers();
        return true;
    }

    private void PresentLevelUpOffers()
    {
        var cards = new List<LevelUpCardData>(levelUpOffers.Count);
        for (int i = 0; i < levelUpOffers.Count; i++) cards.Add(levelUpOffers[i].Card);
        levelUpPanel.SetChoices(cards, index =>
        {
            if (completionPending || index < 0 || index >= levelUpOffers.Count || !CanShow()) return;
            levelUpOffers[index].Apply();
        }, levelUpRerollsRemaining, TryRerollLevelUpChoice);
    }

    private void TryRerollLevelUpChoice(int index)
    {
        if (completionPending || levelUpRerollsRemaining <= 0 || index < 0 || index >= levelUpOffers.Count || !CanShow())
            return;
        if (!TryReplaceLevelUpOffer(index))
        {
            Debug.Log("리롤할 다른 카드가 없습니다.", this);
            return;
        }
        levelUpRerollsRemaining--;
        PresentLevelUpOffers();
    }

    private bool TryReplaceLevelUpOffer(int index)
    {
        ResolveStatusPlayer();
        if (statusPlayer == null || statusPlayer.data == null) return false;
        PlayerData playerData = statusPlayer.data;
        PlayerLoadout loadout = playerData.Loadout;
        CombatSkillTableFile table = CombatSkillTable.CreateSnapshot();
        var shownSkills = new HashSet<int>();
        var shownStats = new HashSet<PlayerStat>();
        for (int i = 0; i < levelUpOffers.Count; i++)
        {
            int id = levelUpOffers[i].Card.SkillId;
            if (id >= 91000) shownStats.Add((PlayerStat)(id - 91000));
            else shownSkills.Add(id);
        }

        var stats = playerData.StatCards.Draw(PlayerStatTable.LoadCards(), 32, levelUpRandom.NextDouble);
        var attacks = LevelUpSkillReward.DrawChoices(table, loadout, 32, levelUpRandom.NextDouble);
        var tacticals = loadout.HasFreeTacticalSlot
            ? LevelUpSkillDraft.Draw(table, 32, 30000, 40000, levelUpRandom.NextDouble, CollectTacticalExcluded(loadout, table))
            : new List<CombatSkillRecord>();
        bool statFirst = levelUpRandom.NextDouble() < LevelUpMixedDraft.DefaultStatShare;
        if (statFirst && TryTakeStatOffer(playerData, stats, shownStats, out LevelUpOffer replacement))
        {
            levelUpOffers[index] = replacement;
            return true;
        }
        if (TryTakeSkillOffer(loadout, table, attacks, tacticals, shownSkills, out replacement))
        {
            levelUpOffers[index] = replacement;
            return true;
        }
        if (!statFirst && TryTakeStatOffer(playerData, stats, shownStats, out replacement))
        {
            levelUpOffers[index] = replacement;
            return true;
        }
        return false;
    }

    private static HashSet<int> CollectTacticalExcluded(PlayerLoadout loadout, CombatSkillTableFile table)
    {
        var tacticalExcluded = new HashSet<int>();
        for (int i = 0; i < PlayerLoadout.TacticalCapacity; i++)
        {
            if (loadout.GetTacticalSkill(i) is Data.SkillData owned)
                tacticalExcluded.Add(owned.DataId);
        }
        if (table?.skills != null)
        {
            foreach (CombatSkillRecord skill in table.skills)
            {
                if (skill == null || skill.Id < 30000 || skill.Id >= 40000) continue;
                // 버프·오라 등은 시전 연결 전이므로 추첨에서 제외합니다.
                if (!TacticalSkillEffect.IsSupported(skill.EffectType))
                    tacticalExcluded.Add(skill.Id);
            }
        }
        return tacticalExcluded;
    }

    private LevelUpOffer CreateStatOffer(PlayerData playerData, PlayerStatCard stat)
    {
        int level = playerData.StatCards.Level(stat.Stat);
        PlayerStatCard chosen = stat;
        return new LevelUpOffer
        {
            Card = CreateStatCard(chosen, level),
            Apply = () =>
            {
                if (playerData.TryUpgradeStat(chosen, level + 1)) CompleteLevelUp();
                else levelUpPanel.ShowMessage("스탯 강화 불가", "선택한 스탯을 강화할 수 없습니다.", CompleteLevelUp);
            }
        };
    }

    private LevelUpOffer CreateSkillOffer(PlayerLoadout loadout, CombatSkillRecord skill, CombatSkillTableFile table)
    {
        CombatSkillRecord chosen = skill;
        return new LevelUpOffer
        {
            Card = CreateSkillCard(chosen, table.rarities, loadout),
            Apply = () => ApplySkillChoice(loadout, chosen)
        };
    }

    private bool TryTakeStatOffer(PlayerData playerData, List<PlayerStatCard> stats, HashSet<PlayerStat> shown, out LevelUpOffer offer)
    {
        offer = default;
        if (stats == null) return false;
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i] == null || shown.Contains(stats[i].Stat)) continue;
            offer = CreateStatOffer(playerData, stats[i]);
            return true;
        }
        return false;
    }

    private bool TryTakeSkillOffer(PlayerLoadout loadout, CombatSkillTableFile table, List<CombatSkillRecord> attacks,
        List<CombatSkillRecord> tacticals, HashSet<int> shown, out LevelUpOffer offer)
    {
        CombatSkillRecord attack = FirstUnseenSkill(attacks, shown);
        CombatSkillRecord tactical = FirstUnseenSkill(tacticals, shown);
        bool tacticalFirst = tactical != null && (attack == null || levelUpRandom.NextDouble() < 0.25);
        CombatSkillRecord chosen = tacticalFirst ? tactical : attack ?? tactical;
        if (chosen == null)
        {
            offer = default;
            return false;
        }
        offer = CreateSkillOffer(loadout, chosen, table);
        return true;
    }

    private static CombatSkillRecord FirstUnseenSkill(List<CombatSkillRecord> skills, HashSet<int> shown)
    {
        if (skills == null) return null;
        for (int i = 0; i < skills.Count; i++)
            if (skills[i] != null && !shown.Contains(skills[i].Id)) return skills[i];
        return null;
    }

    private struct LevelUpOffer
    {
        public LevelUpCardData Card;
        public System.Action Apply;
    }

    private void ApplySkillChoice(PlayerLoadout loadout, CombatSkillRecord selected)
    {
        if (selected == null || loadout == null || !CanShow()) return;
        bool isTactical = selected.Id >= 30000 && selected.Id < 40000;
        bool alreadyOwned = isTactical ? OwnsTactical(loadout, selected.Id) : loadout.GetAttackSkillStack(selected.Id) > 0;
        if (LevelUpSkillReward.TryAcquire(loadout, selected)) CompleteLevelUp();
        else if (!alreadyOwned && (isTactical ? !loadout.HasFreeTacticalSlot : !loadout.HasFreeAttackSlot)) ShowAttackSlotsFull();
        else levelUpPanel.ShowMessage("스킬 획득 불가", "선택한 스킬을 등록하거나 강화할 수 없습니다.", CompleteLevelUp);
    }

    private List<CombatSkillRecord> DrawLevelUpMix(List<CombatSkillRecord> attacks, List<CombatSkillRecord> tacticals, int count)
    {
        var result = new List<CombatSkillRecord>(count);
        int attackIndex = 0;
        int tacticalIndex = 0;
        while (result.Count < count && (attackIndex < attacks.Count || tacticalIndex < tacticals.Count))
        {
            bool tactical = tacticalIndex < tacticals.Count && (attackIndex >= attacks.Count || levelUpRandom.NextDouble() < 0.25);
            result.Add(tactical ? tacticals[tacticalIndex++] : attacks[attackIndex++]);
        }
        return result;
    }

    private static bool OwnsTactical(PlayerLoadout loadout, int skillId)
    {
        for (int i = 0; i < PlayerLoadout.TacticalCapacity; i++)
            if (loadout.GetTacticalSkill(i)?.DataId == skillId) return true;
        return false;
    }

    private void ResolveStatusPlayer()
    {
        if (statusPlayer != null) return;
        foreach (Player candidate in FindObjectsByType<Player>())
            if (candidate.gameObject.scene == gameObject.scene) { statusPlayer = candidate; break; }
    }

    private void ShowAttackSlotsFull()
    {
        levelUpPanel.ShowMessage("스킬 슬롯 가득 참", "더 이상 등록할 스킬 슬롯이 없습니다.", CompleteLevelUp);
    }

    private UI_CombatPopup OpenLevelUpPanel()
    {
        UI_CombatPopup popup = Managers.UI.ShowPopupUI<UI_CombatPopup>(LevelUpKey);
        if (popup != null) levelUpPanel?.Focus();
        return popup;
    }

    private static LevelUpCardData CreateSkillCard(CombatSkillRecord skill, List<SkillRarityRecord> rarities,
        PlayerLoadout loadout = null)
    {
        string id = SkillRarityRecord.NormalizeId(skill.Rarity);
        SkillRarityRecord rarity = rarities.Find(value => value != null
            && string.Equals(SkillRarityRecord.NormalizeId(value.Id), id, System.StringComparison.OrdinalIgnoreCase));
        Color accent = Color.white;
        if (rarity != null && ColorUtility.TryParseHtmlString(rarity.ColorHex, out Color color)) accent = color;
        string description = LevelUpSkillDraft.FormatDescription(skill);
        int currentStack = loadout != null ? loadout.GetAttackSkillStack(skill.Id) : 0;
        if (currentStack > 0 && currentStack < PlayerLoadout.MaxAttackSkillStack)
        {
            int nextStack = currentStack + 1;
            float multiplier = PlayerLoadout.DamageMultiplierForStack(nextStack);
            description += $"\n강화 {currentStack}→{nextStack} (피해 ×{multiplier:0.#})";
        }
        return new LevelUpCardData
        {
            SkillId = skill.Id, Name = skill.Name, Rarity = rarity != null ? rarity.Id : id, Accent = accent,
            Icon = string.IsNullOrEmpty(skill.Icon) ? null : Managers.Resource.Load<Sprite>(skill.Icon),
            ActiveDescription = description, ActiveButtonLabel = skill.Id >= 30000 ? "Skill" : skill.Id >= 20000 ? "C" : "N",
            FlavorText = skill.Explanation
        };
    }

    private static LevelUpCardData CreateStatCard(PlayerStatCard stat, int level) => new LevelUpCardData
    {
        SkillId = 91000 + (int)stat.Stat,
        Name = stat.Name,
        Rarity = "스탯 강화",
        Accent = new Color32(146, 222, 191, 255),
        Description = $"Lv. {level} → {level + 1}\n{stat.FormatValue(level)} → {stat.FormatValue(level + 1)}"
    };

    public UI_CombatPopup ShowLevelUp(IReadOnlyList<LevelUpCardData> choices, System.Action<int> onSelected)
    {
        if (!CanShow() || levelUpPopup == null) return null;
        // 자동 선택 중인 내용과 완료 계약은 외부 표시 호출로 교체하지 않습니다.
        if (automaticLevelUp || completionPending) return levelUpPopup;
        EnsureLevelUpPanel();
        if (levelUpPanel == null) return null;
        levelUpPanel.SetChoices(choices, onSelected);
        return OpenLevelUpPanel();
    }

    private void EnsureLevelUpPanel()
    {
        if (levelUpPanel != null || levelUpPopup.ContentRoot == null) return;
        levelUpPanel = Util.GetOrAddComponent<LevelUpSelectionPanel>(levelUpPopup.gameObject);
        TMPro.TMP_FontAsset font = null;
        foreach (var label in levelUpPopup.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if (label.font != null) { font = label.font; break; }
        levelUpPanel.Initialize(levelUpPopup.ContentRoot, font);
    }

    public void CompleteLevelUp()
    {
        if (completionPending || !CanShow() || levelUpPopup == null || !levelUpPopup.gameObject.activeSelf) return;
        if (automaticLevelUp)
        {
            if (growthSource == null || !growthSource.TryCompleteLevelUp()) return;
            completionPending = true;
            // 같은 클릭/Submit 입력이 다음 카드까지 처리되지 않도록 다음 프레임에 전환합니다.
            advanceLevelUp = StartCoroutine(AdvanceLevelUp());
            return;
        }
        if (levelUpPopup != null)
            HideLevelUpPopup();
        TryShowPendingLevelUp();
    }

    private IEnumerator AdvanceLevelUp()
    {
        yield return null;
        advanceLevelUp = null;
        completionPending = false;
        if (!CanShow()) yield break;
        if (growthSource != null && growthSource.PendingLevelUps > 0)
        {
            if (PrepareLevelUpChoices()) levelUpPanel.Focus();
            yield break;
        }
        automaticLevelUp = false;
        HideLevelUpPopup();
    }

    private void HideLevelUpPopup()
    {
        // 다른 팝업 아래에서도 완료된 창은 닫습니다. UIManager는 다음 스택 접근 시 비활성 항목을 정리합니다.
        if (levelUpPopup != null) levelUpPopup.gameObject.SetActive(false);
    }

    private void CancelLevelUpAdvance()
    {
        if (advanceLevelUp != null) StopCoroutine(advanceLevelUp);
        advanceLevelUp = null;
        completionPending = false;
    }

    public bool CanClosePopupUI(UI_CombatPopup popup) => popup != levelUpPopup || !automaticLevelUp;

    private bool CanShow() => isActiveAndEnabled && (growthSource == null || !growthSource.IsDead)
        && (gameManager == null || gameManager.state == GameState.Playing);

    public void AcquirePause(UI_CombatPopup popup)
    {
        if (!isActiveAndEnabled || (popup != pausePopup && popup != levelUpPopup) || !activePopups.Add(popup))
            return;
        CancelResume();
        if (ownsPause)
        {
            Time.timeScale = 0f;
            return;
        }
        // Transfer a temporary parry stop back to its original scale before owning popup pause.
        foreach (Player candidate in FindObjectsByType<Player>())
            if (candidate.gameObject.scene == gameObject.scene) candidate.EndParryHitStop();
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        ownsPause = true;
        Time.timeScale = 0f;
    }

    public void ReleasePause(UI_CombatPopup popup)
    {
        if (!activePopups.Remove(popup)) return;
        if (activePopups.Count > 0)
        {
            if (activePopups.Contains(pausePopup)) pausePopup.Focus();
            else if (activePopups.Contains(levelUpPopup))
            {
                if (levelUpPanel != null) levelUpPanel.Focus();
                else levelUpPopup.Focus();
            }
            return;
        }
        bool fromLevelUp = popup == levelUpPopup;
        BeginResume(fromLevelUp);
        if (isActiveAndEnabled && pauseButton != null && pauseButton.gameObject.activeInHierarchy)
            pauseButton.Select();
    }

    private void BeginResume(bool fromLevelUp)
    {
        if (!ownsPause) return;
        CancelResume();
        if (!isActiveAndEnabled || (growthSource != null && growthSource.IsDead)
            || (gameManager != null && gameManager.state != GameState.Playing))
        {
            RestoreTimeScaleImmediate();
            return;
        }
        resumeRoutine = StartCoroutine(ResumeAfterPopup(fromLevelUp));
    }

    private IEnumerator ResumeAfterPopup(bool fromLevelUp)
    {
        if (fromLevelUp)
        {
            // 보상 선택 직후 인지 지연을 위해 월드 시간을 99% 줄입니다(1%만 진행).
            float target = previousTimeScale > 0f ? previousTimeScale : 1f;
            Time.timeScale = target * LevelUpSlowMotionScale;
            float elapsed = 0f;
            while (elapsed < LevelUpSlowMotionSeconds)
            {
                if (ShouldAbortResume())
                {
                    AbortResume();
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            // 일시정지·옵션·상태(인벤) 창을 닫은 뒤 0.5초간 정지 상태를 유지합니다.
            Time.timeScale = 0f;
            float elapsed = 0f;
            while (elapsed < ResumeHoldSeconds)
            {
                if (ShouldAbortResume())
                {
                    AbortResume();
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        resumeRoutine = null;
        RestoreTimeScaleImmediate();
    }

    private bool ShouldAbortResume() =>
        !isActiveAndEnabled || activePopups.Count > 0
        || (gameManager != null && gameManager.state != GameState.Playing)
        || (growthSource != null && growthSource.IsDead);

    private void AbortResume()
    {
        resumeRoutine = null;
        // 새 팝업이 다시 AcquirePause를 소유 중이면 시간을 되돌리지 않습니다.
        if (activePopups.Count == 0) RestoreTimeScaleImmediate();
    }

    private void CancelResume()
    {
        if (resumeRoutine == null) return;
        StopCoroutine(resumeRoutine);
        resumeRoutine = null;
    }

    private void RestoreTimeScaleImmediate()
    {
        if (!ownsPause) return;
        ownsPause = false;
        // 승패에 의한 정지나 외부에서 바꾼 시간 배율을 덮어쓰지 않습니다.
        if ((Time.timeScale == 0f || Time.timeScale <= LevelUpSlowMotionScale + 0.0001f)
            && (growthSource == null || !growthSource.IsDead)
            && (gameManager == null || gameManager.state == GameState.Playing))
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
    }

    private void OnDisable()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
        if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
        CloseOwnedPopups();
        if (growthSource != null)
        {
            growthSource.EXPChanged -= HandleExperienceChanged;
            growthSource.PlayerDeath -= HandleGrowthPlayerDeath;
        }
        growthSource = null;
    }

    private void HandleStateChanged(GameState state)
    {
        if (pauseButton != null) pauseButton.interactable = state == GameState.Playing;
        if (state != GameState.Playing)
        {
            if (growthSource != null) growthSource.CancelPendingLevelUps();
            CloseOwnedPopups();
        }
    }

    private void CloseOwnedPopups()
    {
        CancelLevelUpAdvance();
        CancelResume();
        automaticLevelUp = false;
        // OnDisable의 ReleasePause가 재개 코루틴을 다시 켜지 않도록 먼저 비웁니다.
        activePopups.Clear();
        if (pausePopup != null) pausePopup.gameObject.SetActive(false);
        if (levelUpPopup != null) levelUpPopup.gameObject.SetActive(false);
        RestoreTimeScaleImmediate();
    }

    private void OnDestroy()
    {
        // Registered popups may have been parented to a root in another additive scene.
        if (pausePopup != null) Destroy(pausePopup.gameObject);
        if (levelUpPopup != null) Destroy(levelUpPopup.gameObject);
    }

#if UNITY_EDITOR
    [ContextMenu("테스트/경험치 13 획득")]
    private void PreviewExperience()
    {
        if (!Application.isPlaying || !CanShow()) return;
        BindGrowthSource();
        growthSource?.AddEXP(13);
    }

    [ContextMenu("테스트/경험치 30 획득 (연속 레벨업)")]
    private void PreviewMultipleLevels()
    {
        if (!Application.isPlaying || !CanShow()) return;
        BindGrowthSource();
        growthSource?.AddEXP(30);
    }

    [ContextMenu("테스트/레벨업 팝업 열기")]
    private void PreviewLevelUp()
    {
        if (Application.isPlaying) ShowLevelUp();
    }

    [ContextMenu("테스트/원하는 카드 고르기")]
    private void OpenChosenSkillPicker()
    {
        CombatSkillCardPicker.Open(this);
    }

    /// <summary>테스트용. 지정한 스킬 한 장만 레벨업 카드로 열고, 고르면 기존 획득 경로로 등록합니다.</summary>
    public void ShowChosenSkillCard(int skillId)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("지정 카드는 Play 중에만 열 수 있습니다.", this);
            return;
        }
        if (!CanShow() || levelUpPopup == null)
        {
            Debug.LogWarning("지정 카드는 전투 진행 중에만 열 수 있습니다.", this);
            return;
        }
        if (automaticLevelUp || completionPending)
        {
            Debug.LogWarning("자동 레벨업이 진행 중이면 지정 카드를 열 수 없습니다. 레벨업을 마친 뒤 다시 고르세요.", this);
            return;
        }
        if (!CombatSkillTable.TryGet(skillId, out CombatSkillRecord skill) || skill == null)
        {
            Debug.LogError($"스킬 {skillId}를 전투 표에서 찾지 못했습니다.", this);
            return;
        }
        ResolveStatusPlayer();
        if (statusPlayer == null || statusPlayer.data == null)
        {
            Debug.LogError("레벨업 스킬을 등록할 Player/PlayerData가 없습니다.", this);
            return;
        }
        EnsureLevelUpPanel();
        if (levelUpPanel == null)
        {
            Debug.LogError("레벨업 카드 패널을 만들 수 없습니다.", this);
            return;
        }
        PlayerLoadout loadout = statusPlayer.data.Loadout;
        CombatSkillTableFile table = CombatSkillTable.CreateSnapshot();
        levelUpPanel.SetChoices(
            new List<LevelUpCardData> { CreateSkillCard(skill, table.rarities, loadout) },
            index =>
            {
                if (index != 0) return;
                ApplySkillChoice(loadout, skill);
            });
        OpenLevelUpPanel();
    }

    [ContextMenu("테스트/카드 소진 안내")]
    private void PreviewExhaustedCards()
    {
        if (!Application.isPlaying || !CanShow() || levelUpPopup == null || levelUpPopup.gameObject.activeSelf) return;
        EnsureLevelUpPanel();
        if (levelUpPanel == null) return;
        levelUpPanel.ShowMessage("카드 없음", "카드가 소진되었습니다.", CompleteLevelUp);
        OpenLevelUpPanel();
    }

    [ContextMenu("테스트/레벨업 선택 완료")]
    private void CompleteLevelUpPreview()
    {
        if (Application.isPlaying) CompleteLevelUp();
    }

    [ContextMenu("테스트/일시정지 팝업 열기")]
    private void PreviewPause()
    {
        if (Application.isPlaying) ShowPause();
    }
#endif
}
