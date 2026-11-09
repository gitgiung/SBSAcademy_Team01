/// <summary>보상 적용 포트. UI·전투는 이 계약에만 의존하고, 연결 전에는 구현을 호출하지 않습니다.</summary>
public interface ILevelUpRewardApplier
{
    bool TryApply(ILevelUpReward reward, ILevelUpRewardApplyContext context);
}

/// <summary>적용에 필요한 플레이어/로드아웃 핸들. 연결 시 구체 타입을 둡니다.</summary>
public interface ILevelUpRewardApplyContext
{
}

/// <summary>연결 전 기본값. 부작용 없이 항상 실패합니다.</summary>
public sealed class NullLevelUpRewardApplier : ILevelUpRewardApplier
{
    public static NullLevelUpRewardApplier Instance { get; } = new NullLevelUpRewardApplier();

    private NullLevelUpRewardApplier() { }

    public bool TryApply(ILevelUpReward reward, ILevelUpRewardApplyContext context) => false;
}
