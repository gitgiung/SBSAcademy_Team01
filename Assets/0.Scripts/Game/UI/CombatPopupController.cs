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
            HandleStateChanged(gameManager.state);
        }
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
        if (!levelUpPopup.gameObject.activeSelf)
        {
            ResolveStatusPlayer();
            if (statusPlayer == null || statusPlayer.data == null)
            {
                Debug.LogError("레벨업 스킬을 등록할 Player/PlayerData가 없습니다.", this);
                return null;
            }
            PlayerLoadout loadout = statusPlayer.data.Loadout;
            if (!loadout.HasFreeAttackSlot)
            {
                ShowAttackSlotsFull();
                return OpenLevelUpPanel();
            }
            var excluded = new HashSet<int>();
            for (int i = 0; i < loadout.UnlockedAttackSlots; i++)
                if (loadout.GetAttackSkill(i) is Data.SkillData owned) excluded.Add(owned.DataId);
            CombatSkillTableFile table = CombatSkillTable.CreateSnapshot();
            var skills = LevelUpSkillDraft.Draw(table, 3, 10000, 20000, levelUpRandom.NextDouble, excluded);
            if (skills.Count == 0)
            {
                levelUpPanel.ShowMessage("카드 없음", "카드가 소진되었습니다.", CompleteLevelUp);
                return OpenLevelUpPanel();
            }
            var choices = new List<LevelUpCardData>();
            foreach (var skill in skills) choices.Add(CreateSkillCard(skill, table.rarities));
            levelUpPanel.SetChoices(choices, index =>
            {
                // 열려 있는 동안 보유 상태가 바뀌어도 성공한 획득만 완료합니다.
                if (index < 0 || index >= skills.Count || !CanShow()) return;
                if (LevelUpSkillReward.TryAcquire(loadout, skills[index])) CompleteLevelUp();
                else if (!loadout.HasFreeAttackSlot) ShowAttackSlotsFull();
                else levelUpPanel.ShowMessage("스킬 획득 불가", "선택한 스킬을 등록할 수 없습니다.", CompleteLevelUp);
            });
        }
        return OpenLevelUpPanel();
    }

    private void ResolveStatusPlayer()
    {
        if (statusPlayer != null) return;
        foreach (Player candidate in FindObjectsByType<Player>())
            if (candidate.gameObject.scene == gameObject.scene) { statusPlayer = candidate; break; }
    }

    private void ShowAttackSlotsFull()
    {
        levelUpPanel.ShowMessage("스킬 슬롯 가득 참", "공격 스킬 슬롯이 가득 찼습니다.", CompleteLevelUp);
    }

    private UI_CombatPopup OpenLevelUpPanel()
    {
        UI_CombatPopup popup = Managers.UI.ShowPopupUI<UI_CombatPopup>(LevelUpKey);
        if (popup != null) levelUpPanel?.Focus();
        return popup;
    }

    private static LevelUpCardData CreateSkillCard(CombatSkillRecord skill, List<SkillRarityRecord> rarities)
    {
        string id = SkillRarityRecord.NormalizeId(skill.Rarity);
        SkillRarityRecord rarity = rarities.Find(value => value != null
            && string.Equals(SkillRarityRecord.NormalizeId(value.Id), id, System.StringComparison.OrdinalIgnoreCase));
        Color accent = Color.white;
        if (rarity != null && ColorUtility.TryParseHtmlString(rarity.ColorHex, out Color color)) accent = color;
        return new LevelUpCardData
        {
            SkillId = skill.Id, Name = skill.Name, Rarity = rarity != null ? rarity.Id : id, Accent = accent,
            Icon = string.IsNullOrEmpty(skill.Icon) ? null : Managers.Resource.Load<Sprite>(skill.Icon),
            ActiveDescription = LevelUpSkillDraft.FormatDescription(skill), ActiveButtonLabel = "N",
            FlavorText = skill.Explanation
        };
    }

    public UI_CombatPopup ShowLevelUp(IReadOnlyList<LevelUpCardData> choices, System.Action<int> onSelected)
    {
        if (!CanShow() || levelUpPopup == null) return null;
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
        if (levelUpPopup != null)
            levelUpPopup.ClosePopupUI();
    }

    private bool CanShow() => isActiveAndEnabled && (gameManager == null || gameManager.state == GameState.Playing);

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
        if (Time.timeScale == 0f && (gameManager == null || gameManager.state == GameState.Playing))
            Time.timeScale = previousTimeScale;
    }

    private void OnDisable()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
        if (gameManager != null) gameManager.StateChanged -= HandleStateChanged;
        CloseOwnedPopups();
    }

    private void HandleStateChanged(GameState state)
    {
        if (pauseButton != null) pauseButton.interactable = state == GameState.Playing;
        if (state != GameState.Playing) CloseOwnedPopups();
    }

    private void CloseOwnedPopups()
    {
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
    [ContextMenu("테스트/레벨업 팝업 열기")]
    private void PreviewLevelUp()
    {
        if (Application.isPlaying) ShowLevelUp();
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
