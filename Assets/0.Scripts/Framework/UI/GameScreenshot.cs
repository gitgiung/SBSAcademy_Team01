using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 인게임 보조 스크린샷입니다. 안드로이드 OS 캡처는 <see cref="AndroidCapturePermit"/>이 담당합니다.
/// F12 또는 <see cref="Capture"/>로 PNG를 persistentDataPath/Screenshots에 저장합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameScreenshot : MonoBehaviour
{
    private static GameScreenshot instance;
    private bool capturing;

    public static void EnsureOn(GameObject host)
    {
        if (host == null) return;
        if (host.GetComponent<GameScreenshot>() == null)
            host.AddComponent<GameScreenshot>();
    }

    /// <summary>즉시 캡처를 요청합니다. 프레임 렌더 종료 후 파일을 씁니다.</summary>
    public static void Capture()
    {
        if (instance == null)
        {
            GameObject host = GameObject.Find("@Managers");
            if (host == null) host = new GameObject("@Managers");
            EnsureOn(host);
            Managers.Init();
        }
        if (instance == null) return;
        instance.RequestCapture();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f12Key.wasPressedThisFrame)
            RequestCapture();
    }

    private void RequestCapture()
    {
        if (!isActiveAndEnabled || capturing) return;
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        capturing = true;
        // timeScale 0(일시정지)에서도 프레임 끝에서 찍습니다.
        yield return new WaitForEndOfFrame();
        string directory = Path.Combine(Application.persistentDataPath, "Screenshots");
        string path = null;
        try
        {
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null)
            {
                Debug.LogWarning("스크린샷 텍스처를 만들지 못했습니다.", this);
                yield break;
            }
            try
            {
                byte[] png = texture.EncodeToPNG();
                if (png == null || png.Length == 0)
                {
                    Debug.LogWarning("스크린샷 PNG 인코딩에 실패했습니다.", this);
                    yield break;
                }
                File.WriteAllBytes(path, png);
                Debug.Log($"스크린샷 저장: {path}", this);
            }
            finally
            {
                Destroy(texture);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"스크린샷 저장 실패 ({path}): {exception.Message}", this);
        }
        finally
        {
            capturing = false;
        }
    }
}
