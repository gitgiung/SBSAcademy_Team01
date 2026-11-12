using System.Collections;
using UnityEngine;

public sealed partial class Player
{
    [Header("패링 성공 연출")]
    [SerializeField, Min(0f)] private float parryHitStopDuration = 0.08f;
    [SerializeField, Min(0f)] private float parryRecoilDistance = 0.35f;
    [SerializeField, Min(0.01f)] private float parryRecoilDuration = 0.12f;

    private Coroutine parryFeedbackRoutine;
    private MonoBehaviour feedbackOwner;
    private bool parryFeedbackRunning;
    private bool ownsParryHitStop;
    private float timeScaleBeforeParry;

    private void BeginParryFeedback(GameObject source)
    {
        BeginReactionFeedback(null, source, parryHitStopDuration, parryRecoilDistance, parryRecoilDuration);
    }

    internal void BeginHitFeedback(MonoBehaviour owner, GameObject source, float stop, float distance, float duration)
    {
        if (Time.timeScale <= 0f || !isActiveAndEnabled || hasDied || data.IsDead) return;
        CancelParryFeedback();
        BeginReactionFeedback(owner, source, stop, distance, duration);
    }

    internal void CancelHitFeedback(MonoBehaviour owner)
    {
        if (feedbackOwner == owner) CancelParryFeedback();
    }

    private void BeginReactionFeedback(MonoBehaviour owner, GameObject source, float stop, float distance, float duration)
    {
        if (parryFeedbackRunning || Time.timeScale <= 0f) return;
        Vector2 away = source != null ? (Vector2)(transform.position - source.transform.position) : Vector2.zero;
        if (away.sqrMagnitude > 0.0001f) away.Normalize();
        else away = Vector2.zero;
        feedbackOwner = owner;
        parryFeedbackRunning = true;
        joystickController?.StopMovement();
        parryFeedbackRoutine = StartCoroutine(ParryFeedbackRoutine(away, stop, distance, duration));
    }

    private IEnumerator ParryFeedbackRoutine(Vector2 away, float stop, float distance, float duration)
    {
        stop = FeedbackValue(stop);
        distance = FeedbackValue(distance);
        duration = Mathf.Max(0.01f, FeedbackValue(duration));
        if (stop > 0f)
        {
            timeScaleBeforeParry = Time.timeScale;
            ownsParryHitStop = true;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(stop);
            EndParryHitStop();
        }

        float elapsed = 0f;
        while (elapsed < duration && distance > 0f && away.sqrMagnitude > 0f)
        {
            float step = Mathf.Min(Time.deltaTime, duration - elapsed);
            if (step > 0f)
            {
                movement.ApplyDisplacement(away * (distance * step / duration));
                elapsed += step;
            }
            yield return null;
        }
        parryFeedbackRunning = false;
        feedbackOwner = null;
        parryFeedbackRoutine = null;
    }

    // Popup pause calls this before capturing its own resume scale.
    internal void EndParryHitStop()
    {
        if (!ownsParryHitStop) return;
        ownsParryHitStop = false;
        if (Time.timeScale != 0f) return;
        foreach (GameManager manager in FindObjectsByType<GameManager>())
            if (manager.gameObject.scene == gameObject.scene && manager.state != GameState.Playing)
                return;
        Time.timeScale = timeScaleBeforeParry;
    }

    private void CancelParryFeedback()
    {
        EndParryHitStop();
        if (parryFeedbackRoutine != null) StopCoroutine(parryFeedbackRoutine);
        parryFeedbackRoutine = null;
        feedbackOwner = null;
        parryFeedbackRunning = false;
    }

    private static float FeedbackValue(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
}

/// <summary>패링 성공 순간의 짧은 기기 진동. 에디터와 PC에서는 재생하지 않습니다.</summary>
public static class DeviceHaptics
{
    public static void Pulse(long milliseconds = 40)
    {
        if (milliseconds <= 0) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try { AndroidPulse(milliseconds); }
        catch (System.Exception exception)
        {
            Debug.LogWarning("패링 진동을 재생하지 못했습니다: " + exception.Message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void AndroidPulse(long milliseconds)
    {
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
        {
            if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdk = version.GetStatic<int>("SDK_INT");
                if (sdk >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        int amplitude = effectClass.GetStatic<int>("DEFAULT_AMPLITUDE");
                        using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                            vibrator.Call("vibrate", effect);
                    }
                }
                else vibrator.Call("vibrate", milliseconds);
            }
        }
    }
#endif
}
