using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public sealed partial class Player
{
    [Header("패링 성공 카메라 줌")]
    [SerializeField, Range(0f, 0.2f)] private float parryZoomRatio = 0.05f;
    [SerializeField, Min(0.01f)] private float parryZoomInDuration = 0.06f;
    [SerializeField, Min(0.01f)] private float parryZoomOutDuration = 0.18f;
    private Coroutine parryZoomRoutine;
    private Camera parryZoomCamera;
    private CinemachineCamera parryZoomVirtualCamera;
    private CinemachineBrain parryZoomBrain;
    private float parryZoomOriginalLens;
    private bool parryZoomOrthographic;
    private bool ownsParryZoom;

    private void PlayParryCameraZoom()
    {
        CancelParryCameraZoom();
        float ratio = Mathf.Clamp(FeedbackValue(parryZoomRatio), 0f, 0.2f);
        if (ratio <= 0f) return;
        parryZoomCamera = Camera.main;
        if (parryZoomCamera == null) return;
        parryZoomBrain = parryZoomCamera.GetComponent<CinemachineBrain>();
        if (parryZoomBrain != null && parryZoomBrain.isActiveAndEnabled)
        {
            parryZoomVirtualCamera = parryZoomBrain.ActiveVirtualCamera as CinemachineCamera;
            if (parryZoomVirtualCamera == null) return;
        }
        parryZoomOrthographic = parryZoomCamera.orthographic;
        parryZoomOriginalLens = parryZoomVirtualCamera != null
            ? (parryZoomOrthographic ? parryZoomVirtualCamera.Lens.OrthographicSize : parryZoomVirtualCamera.Lens.FieldOfView)
            : (parryZoomOrthographic ? parryZoomCamera.orthographicSize : parryZoomCamera.fieldOfView);
        ownsParryZoom = true;
        parryZoomRoutine = StartCoroutine(ParryCameraZoomRoutine(ratio));
    }

    private IEnumerator ParryCameraZoomRoutine(float ratio)
    {
        float closeLens = Mathf.Max(0.01f, parryZoomOriginalLens * (1f - ratio));
        yield return TweenParryZoom(parryZoomOriginalLens, closeLens, parryZoomInDuration);
        yield return TweenParryZoom(closeLens, parryZoomOriginalLens, parryZoomOutDuration);
        RestoreParryZoomLens();
        parryZoomRoutine = null;
    }

    private IEnumerator TweenParryZoom(float from, float to, float duration)
    {
        duration = Mathf.Max(0.01f, FeedbackValue(duration));
        float elapsed = 0f;
        while (elapsed < duration && parryZoomCamera != null)
        {
            if (parryZoomBrain != null && parryZoomVirtualCamera != null &&
                parryZoomBrain.ActiveVirtualCamera != parryZoomVirtualCamera) yield break;
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetParryZoomLens(Mathf.Lerp(from, to, progress));
            yield return null;
        }
    }

    private void SetParryZoomLens(float value)
    {
        if (parryZoomVirtualCamera != null)
        {
            if (parryZoomOrthographic) parryZoomVirtualCamera.Lens.OrthographicSize = value;
            else parryZoomVirtualCamera.Lens.FieldOfView = value;
        }
        else if (parryZoomCamera != null)
        {
            if (parryZoomOrthographic) parryZoomCamera.orthographicSize = value;
            else parryZoomCamera.fieldOfView = value;
        }
    }

    private void RestoreParryZoomLens()
    {
        if (ownsParryZoom) SetParryZoomLens(parryZoomOriginalLens);
        ownsParryZoom = false;
        parryZoomCamera = null;
        parryZoomVirtualCamera = null;
        parryZoomBrain = null;
    }

    private void CancelParryCameraZoom()
    {
        if (parryZoomRoutine != null) StopCoroutine(parryZoomRoutine);
        parryZoomRoutine = null;
        RestoreParryZoomLens();
    }
}