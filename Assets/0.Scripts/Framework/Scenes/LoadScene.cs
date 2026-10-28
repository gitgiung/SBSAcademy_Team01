using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : BaseScene
{
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

        // 2. 준비가 끝나면 게임 씬을 불러온다.
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                Define.EScene.MainTestScene.ToString()
            );

        while (!operation.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator PrepareGame()
    {
        // 나중에 필요한 준비 작업을 여기에 추가한다.
        // 예: 리소스 로드 → 아이템·스킬·증강 데이터 초기화

        yield break;
    }
    
    public override void Clear()
    {
        
    }
}
