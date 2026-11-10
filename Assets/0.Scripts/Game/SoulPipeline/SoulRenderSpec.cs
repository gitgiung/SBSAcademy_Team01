using UnityEngine;

/// <summary>
/// 포켓봉크식(렌더만) 표시 규격. 장르/시스템 이식 금지.
/// 평탄 3D 맵(XZ) + 기울인 직교 카메라로 캐릭을 공간에 올린다.
/// </summary>
public static class SoulRenderSpec
{
    public const int UniversalRendererIndex = 1;
    public const float CharacterWorldScale = 1f;
    public const float CameraOrthoSize = 9f;
    public const float CameraYawDegrees = 45f;
    public const float CameraPitchDegrees = 35f;
    public const float CameraDistance = 18f;
    public const float GroundSize = 40f;

    public static Quaternion CameraRotation =>
        Quaternion.Euler(CameraPitchDegrees, CameraYawDegrees, 0f);

    public static Vector3 CameraOffsetFromFocus =>
        CameraRotation * new Vector3(0f, 0f, -CameraDistance);

    public static Vector3 FlatGroundScale =>
        new Vector3(GroundSize, 1f, GroundSize);
}
