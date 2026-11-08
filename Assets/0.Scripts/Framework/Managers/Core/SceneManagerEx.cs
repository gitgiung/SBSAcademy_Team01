using System;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
    private bool clearing;
    private Define.EScene? pendingLoadingDestination;
    public BaseScene CurrentScene => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<BaseScene>())
        .FirstOrDefault(scene => scene.isActiveAndEnabled);

    public void LoadScene(Define.EScene type)
    {
        string sceneName = ValidateScene(type);
        Managers.Clear();
        SceneManager.LoadScene(sceneName);
    }

    // 검증과 정리는 요청 프레임에 끝내고, 실제 로드는 삭제 처리 후 호출할 수 있게 분리합니다.
    internal Action PrepareSceneLoad(Define.EScene type)
    {
        string sceneName = ValidateScene(type);
        Managers.Clear();
        return () => SceneManager.LoadScene(sceneName);
    }

    public AsyncOperation LoadSceneAsync(Define.EScene type)
    {
        string sceneName = ValidateScene(type);
        Managers.Clear();
        return SceneManager.LoadSceneAsync(sceneName);
    }

    /// <summary>
    /// 로딩 씬을 거쳐 목적지로 이동합니다. 목적지는 로딩 씬이 게임 준비를 마친 뒤 한 번만 사용됩니다.
    /// 목적지를 지정하지 않고 LoadScene을 불러오면 로딩 씬은 기존처럼 MainScene으로 이동합니다.
    /// </summary>
    public void LoadSceneThroughLoading(Define.EScene destination)
    {
        if (destination == Define.EScene.Unknown || destination == Define.EScene.LoadScene)
            throw new ArgumentException($"로딩 씬의 목적지로 쓸 수 없는 씬입니다: {destination}");

        // 목적지가 빌드에 없으면 로딩 씬으로 넘어가기 전에 거절합니다.
        ValidateScene(destination);
        pendingLoadingDestination = destination;
        try { LoadScene(Define.EScene.LoadScene); }
        catch
        {
            pendingLoadingDestination = null;
            throw;
        }
    }

    /// <summary>로딩 씬 전용. 예약된 목적지를 돌려주고 비웁니다. 예약이 없으면 fallback을 돌려줍니다.</summary>
    internal Define.EScene ConsumeLoadingDestination(Define.EScene fallback)
    {
        Define.EScene destination = pendingLoadingDestination ?? fallback;
        pendingLoadingDestination = null;
        return destination;
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
