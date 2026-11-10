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

    private readonly HashSet<UI_CombatPopup> activePopups = new HashSet<UI_CombatPopup>();
    private float previousTimeScale;
    private bool ownsPause;
    private PlayerData growthSource;
    private bool automaticLevelUp;
    private bool completionPending;
    private Coroutine advanceLevelUp;

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
        PlayerLoadout loadout = statusPlayer.data.Loadout;
        CombatSkillTableFile table = CombatSkillTable.CreateSnapshot();
        var attackExcluded = new HashSet<int>();
        var tacticalExcluded = new HashSet<int>();
        var upgradeable = new HashSet<int>();
        for (int i = 0; i < loadout.UnlockedAttackSlots; i++)
        {
            if (loadout.GetAttackSkill(i) is not Data.SkillData owned) continue;
            if (loadout.IsAttackSkillAtMaxStack(owned.DataId)) attackExcluded.Add(owned.DataId);
            else upgradeable.Add(owned.DataId);
        }
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
        bool canOfferAttack = loadout.HasFreeAttackSlot || upgradeable.Count > 0;
        bool canOfferTactical = loadout.HasFreeTacticalSlot
            && LevelUpSkillDraft.Draw(table, 1, 30000, 40000, () => 0d, tacticalExcluded).Count > 0;
        if (!canOfferAttack && !canOfferTactical)
        {
            ShowAttackSlotsFull();
            return true;
        }
        if (!loadout.HasFreeAttackSlot && table?.skills != null)
        {
            // 빈 공격 슬롯이 없으면 강화 가능한 보유 약공격만 남깁니다.
            foreach (CombatSkillRecord skill in table.skills)
            {
                if (skill == null || skill.Id < 10000 || skill.Id >= 20000) continue;
                if (!upgradeable.Contains(skill.Id)) attackExcluded.Add(skill.Id);
            }
        }
        if (!canOfferTactical && table?.skills != null)
        {
            foreach (CombatSkillRecord skill in table.skills)
                if (skill != null && skill.Id >= 30000 && skill.Id < 40000)
                    tacticalExcluded.Add(skill.Id);
        }
        // 임시 혼합 비율: 약공격 0.75 / 전략 0.25. 확정 밸런스가 아닙니다.
        var skills = DrawLevelUpMix(table, 3, canOfferAttack, canOfferTactical, attackExcluded, tacticalExcluded);
        if (skills.Count == 0)
        {
            levelUpPanel.ShowMessage("카드 없음", "카드가 소진되었습니다.", CompleteLevelUp);
            return true;
        }
        var choices = new List<LevelUpCardData>();
        foreach (var skill in skills) choices.Add(CreateSkillCard(skill, table.rarities, loadout));
        levelUpPanel.SetChoices(choices, index =>
        {
            // 열려 있는 동안 보유 상태가 바뀌어도 성공한 획득만 완료합니다.
            if (index < 0 || index >= skills.Count) return;
            ApplyLevelUpSelection(skills[index], loadout);
        });
        return true;
    }

    private void ApplyLevelUpSelection(CombatSkillRecord selected, PlayerLoadout loadout)
    {
        if (selected == null || loadout == null || !CanShow()) return;
        bool isTactical = selected.Id >= 30000 && selected.Id < 40000;
        bool alreadyOwned = isTactical ? OwnsTactical(loadout, selected.Id)
            : loadout.GetAttackSkillStack(selected.Id) > 0;
        if (LevelUpSkillReward.TryAcquire(loadout, selected)) CompleteLevelUp();
        else if (!alreadyOwned && isTactical && !loadout.HasFreeTacticalSlot) ShowAttackSlotsFull();
        else if (!alreadyOwned && !isTactical && !loadout.HasFreeAttackSlot) ShowAttackSlotsFull();
        else levelUpPanel.ShowMessage("스킬 획득 불가", "선택한 스킬을 등록하거나 강화할 수 없습니다.", CompleteLevelUp);
    }

    private List<CombatSkillRecord> DrawLevelUpMix(CombatSkillTableFile table, int count,
        bool canOfferAttack, bool canOfferTactical, HashSet<int> attackExcluded, HashSet<int> tacticalExcluded)
    {
        var result = new List<CombatSkillRecord>();
        var drawn = new HashSet<int>();
        while (result.Count < count)
        {
            bool preferTactical = canOfferTactical && (!canOfferAttack || levelUpRandom.NextDouble() < 0.25);
            List<CombatSkillRecord> pick = null;
            if (preferTactical)
                pick = LevelUpSkillDraft.Draw(table, 1, 30000, 40000, levelUpRandom.NextDouble, Union(tacticalExcluded, drawn));
            if (pick == null || pick.Count == 0)
                pick = LevelUpSkillDraft.Draw(table, 1, 10000, 20000, levelUpRandom.NextDouble, Union(attackExcluded, drawn));
            if ((pick == null || pick.Count == 0) && canOfferTactical)
                pick = LevelUpSkillDraft.Draw(table, 1, 30000, 40000, levelUpRandom.NextDouble, Union(tacticalExcluded, drawn));
            if (pick == null || pick.Count == 0) break;
            result.Add(pick[0]);
            drawn.Add(pick[0].Id);
        }
        return result;
    }

    private static HashSet<int> Union(HashSet<int> left, HashSet<int> right)
    {
        var set = new HashSet<int>(left);
        foreach (int id in right) set.Add(id);
        return set;
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
        bool isUpgrade = currentStack > 0 && currentStack < PlayerLoadout.MaxAttackSkillStack;
        if (isUpgrade)
        {
            int nextStack = currentStack + 1;
            float multiplier = PlayerLoadout.DamageMultiplierForStack(nextStack);
            description += $"\n강화 {currentStack}→{nextStack} (피해 ×{multiplier:0.#})";
        }
        return new LevelUpCardData
        {
            SkillId = skill.Id, Name = skill.Name, Rarity = rarity != null ? rarity.Id : id, Accent = accent,
            Icon = string.IsNullOrEmpty(skill.Icon) ? null : Managers.Resource.Load<Sprite>(skill.Icon),
            ActiveDescription = description, ActiveButtonLabel = "N",
            FlavorText = skill.Explanation, IsUpgrade = isUpgrade
        };
    }

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
        if (ownsPause) return;
        // Transfer a temporary parry stop back to its original scale before owning popup pause.
        foreach (Player candidate in FindObjectsByType<Player>())
            if (candidate.gameObject.scene == gameObject.scene) candidate.EndParryHitStop();
        previousTimeScale = Time.timeScale;
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
        RestoreTimeScale();
        if (isActiveAndEnabled && pauseButton != null && pauseButton.gameObject.activeInHierarchy)
            pauseButton.Select();
    }

    private void RestoreTimeScale()
    {
        if (!ownsPause) return;
        ownsPause = false;
        // 승패에 의한 정지나 외부에서 바꾼 시간 배율을 덮어쓰지 않습니다.
        if (Time.timeScale == 0f && (growthSource == null || !growthSource.IsDead)
            && (gameManager == null || gameManager.state == GameState.Playing))
            Time.timeScale = previousTimeScale;
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
        automaticLevelUp = false;
        if (pausePopup != null) pausePopup.gameObject.SetActive(false);
        if (levelUpPopup != null) levelUpPopup.gameObject.SetActive(false);
        activePopups.Clear();
        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        // Registered popups may have been parented to a root in another additive scene.
        if (pausePopup != null) Destroy(pausePopup.gameObject);
        if (levelUpPopup != null) Destroy(levelUpPopup.gameObject);
    }

#if UNITY_EDITOR
    [ContextMenu("테스트/SoulArena3D 로드")]
    private void LoadSoulArenaDebug()
    {
        if (!Application.isPlaying) return;
        try
        {
            Managers.Scene.LoadScene(Define.EScene.SoulArena3D);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SoulArena3D 로드 실패: {ex.Message}", this);
        }
    }

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
                ApplyLevelUpSelection(skill, loadout);
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
