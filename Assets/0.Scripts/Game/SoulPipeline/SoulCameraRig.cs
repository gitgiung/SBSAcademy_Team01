using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>평탄 3D 맵용 기울인 직교 카메라. URP Universal Renderer(index 1)를 강제한다.</summary>
[DisallowMultipleComponent]
public sealed class SoulCameraRig : MonoBehaviour
{
    public const KeyCode DepthViewKey = KeyCode.F2;

    [SerializeField] private Transform focus;
    [SerializeField] private Camera cam;
    [SerializeField] private float orthoSize = SoulRenderSpec.CameraOrthoSize;
    private SoulBossApproachSlash cachedBoss;
    private float bossCacheTimer;
    private float depthDistance = SoulRenderSpec.DepthViewDistance;
    private float depthYaw = SoulRenderSpec.CameraYawDegrees;

    /// <summary>F2 원근 확인 뷰 여부. 기본은 Main과 같은 직교 2D 느낌.</summary>
    public static bool DepthView { get; private set; }

    /// <summary>현재 화면 기준 yaw. 이동 입력을 화면 방향에 맞출 때 쓴다.</summary>
    public static float ViewYaw { get; private set; } = SoulRenderSpec.CameraYawDegrees;

    public void Bind(Transform focusTarget, Camera camera)
    {
        focus = focusTarget;
        cam = camera;
        ApplyCameraSettings();
    }

    /// <summary>직교(2D 느낌) ↔ 원근(3D 확인) 전환. 판정·이동 규칙은 바뀌지 않는다.</summary>
    public void SetDepthView(bool enabled)
    {
        DepthView = enabled;
        depthYaw = SoulRenderSpec.CameraYawDegrees;
        depthDistance = SoulRenderSpec.DepthViewDistance;
        ApplyProjection();
    }

    /// <summary>원근 뷰에서 카메라를 초점 주위로 돌린다.</summary>
    public void OrbitDepthView(float yawDelta)
    {
        if (DepthView) depthYaw += yawDelta;
    }

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        orthoSize = SoulRenderSpec.CameraOrthoSize;
        DepthView = false;
        ViewYaw = SoulRenderSpec.CameraYawDegrees;
        ApplyCameraSettings();
    }

    private void OnDestroy()
    {
        DepthView = false;
        ViewYaw = SoulRenderSpec.CameraYawDegrees;
    }

    private void LateUpdate()
    {
        if (focus == null || cam == null) return;

        if (Input.GetKeyDown(DepthViewKey))
            SetDepthView(!DepthView);

        float scroll = Input.mouseScrollDelta.y;
        if (DepthView)
        {
            if (Mathf.Abs(scroll) > 0.01f)
                depthDistance = Mathf.Clamp(depthDistance - scroll * 1.5f, 8f, 40f);
            if (Input.GetMouseButton(1))
                depthYaw += Input.GetAxis("Mouse X") * 4f;
            if (Input.GetKey(KeyCode.Z)) depthYaw -= 90f * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.C)) depthYaw += 90f * Time.unscaledDeltaTime;
        }
        else
        {
            if (Mathf.Abs(scroll) > 0.01f)
                orthoSize = Mathf.Clamp(orthoSize - scroll * 0.75f, 5f, 16f);
            cam.orthographicSize = orthoSize;
        }

        Vector3 focusPos = focus.position;
        focusPos.y = 0f;

        // 보스 윈드업 때 시야를 살짝 보스 쪽으로 끌어 모션을 읽기 쉽게.
        bossCacheTimer -= Time.unscaledDeltaTime;
        if (cachedBoss == null || bossCacheTimer <= 0f)
        {
            cachedBoss = FindAnyObjectByType<SoulBossApproachSlash>();
            bossCacheTimer = 1f;
        }
        if (cachedBoss != null && cachedBoss.enabled && cachedBoss.IsWindup)
        {
            Vector3 mid = Vector3.Lerp(focusPos, cachedBoss.transform.position, 0.22f);
            mid.y = 0f;
            focusPos = mid;
        }

        PlaceCamera(focusPos);
    }

    private void PlaceCamera(Vector3 focusPos)
    {
        if (DepthView)
        {
            Quaternion rot = Quaternion.Euler(SoulRenderSpec.DepthViewPitchDegrees, depthYaw, 0f);
            cam.transform.SetPositionAndRotation(focusPos + rot * new Vector3(0f, 0f, -depthDistance), rot);
            ViewYaw = depthYaw;
        }
        else
        {
            cam.transform.SetPositionAndRotation(focusPos + SoulRenderSpec.CameraOffsetFromFocus, SoulRenderSpec.CameraRotation);
            ViewYaw = SoulRenderSpec.CameraYawDegrees;
        }
    }

    private void ApplyProjection()
    {
        if (cam == null) return;
        cam.orthographic = !DepthView;
        if (DepthView)
            cam.fieldOfView = SoulRenderSpec.DepthViewFieldOfView;
        else
            cam.orthographicSize = orthoSize;
        if (focus != null)
        {
            Vector3 focusPos = focus.position;
            focusPos.y = 0f;
            PlaceCamera(focusPos);
        }
    }

    private void ApplyCameraSettings()
    {
        if (cam == null) return;
        cam.orthographic = !DepthView;
        cam.orthographicSize = SoulRenderSpec.CameraOrthoSize;
        cam.fieldOfView = SoulRenderSpec.DepthViewFieldOfView;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 120f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        cam.transform.rotation = SoulRenderSpec.CameraRotation;

        var urp = cam.GetComponent<UniversalAdditionalCameraData>();
        if (urp == null) urp = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        urp.SetRenderer(SoulRenderSpec.UniversalRendererIndex);
        urp.renderType = CameraRenderType.Base;
    }
}
