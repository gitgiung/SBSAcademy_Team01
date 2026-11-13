using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StatusPanelUI;

/// <summary>기존 일시정지 팝업 내부의 상태/약공격 장착/음량 옵션. 전투 정지와 팝업 스택은 소유하지 않습니다.</summary>
public sealed class PlayerStatusPanel : MonoBehaviour
{
    private PlayerData source;
    private PlayerLoadout loadout;
    private bool subscribed;
    private GameObject mainRoot;
    private GameObject optionsRoot;
    private Button backButton;
    private RectTransform contentRoot;
    private readonly List<TMP_Text> statValues = new List<TMP_Text>();
    private TMP_Text levelValue;
    private readonly List<Cell> attacks = new List<Cell>();
    private readonly List<Cell> tacticals = new List<Cell>();
    private ItemRow relicRow;
    private ItemRow augmentRow;
    private Slider master;
    private Slider bgm;
    private Slider effect;
    private TMP_Text masterValue;
    private TMP_Text bgmValue;
    private TMP_Text effectValue;
    private MobileControlLayout controlLayout;
    private Button swapControlsButton;
    private TMP_Text swapControlsLabel;
    public int AttackSlotCount => attacks.Count;
    public int TacticalSlotCount => tacticals.Count;
    public bool OptionsVisible => optionsRoot != null && optionsRoot.activeSelf;

    public void Initialize(RectTransform content, Button back, PlayerData data, TMP_FontAsset font)
    {
        Unsubscribe();
        source = data;
        loadout = data != null ? data.Loadout : null;
        if (mainRoot == null) Build(content, back, font);
        Subscribe();
        HideOptions();
        Canvas.ForceUpdateCanvases();
        Refresh();
    }

