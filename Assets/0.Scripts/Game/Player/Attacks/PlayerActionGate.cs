/// <summary>플레이어 한 명의 약공격 입력 잠금과 실행 식별자를 소유합니다.</summary>
internal sealed class PlayerActionGate
{
    private readonly PlayerData data;
    private int actionVersion;
    public bool IsInputLocked { get; private set; }

    public PlayerActionGate(PlayerData data) { this.data = data; }

    public bool TryBeginLightAttack(float mpCost, out int actionId)
    {
        actionId = 0;
        if (IsInputLocked) return false;
        // MP 알림에서 다른 행동이 재진입해도 중복 시작하지 않도록 먼저 예약합니다.
        int version = NextVersion();
        IsInputLocked = true;
        if (!data.TrySpendMP(mpCost))
        {
            if (version == actionVersion) IsInputLocked = false;
            return false;
        }
        if (!IsInputLocked || version != actionVersion)
        {
            data.RestoreMP(mpCost); // 비용 알림 중 비활성화돼 실제 시작하지 못한 경우만 복구합니다.
            return false;
        }
        actionId = version;
        return true;
    }

    public bool CompleteLightAttack(int actionId)
    {
        if (!IsInputLocked || actionId != actionVersion) return false;
        IsInputLocked = false;
        return true;
    }

    public void Reset()
    {
        NextVersion();
        IsInputLocked = false;
    }

    private int NextVersion()
    {
        unchecked { actionVersion++; }
        if (actionVersion == 0) actionVersion++;
        return actionVersion;
    }
}
