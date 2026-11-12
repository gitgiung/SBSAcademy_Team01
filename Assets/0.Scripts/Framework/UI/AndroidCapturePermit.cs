using System;
using UnityEngine;

/// <summary>
/// 안드로이드 시스템 캡처(전원+볼륨/제스처)가 막히지 않도록 Activity window의 FLAG_SECURE를 제거합니다.
/// 에디터·비안드로이드에서는 아무 동작도 하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AndroidCapturePermit : MonoBehaviour
{
    private const int FlagSecure = 0x00002000;

    public static void EnsureOn(GameObject host)
    {
        if (host == null) return;
        if (host.GetComponent<AndroidCapturePermit>() == null)
            host.AddComponent<AndroidCapturePermit>();
    }

    private void OnEnable() => ClearSecureFlag();

    private void OnApplicationFocus(bool focused)
    {
        if (focused) ClearSecureFlag();
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused) ClearSecureFlag();
    }

    private static void ClearSecureFlag()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (activity == null) return;
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
                        {
                            if (window == null) return;
                            window.Call("clearFlags", FlagSecure);
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning("안드로이드 캡처 허용(UI 스레드) 실패: " + exception.Message);
                    }
                }));
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("안드로이드 캡처 허용 실패: " + exception.Message);
        }
#endif
    }
}
