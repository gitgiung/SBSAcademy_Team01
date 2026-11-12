using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>전투·로비 UI 글자 크기를 한 비율로 키웁니다. 코드 생성 글자와 씬/프리팹 TMP에 공통 적용합니다.</summary>
public static class UiFontScale
{
    public const float Factor = 1.3f;

    public static float Size(float baseSize) => baseSize * Factor;

    public static void ApplyTo(TMP_Text text)
    {
        if (text == null || text.GetComponent<UiFontScaledMarker>() != null) return;
        text.fontSize = Size(text.fontSize);
        if (text.enableAutoSizing)
        {
            text.fontSizeMin = Size(text.fontSizeMin);
            text.fontSizeMax = Size(text.fontSizeMax);
        }
        text.gameObject.AddComponent<UiFontScaledMarker>();
    }

    public static void ApplyScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                ApplyTo(text);
    }
}

/// <summary>이미 1.3배 반영된 TMP에 중복 배율을 막습니다.</summary>
[DisallowMultipleComponent]
public sealed class UiFontScaledMarker : MonoBehaviour { }

/// <summary>씬 로드마다 기존 TMP 글자 크기를 1.3배로 맞춥니다.</summary>
[DisallowMultipleComponent]
public sealed class UiFontScaleBootstrap : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            UiFontScale.ApplyScene(SceneManager.GetSceneAt(i));
    }

    private void OnDisable() => SceneManager.sceneLoaded -= HandleSceneLoaded;

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => UiFontScale.ApplyScene(scene);
}
