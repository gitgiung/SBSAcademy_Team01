using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : BaseScene
{
    [SerializeField, Tooltip("비워 두면 리소스 사전 로딩을 생략하고 테스트 모드로 준비합니다.")]
    private string preloadLabel = "";

    [SerializeField, Tooltip("실제 게임에서는 SkillData 누락 시 진입을 막도록 설정합니다.")]
    private bool requireSkillData = false;

    public override bool Init()
    {
        if (base.Init() == false)
        {
            return false;
        }

        SceneType = Define.EScene.LoadScene;
        
        return true;
    }

    private IEnumerator Start()
    {
        // 예약된 목적지는 준비 중 어떤 일이 생겨도 남지 않도록 시작 즉시 꺼낸다.
        // 예약이 없으면(재시작 등) 기존처럼 MainScene으로 이동한다.
        Define.EScene destination = Managers.Scene.ConsumeLoadingDestination(Define.EScene.MainScene);

        yield return null;

        // 원격 설정이 없으면 기존 로컬 경로다. 활성화된 원격 준비 실패는 TestReady로 우회하지 않는다.
        bool remoteRequired = RemoteContentSettings.Load()?.UseRemoteContent == true
            || Managers.RemoteStatus.State != RemoteContentState.Disabled;
        while (true)
        {
            yield return Managers.PrepareRemoteContent();
            RemoteContentState state = Managers.RemoteStatus.State;
            if (state == RemoteContentState.Ready) break;
            if (state == RemoteContentState.Disabled)
            {
                if (remoteRequired)
                {
                    Debug.LogError("원격 준비 담당 객체가 없어져 씬 진입을 중단했습니다.", this);
                    yield break;
                }
                break;
            }
            // 실패를 UI에 남기고, 사용자가 재시도를 요청할 때까지 자동 반복하지 않는다.
            while (Managers.RemoteStatus.State == RemoteContentState.Failed) yield return null;
        }

        // 1. 게임 실행에 필요한 것들을 준비한다.
        yield return PrepareGame();

        if (Managers.PreparationState != GamePreparationState.Ready
            && Managers.PreparationState != GamePreparationState.TestReady)
        {
            Debug.LogError($"Game preparation failed: {Managers.PreparationError}", this);
            yield break;
        }

        string sceneName = destination.ToString();
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"로드할 수 없는 씬입니다: {sceneName}. Build Profiles의 Scene List를 확인하세요.", this);
            yield break;
        }

        // 2. 준비가 끝나면 목적지 씬(로비 또는 게임 씬)을 불러온다.
        yield return Managers.Scene.LoadSceneAsync(destination);
    }

    private IEnumerator PrepareGame()
    {
        yield return Managers.PrepareGame(preloadLabel, requireSkillData);
    }
    
    public override void Clear()
    {
        
    }
}
