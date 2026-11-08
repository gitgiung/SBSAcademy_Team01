using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerSkillButton { Light, Heavy, Tactical }

[Serializable]
public sealed class PlayerOwnedItem
{
    public int DataId;
    public string Name;
    public Sprite Icon;
}

/// <summary>한 플레이어의 보유/장착 상태. 획득 효과나 스킬 실행은 소유하지 않습니다.</summary>
[Serializable]
public sealed class PlayerLoadout
{
    public const int AttackCapacity = 8;
    public const int TacticalCapacity = 5;
    public const int ItemCapacity = 60;
    /// <summary>같은 스킬을 다시 고를 수 있는 최대 횟수입니다. 1=기본, 2=1.2배, 3=1.5배.</summary>
    public const int MaxAttackSkillStack = 3;
    [SerializeField, Range(6, 8)] private int unlockedAttackSlots = 6;
    [SerializeField] private Data.SkillData[] attackSkills = new Data.SkillData[AttackCapacity];
    [SerializeField] private int[] attackSkillStacks = new int[AttackCapacity];
    [SerializeField] private Data.SkillData[] tacticalSkills = new Data.SkillData[TacticalCapacity];
    [SerializeField] private List<PlayerOwnedItem> relics = new List<PlayerOwnedItem>();
    [SerializeField] private List<PlayerOwnedItem> augments = new List<PlayerOwnedItem>();
    [SerializeField] private int lightSkillId;
    [SerializeField] private int heavySkillId;
    [SerializeField] private int tacticalSkillId;
    public event Action Changed;

    public int UnlockedAttackSlots => Mathf.Clamp(unlockedAttackSlots, 6, AttackCapacity);
    public IReadOnlyList<PlayerOwnedItem> Relics => relics.AsReadOnly();
    public IReadOnlyList<PlayerOwnedItem> Augments => augments.AsReadOnly();
    public bool HasFreeAttackSlot
    {
        get
        {
            for (int i = 0; i < UnlockedAttackSlots; i++) if (GetAttackSkill(i) == null) return true;
            return false;
        }
    }

    private void EnsureSlots()
    {
        if (attackSkills == null || attackSkills.Length != AttackCapacity) Array.Resize(ref attackSkills, AttackCapacity);
        if (attackSkillStacks == null || attackSkillStacks.Length != AttackCapacity) Array.Resize(ref attackSkillStacks, AttackCapacity);
        if (tacticalSkills == null || tacticalSkills.Length != TacticalCapacity) Array.Resize(ref tacticalSkills, TacticalCapacity);
        for (int i = 0; i < AttackCapacity; i++)
        {
            if (Valid(attackSkills[i]) && attackSkillStacks[i] < 1) attackSkillStacks[i] = 1;
            if (!Valid(attackSkills[i])) attackSkillStacks[i] = 0;
        }
    }

    private static bool Valid(Data.SkillData skill) => skill != null && skill.DataId > 0;

    /// <summary>스택 1=1배, 2=1.2배, 3=1.5배. 범위를 벗어나면 가장 가까운 단계로 맞춥니다.</summary>
    public static float DamageMultiplierForStack(int stack)
    {
        if (stack <= 1) return 1f;
        if (stack == 2) return 1.2f;
        return 1.5f;
    }

    public Data.SkillData GetAttackSkill(int index)
    {
        EnsureSlots();
        return index >= 0 && index < UnlockedAttackSlots && Valid(attackSkills[index]) ? attackSkills[index] : null;
    }
    public Data.SkillData GetTacticalSkill(int index)
    {
        EnsureSlots();
        return index >= 0 && index < TacticalCapacity && Valid(tacticalSkills[index]) ? tacticalSkills[index] : null;
    }

    /// <summary>보유하지 않으면 0, 보유하면 1~MaxAttackSkillStack.</summary>
    public int GetAttackSkillStack(int skillId)
    {
        EnsureSlots();
        if (skillId <= 0) return 0;
        for (int i = 0; i < UnlockedAttackSlots; i++)
            if (GetAttackSkill(i)?.DataId == skillId) return Mathf.Clamp(attackSkillStacks[i], 1, MaxAttackSkillStack);
        return 0;
    }

    public float GetAttackSkillDamageMultiplier(int skillId)
        => DamageMultiplierForStack(GetAttackSkillStack(skillId));

    public bool IsAttackSkillAtMaxStack(int skillId)
        => GetAttackSkillStack(skillId) >= MaxAttackSkillStack;

    private bool Owns(int id)
    {
        for (int i = 0; i < UnlockedAttackSlots; i++) if (GetAttackSkill(i)?.DataId == id) return true;
        for (int i = 0; i < TacticalCapacity; i++) if (GetTacticalSkill(i)?.DataId == id) return true;
        return false;
    }

