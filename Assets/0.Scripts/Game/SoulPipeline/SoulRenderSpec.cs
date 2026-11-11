using UnityEngine;

/// <summary>
/// SoulArena 표시 규격. 장르/시스템 이식 금지.
/// 평탄 3D(XZ) + 직교. MainScene(ortho·정면)과 비슷하게 읽히도록
/// 탑뷰에서 살짝만 기울인다(yaw 0, pitch 55). WASD는 CameraYaw 기준.
/// </summary>
public static class SoulRenderSpec
{
    public const int UniversalRendererIndex = 1;
    public const float CharacterWorldScale = 1f;
    public const float CameraOrthoSize = 10f;
    public const float CameraYawDegrees = 0f;
    public const float CameraPitchDegrees = 55f;
    public const float CameraDistance = 18f;
    public const float GroundSize = 40f;

    // F2 원근 확인 뷰: 2D가 아님을 보여 주는 용도. 기본 플레이 뷰는 위 직교 값.
    public const float DepthViewPitchDegrees = 38f;
    public const float DepthViewFieldOfView = 40f;
    public const float DepthViewDistance = 24f;

    public static Quaternion CameraRotation =>
        Quaternion.Euler(CameraPitchDegrees, CameraYawDegrees, 0f);

    public static Vector3 CameraOffsetFromFocus =>
        CameraRotation * new Vector3(0f, 0f, -CameraDistance);

    public static Vector3 FlatGroundScale =>
        new Vector3(GroundSize, 1f, GroundSize);
}
