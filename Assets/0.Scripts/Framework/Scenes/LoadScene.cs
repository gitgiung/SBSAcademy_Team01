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

        SceneType = Define.EScene.LoadTestScene;
        
        return true;
    }

    private IEnumerator Start()
    {
        yield return null;

        // 1. 게임 실행에 필요한 것들을 준비한다.
        yield return PrepareGame();

        if (Managers.PreparationState != GamePreparationState.Ready
            && Managers.PreparationState != GamePreparationState.TestReady)
        {
            Debug.LogError($"Game preparation failed: {Managers.PreparationError}", this);
            yield break;
        }

        string sceneName = Define.EScene.MainTestScene.ToString();
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"로드할 수 없는 씬입니다: {sceneName}. Build Profiles의 Scene List를 확인하세요.", this);
            yield break;
        }

        // 2. 준비가 끝나면 게임 씬을 불러온다.
        yield return Managers.Scene.LoadSceneAsync(Define.EScene.MainTestScene);
    }

    private IEnumerator PrepareGame()
    {
        yield return Managers.PrepareGame(preloadLabel, requireSkillData);
    }
    
    public override void Clear()
    {
        
    }
}