    private void Build(RectTransform content, Button back, TMP_FontAsset font)
    {
        contentRoot = content;
        backButton = back;
        var panel = (RectTransform)content.parent;
        panel.anchorMin = new Vector2(.075f, .12f);
        panel.anchorMax = new Vector2(.94f, .86f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        var panelImage = panel.GetComponent<Image>();
        if (panelImage == null) panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.sprite = Rounded;
        panelImage.type = UnityEngine.UI.Image.Type.Sliced;
        panelImage.color = Navy;
        // 기존 일시정지 제목/안내만 교체하고 뒤로가기 Button/리스너는 보존합니다.
        foreach (Transform child in panel)
            if (child != content && child != back.transform) child.gameObject.SetActive(false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = content.offsetMax = Vector2.zero;
        mainRoot = Stretch(content, "StatusContent").gameObject;
        var stats = Rect(mainRoot.transform, "Stats", 10, 20, 145, 460);
        Image(stats, new Color32(249, 233, 197, 255));
        Text(Rect(stats, "Heading", 80, 10, 400, 35), "스탯", font, 30, Color.black, TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;
        levelValue = Text(Rect(stats, "PlayerLevel", 490, 10, 450, 35), "", font, 30, Color.black, TextAlignmentOptions.Right);
        levelValue.textWrappingMode = TextWrappingModes.NoWrap;
        string[] labels = { "체력", "체력 재생", "피해량", "이동속도", "방어력", "MP", "MP 회복", "재사용 대기시간",
            "속성 피해량(전체)", "투사체 개수", "투사체 크기", "방어막 체력", "기술 범위", "경험치 획득", "획득 반경" };
        for (int i = 0; i < labels.Length; i++)
        {
            float y = 51 + i * 29f;
            Text(Rect(stats, "StatLabel" + i, 65, y, 625, 24), labels[i], font, 20, Color.black, TextAlignmentOptions.Left);
            var valueLabel = Text(Rect(stats, "StatValue" + i, 685, y, 255, 24), "—", font, 19, Color.black, TextAlignmentOptions.Right);
            valueLabel.textWrappingMode = TextWrappingModes.NoWrap;
            statValues.Add(valueLabel);
            Image(Rect(stats, "Divider" + i, 65, y + 26, 875, .6f), new Color(0.3f, .25f, .2f, .25f));
        }
        Header(mainRoot.transform, "공격 스킬", 175, 20, 155, new Color32(250, 147, 204, 255), font);
        Header(mainRoot.transform, "유물", 175, 175, 100, new Color32(248, 197, 173, 255), font);
        Header(mainRoot.transform, "증강", 175, 333, 100, new Color32(182, 227, 154, 255), font);
        Header(mainRoot.transform, "전략 스킬", 790, 140, 170, new Color32(195, 234, 248, 255), font);
        for (int i = 0; i < 8; i++)
        {
            int index = i;
            var cell = new Cell(Rect(mainRoot.transform, "AttackSlot" + i, 175 + i * 70, 80, 60, 60), font, true);
            cell.SetAction(() => ToggleAttackSkill(index));
            attacks.Add(cell);
        }
        Text(Rect(mainRoot.transform, "AttackEquipHint", 175, 143, 560, 26),
            "공격 스킬 클릭: 장착 / 다시 클릭: 해제", font, 17, Color.white, TextAlignmentOptions.Left);
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            var cell = new Cell(Rect(mainRoot.transform, "TacticalSlot" + i, 790 + i % 3 * 72, 200 + i / 3 * 75, 60, 60), font, true);
            cell.SetAction(() => ToggleTacticalSkill(index));
            tacticals.Add(cell);
        }
        Text(Rect(mainRoot.transform, "TacticalEquipHint", 790, 350, 200, 40),
            "전략 클릭: Skill 버튼 장착", font, 15, Color.white, TextAlignmentOptions.Left);
        relicRow = new ItemRow(mainRoot.transform, "Relics", 230, font);
        augmentRow = new ItemRow(mainRoot.transform, "Augments", 390, font);
        var gear = Button(Rect(mainRoot.transform, "OptionsButton", 936, 16, 43, 43), "설정", font,
            new Color32(221, 228, 231, 255), ShowOptions);
        gear.GetComponentInChildren<TMP_Text>().text = "";
        for (int i = 0; i < 4; i++)
        {
            var tooth = Rect(gear.transform, "GearTeeth" + i, 390, 35, 220, 430);
            tooth.localRotation = Quaternion.Euler(0, 0, i * 45);
            Image(tooth, Color.white);
        }
        Image(Rect(gear.transform, "GearCenter", 200, 100, 600, 300), Color.white);
        Image(Rect(gear.transform, "GearHole", 365, 182.5f, 270, 135), new Color32(137, 151, 160, 255));
        // 뒤로가기는 원래 팝업의 닫기 경로를 그대로 사용합니다.
        var backRect = (RectTransform)back.transform;
        backRect.anchorMin = new Vector2(.845f, .04f);
        backRect.anchorMax = new Vector2(.975f, .11f);
        backRect.offsetMin = backRect.offsetMax = Vector2.zero;
        var backImage = back.GetComponent<Image>();
        if (backImage == null) backImage = back.gameObject.AddComponent<Image>();
        backImage.sprite = Rounded;
        backImage.type = UnityEngine.UI.Image.Type.Sliced;
        backImage.color = new Color32(255, 116, 132, 255);
        back.targetGraphic = backImage;
        var backText = back.GetComponentInChildren<TMP_Text>();
        if (backText == null) backText = Text(Stretch(backRect, "Label"), "뒤로가기", font, 29);
        backText.color = Ink;
        back.transform.SetAsLastSibling();
        BuildOptions(content, font);
    }

    private static void Header(Transform parent, string title, float x, float y, float width, Color color, TMP_FontAsset font)
    {
        var rect = Rect(parent, title, x, y, width, 36);
        Image(rect, color);
        Text(Stretch(rect, "Heading"), title, font, 32).fontStyle = FontStyles.Bold;
    }

    private void BuildOptions(Transform parent, TMP_FontAsset font)
    {
        optionsRoot = Stretch(parent, "AudioOptions").gameObject;
        Image((RectTransform)optionsRoot.transform, Navy, true);
        Text(Rect(optionsRoot.transform, "Title", 200, 35, 600, 55), "설정", font, 44, Color.white);
        master = VolumeRow("MasterVolume", "전체 음량", 125, font, out masterValue);
        bgm = VolumeRow("BgmVolume", "배경 음악", 205, font, out bgmValue);
        effect = VolumeRow("EffectVolume", "효과음", 285, font, out effectValue);
        master.onValueChanged.AddListener(value => { Managers.Sound.MasterVolume = value; masterValue.text = Percent(value); });
        bgm.onValueChanged.AddListener(value => { Managers.Sound.SetVolume(Define.ESound.Bgm, value); bgmValue.text = Percent(value); });
        effect.onValueChanged.AddListener(value => { Managers.Sound.SetVolume(Define.ESound.Effect, value); effectValue.text = Percent(value); });
        Button(Rect(optionsRoot.transform, "Screenshot", 30, 435, 200, 40), "스크린샷", font,
            new Color32(113, 247, 255, 255), GameScreenshot.Capture);
        Text(Rect(optionsRoot.transform, "ControlLayoutLabel", 145, 365, 185, 45), "조작 위치", font, 30, Color.white, TextAlignmentOptions.Left);
        swapControlsButton = Button(Rect(optionsRoot.transform, "SwapControls", 340, 365, 480, 45),
            "", font, new Color32(55, 232, 184, 255), SwapControls);
        swapControlsLabel = swapControlsButton.GetComponentInChildren<TMP_Text>();
        swapControlsLabel.fontSize = 24;
        Button(Rect(optionsRoot.transform, "BackToStatus", 810, 435, 155, 40), "뒤로가기", font,
            new Color32(255, 116, 132, 255), HideOptions);
        optionsRoot.SetActive(false);
    }

    private Slider VolumeRow(string name, string title, float y, TMP_FontAsset font, out TMP_Text valueLabel)
    {
        Text(Rect(optionsRoot.transform, name + "Label", 145, y, 185, 45), title, font, 30, Color.white, TextAlignmentOptions.Left);
        var rect = Rect(optionsRoot.transform, name, 340, y, 400, 45);
        var slider = rect.gameObject.AddComponent<Slider>();
        var track = Stretch(rect, "Track");
        track.anchorMin = new Vector2(0, .35f); track.anchorMax = new Vector2(1, .65f);
        Image(track, new Color32(74, 82, 103, 255), true);
        var fill = Image(Stretch(track, "Fill"), new Color32(55, 232, 184, 255));
        slider.fillRect = fill.rectTransform;
        var area = Stretch(rect, "HandleArea");
        area.offsetMin = new Vector2(8, 0); area.offsetMax = new Vector2(-8, 0);
        var handle = Image(Stretch(area, "Handle"), Color.white, true);
        handle.rectTransform.anchorMin = Vector2.zero;
        handle.rectTransform.anchorMax = new Vector2(0, 1);
        handle.rectTransform.sizeDelta = new Vector2(22, 0);
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0; slider.maxValue = 1;
        valueLabel = Text(Rect(optionsRoot.transform, name + "Value", 775, y, 100, 45), "", font, 30, Color.white);
        return slider;
    }

    private static string Percent(float value) => Mathf.RoundToInt(value * 100) + "%";
    private void RefreshControlLayout()
    {
        if (controlLayout == null)
            foreach (var candidate in FindObjectsByType<MobileControlLayout>(FindObjectsInactive.Include))
                if (candidate.gameObject.scene == gameObject.scene) { controlLayout = candidate; break; }
        swapControlsButton.interactable = controlLayout != null && controlLayout.isActiveAndEnabled;
        swapControlsLabel.text = controlLayout != null && controlLayout.ControlsSwapped
            ? "이동 왼쪽 · 버튼 오른쪽" : "버튼 왼쪽 · 이동 오른쪽";
    }

    private void SwapControls()
    {
        if (controlLayout == null) return;
        controlLayout.SetControlsSwapped(!controlLayout.ControlsSwapped);
        RefreshControlLayout();
    }

    public void ShowOptions()
    {
        if (optionsRoot == null) return;
        var sound = Managers.Sound;
        master.SetValueWithoutNotify(sound.MasterVolume); masterValue.text = Percent(master.value);
        bgm.SetValueWithoutNotify(sound.GetVolume(Define.ESound.Bgm)); bgmValue.text = Percent(bgm.value);
        effect.SetValueWithoutNotify(sound.GetVolume(Define.ESound.Effect)); effectValue.text = Percent(effect.value);
        RefreshControlLayout();
        mainRoot.SetActive(false);
        backButton.gameObject.SetActive(false);
        optionsRoot.SetActive(true);
        master.Select();
    }
    public void HideOptions()
    {
        if (optionsRoot == null) return;
        optionsRoot.SetActive(false);
        mainRoot.SetActive(true);
        backButton.gameObject.SetActive(true);
        backButton.Select();
    }

    public void Refresh()
    {
        if (mainRoot == null) return;
        RefreshStats();
        for (int i = 0; i < attacks.Count; i++)
        {
            var skill = loadout?.GetAttackSkill(i);
            bool canEquip = TryGetAttackButton(skill, out PlayerSkillButton button);
            bool equipped = canEquip && loadout.GetEquippedSkillId(button) == skill.DataId;
            int level = skill != null ? loadout.GetAttackSkillStack(skill.DataId) : 0;
            ShowSkill(attacks[i], skill, level, i >= (loadout?.UnlockedAttackSlots ?? 6), equipped);
            attacks[i].SetInteractable(canEquip);
        }
        for (int i = 0; i < tacticals.Count; i++)
        {
            var skill = loadout?.GetTacticalSkill(i);
            bool equipped = skill != null && loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) == skill.DataId;
            ShowSkill(tacticals[i], skill, skill != null ? 1 : 0, false, equipped);
            tacticals[i].SetInteractable(skill != null && skill.DataId >= 30000 && skill.DataId < 40000);
        }
        relicRow.Refresh(loadout?.Relics);
        augmentRow.Refresh(loadout?.Augments);
    }

    private static void ShowSkill(Cell cell, Data.SkillData skill, int level, bool locked, bool equipped)
    {
        Color? rarityColor = null;
        if (skill != null && CombatSkillTable.TryGet(skill.DataId, out CombatSkillRecord record)
            && CombatSkillTable.TryGetRarity(record.GetRarityId(), out SkillRarityRecord rarity)
            && !string.IsNullOrEmpty(rarity.ColorHex)
            && ColorUtility.TryParseHtmlString(rarity.ColorHex, out Color color))
            rarityColor = color;
        cell.Show(skill?.Name, SkillIcon(skill), locked, equipped, level, rarityColor);
    }
    private void ToggleAttackSkill(int index)
    {
        var skill = loadout?.GetAttackSkill(index);
        if (!TryGetAttackButton(skill, out PlayerSkillButton button)) return;
        if (loadout.GetEquippedSkillId(button) == skill.DataId)
            loadout.TryUnequip(button);
        else
            loadout.TryEquip(button, skill.DataId);
    }
    private static bool TryGetAttackButton(Data.SkillData skill, out PlayerSkillButton button)
    {
        button = PlayerSkillButton.Light;
        if (skill == null || skill.DataId < 10000 || skill.DataId >= 30000) return false;
        if (skill.DataId < 20000) return true;
        button = PlayerSkillButton.Heavy;
        return CombatSkillTable.TryGet(skill.DataId, out CombatSkillRecord record)
            && LightSkillCatalog.TryCreateHeavy(record, out _);
    }
    private void ToggleTacticalSkill(int index)
    {
        var skill = loadout?.GetTacticalSkill(index);
        if (skill == null || skill.DataId < 30000 || skill.DataId >= 40000) return;
        if (loadout.GetEquippedSkillId(PlayerSkillButton.Tactical) == skill.DataId)
            loadout.TryUnequip(PlayerSkillButton.Tactical);
        else
            loadout.TryEquip(PlayerSkillButton.Tactical, skill.DataId);
    }
    private static Sprite SkillIcon(Data.SkillData skill) => skill != null
        ? SkillCardIcons.Get(skill.DataId, skill.IconLabel) : null;
    private void RefreshStats()
    {
        if (levelValue != null) levelValue.text = source != null ? $"Lv.{source.CurrentLevel}" : "Lv.—";
        foreach (var label in statValues) label.text = "—";
        if (source == null) return;
        statValues[0].text = $"{source.currentHealth:0.#} / {source.maxHealth:0.#}";
        var stats = source.StatCards;
        statValues[1].text = $"{stats.Value(PlayerStat.HP_Regen):P1}/초";
        statValues[2].text = $"×{source.DamageMultiplier:0.##}";
        statValues[3].text = $"{source.MoveSpeed:0.##}";
        statValues[4].text = $"{stats.Value(PlayerStat.DEF):0.##}";
        statValues[5].text = $"{source.CurrentMP:0.#} / {source.maxMP:0.#}";
        statValues[6].text = $"{source.MPRecovery:0.##}/초";
        statValues[7].text = $"{stats.Value(PlayerStat.Cooldown):P0}";
        statValues[8].text = $"{stats.Value(PlayerStat.Elm_All_ATK):P0}";
        statValues[9].text = $"+{stats.Value(PlayerStat.Projectile_Count):0}";
        statValues[10].text = $"{stats.Value(PlayerStat.Projectile_Size):P0}";
        statValues[11].text = source.HasShield ? $"{source.CurrentShield:0.#} / {source.MaxShield:0.#}" : "0";
        statValues[12].text = $"{stats.Value(PlayerStat.Skill_Range):P0}";
        statValues[13].text = $"×{stats.Value(PlayerStat.XP_Gain, 1f):0.##}";
        statValues[14].text = $"{source.PickupDistance:0.##}";
    }
    private void OnResourceChanged(float current, float maximum) => RefreshStats();
    private void Subscribe()
    {
        if (subscribed || source == null) return;
        source.HealthChanged += OnResourceChanged;
        source.MPChanged += OnResourceChanged;
        source.EXPChanged += OnResourceChanged;
        source.ShieldChanged += OnResourceChanged;
        loadout.Changed += Refresh;
        subscribed = true;
    }
    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (source != null)
        {
            source.HealthChanged -= OnResourceChanged;
            source.MPChanged -= OnResourceChanged;
            source.EXPChanged -= OnResourceChanged;
            source.ShieldChanged -= OnResourceChanged;
        }
        if (loadout != null) loadout.Changed -= Refresh;
        subscribed = false;
    }
    private void OnEnable() { Subscribe(); if (mainRoot != null) Canvas.ForceUpdateCanvases(); Refresh(); }
    private void OnDisable() { Unsubscribe(); }
    private void OnDestroy() { Unsubscribe(); }
    private void LateUpdate()
    {
        if (contentRoot == null) return;
        relicRow?.Layout();
        augmentRow?.Layout();
    }
}
