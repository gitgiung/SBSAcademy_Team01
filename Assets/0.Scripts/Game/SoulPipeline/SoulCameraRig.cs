using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>평탄 3D 맵용 기울인 직교 카메라. URP Universal Renderer(index 1)를 강제한다.</summary>
[DisallowMultipleComponent]
public sealed class SoulCameraRig : MonoBehaviour
{
    [SerializeField] private Transform focus;
    [SerializeField] private Camera cam;
    [SerializeField] private float orthoSize = SoulRenderSpec.CameraOrthoSize;
    private SoulBossApproachSlash cachedBoss;
    private float bossCacheTimer;

    public void Bind(Transform focusTarget, Camera camera)
    {
        focus = focusTarget;
        cam = camera;
        ApplyCameraSettings();
    }

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        orthoSize = SoulRenderSpec.CameraOrthoSize;
        ApplyCameraSettings();
    }

    private void LateUpdate()
    {
        if (focus == null || cam == null) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            orthoSize = Mathf.Clamp(orthoSize - scroll * 0.75f, 5f, 16f);

        cam.orthographicSize = orthoSize;
        Vector3 focusPos = focus.position;
        focusPos.y = 0f;

        // 보스 윈드업 때 시야를 살짝 보스 쪽으로 끌어 모션을 읽기 쉽게.
        bossCacheTimer -= Time.unscaledDeltaTime;
        if (cachedBoss == null || bossCacheTimer <= 0f)
        {
            cachedBoss = FindFirstObjectByType<SoulBossApproachSlash>();
            bossCacheTimer = 1f;
        }
        if (cachedBoss != null && cachedBoss.enabled && cachedBoss.IsWindup)
        {
            Vector3 mid = Vector3.Lerp(focusPos, cachedBoss.transform.position, 0.22f);
            mid.y = 0f;
            focusPos = mid;
        }

        cam.transform.rotation = SoulRenderSpec.CameraRotation;
        cam.transform.position = focusPos + SoulRenderSpec.CameraOffsetFromFocus;
    }

    private void ApplyCameraSettings()
    {
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = SoulRenderSpec.CameraOrthoSize;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 80f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        cam.transform.rotation = SoulRenderSpec.CameraRotation;

        var urp = cam.GetComponent<UniversalAdditionalCameraData>();
        if (urp == null) urp = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        urp.SetRenderer(SoulRenderSpec.UniversalRendererIndex);
        urp.renderType = CameraRenderType.Base;
    }
}
