using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
    public BaseScene CurrentScene { get { return GameObject.FindAnyObjectByType<BaseScene>(); } }

    public void LoadScene(Define.EScene type)
    {
        string sceneName = GetSceneName(type);
        // 로드할 수 없는 씬이면 현재 플레이 객체를 먼저 지우지 않습니다.
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            throw new InvalidOperationException($"로드할 수 없는 씬입니다: {type}. Build Profiles의 Scene List를 확인하세요.");

        Managers.Clear();
        SceneManager.LoadScene(sceneName);
    }

    private string GetSceneName(Define.EScene type)
    {
        return Enum.GetName(typeof(Define.EScene), type);
    }

    public void Clear()
    {
        // BaseScene 구현이 첨부되지 않아 기존처럼 비워 둡니다.
    }
}
