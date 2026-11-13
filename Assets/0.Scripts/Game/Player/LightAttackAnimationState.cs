using UnityEngine;

/// <summary>실제 Animator 상태의 시작/종료를 같은 약공격 실행 ID에 연결합니다.</summary>
public sealed class LightAttackAnimationState : StateMachineBehaviour
{
    private Player owner;
    private int actionId;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        owner = animator.GetComponent<Player>();
        actionId = owner != null ? owner.ActiveLightAttackId : 0;
        if (owner != null) owner.NotifyLightAttackAnimationProgress(actionId, 0f);
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (owner != null) owner.NotifyLightAttackAnimationProgress(actionId, Mathf.Clamp01(stateInfo.normalizedTime) * stateInfo.length);
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (owner != null)
        {
            // 큰 프레임 간격으로 타격 시점을 건너뛰어도 정상 완주한 모션은 한 번 발사합니다.
            if (stateInfo.normalizedTime >= 1f)
                owner.NotifyLightAttackAnimationProgress(actionId, stateInfo.length);
            owner.NotifyLightAttackAnimationFinished(actionId);
        }
        actionId = 0;
    }
}
