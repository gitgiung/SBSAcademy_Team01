/// <summary>레벨업 카드 한 장의 공통 계약. 적용 부작용은 소유하지 않습니다.</summary>
public interface ILevelUpReward
{
    int Id { get; }
    string Name { get; }
    string Rarity { get; }
    LevelUpRewardKind Kind { get; }
}

public enum LevelUpRewardKind
{
    Skill = 0,
    Stat = 1
}
