using System;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
    private bool clearing;
    public BaseScene CurrentScene => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<BaseScene>())
        .FirstOrDefault(scene => scene.isActiveAndEnabled);

    public void LoadScene(Define.EScene type)
    {
        string sceneName = ValidateScene(type);
        Managers.Clear();
        SceneManager.LoadScene(sceneName);
    }

    public AsyncOperation LoadSceneAsync(Define.EScene type)
    {
        string sceneName = ValidateScene(type);
        Managers.Clear();
        return SceneManager.LoadSceneAsync(sceneName);
    }

    private string ValidateScene(Define.EScene type)
    {
        string sceneName = GetSceneName(type);
        // 로드할 수 없는 씬이면 현재 플레이 객체를 먼저 지우지 않습니다.
        if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            throw new InvalidOperationException($"로드할 수 없는 씬입니다: {type}. Build Profiles의 Scene List를 확인하세요.");

        return sceneName;
    }

    private string GetSceneName(Define.EScene type)
    {
        return Enum.GetName(typeof(Define.EScene), type);
    }

    public void Clear()
    {
        if (clearing) return;
        clearing = true;
        try { CurrentScene?.Clear(); }
        finally { clearing = false; }
    }
}