    /// <summary>새 스킬만 빈 슬롯에 넣습니다. 이미 보유 중이면 false입니다.</summary>
    public bool TryAddAttackSkill(Data.SkillData skill)
    {
        if (!Valid(skill) || skill.DataId < 10000 || skill.DataId >= 30000 || Owns(skill.DataId)) return false;
        EnsureSlots();
        for (int i = 0; i < UnlockedAttackSlots; i++)
            if (GetAttackSkill(i) == null)
            {
                attackSkills[i] = skill;
                attackSkillStacks[i] = 1;
                Changed?.Invoke();
                return true;
            }
        return false;
    }

    /// <summary>미보유면 신규 등록, 보유 중이면 슬롯을 쓰지 않고 스택만 올립니다. 최대 스택이면 false.</summary>
    public bool TryAddOrUpgradeAttackSkill(Data.SkillData skill)
    {
        if (!Valid(skill) || skill.DataId < 10000 || skill.DataId >= 30000) return false;
        EnsureSlots();
        for (int i = 0; i < UnlockedAttackSlots; i++)
        {
            if (GetAttackSkill(i)?.DataId != skill.DataId) continue;
            if (attackSkillStacks[i] >= MaxAttackSkillStack) return false;
            attackSkillStacks[i]++;
            Changed?.Invoke();
            return true;
        }
        return TryAddAttackSkill(skill);
    }
    public bool TryAddTacticalSkill(Data.SkillData skill)
    {
        if (!Valid(skill) || skill.DataId < 30000 || skill.DataId >= 40000 || Owns(skill.DataId)) return false;
        for (int i = 0; i < TacticalCapacity; i++)
            if (GetTacticalSkill(i) == null) { tacticalSkills[i] = skill; Changed?.Invoke(); return true; }
        return false;
    }
    public bool UnlockAttackSlot()
    {
        if (UnlockedAttackSlots >= AttackCapacity) return false;
        unlockedAttackSlots = UnlockedAttackSlots + 1;
        Changed?.Invoke();
        return true;
    }
    public bool TryEquip(PlayerSkillButton button, int id)
    {
        if (!Owns(id)) return false;
        switch (button)
        {
            case PlayerSkillButton.Light: if (id < 10000 || id >= 20000) return false; lightSkillId = id; break;
            case PlayerSkillButton.Heavy: if (id < 20000 || id >= 30000) return false; heavySkillId = id; break;
            case PlayerSkillButton.Tactical: if (id < 30000 || id >= 40000) return false; tacticalSkillId = id; break;
            default: return false;
        }
        Changed?.Invoke();
        return true;
    }
    public int GetEquippedSkillId(PlayerSkillButton button)
    {
        int id = button == PlayerSkillButton.Light ? lightSkillId
            : button == PlayerSkillButton.Heavy ? heavySkillId : button == PlayerSkillButton.Tactical ? tacticalSkillId : 0;
        return Owns(id) ? id : 0;
    }
    /// <summary>보유 스킬을 제거하지 않고 해당 버튼의 장착만 해제합니다.</summary>
    public bool TryUnequip(PlayerSkillButton button)
    {
        switch (button)
        {
            case PlayerSkillButton.Light: if (lightSkillId == 0) return false; lightSkillId = 0; break;
            case PlayerSkillButton.Heavy: if (heavySkillId == 0) return false; heavySkillId = 0; break;
            case PlayerSkillButton.Tactical: if (tacticalSkillId == 0) return false; tacticalSkillId = 0; break;
            default: return false;
        }
        Changed?.Invoke();
        return true;
    }
    public bool RemoveSkill(int id)
    {
        EnsureSlots();
        bool removed = false;
        for (int i = 0; i < AttackCapacity; i++)
            if (attackSkills[i]?.DataId == id && id > 0)
            {
                attackSkills[i] = null;
                attackSkillStacks[i] = 0;
                removed = true;
            }
        for (int i = 0; i < TacticalCapacity; i++)
            if (tacticalSkills[i]?.DataId == id && id > 0) { tacticalSkills[i] = null; removed = true; }
        if (!removed) return false;
        if (lightSkillId == id) lightSkillId = 0;
        if (heavySkillId == id) heavySkillId = 0;
        if (tacticalSkillId == id) tacticalSkillId = 0;
        Changed?.Invoke();
        return true;
    }
    public bool TryAddRelic(PlayerOwnedItem item) => AddItem(relics, item);
    public bool TryAddAugment(PlayerOwnedItem item) => AddItem(augments, item);
    private bool AddItem(List<PlayerOwnedItem> list, PlayerOwnedItem item)
    {
        if (item == null || item.DataId <= 0 || list.Count >= ItemCapacity || list.Exists(x => x.DataId == item.DataId)) return false;
        list.Add(item);
        Changed?.Invoke();
        return true;
    }
}
