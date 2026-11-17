using UnityEngine;

/// <summary>
/// 예전 코드에서 만들던 글자만 1.3배입니다.
/// 씬·프리팹의 기존 글자는 저장된 크기에 이미 반영되어 있습니다.
/// 새로 만드는 UI는 이 배율을 쓰지 않고 fontSize를 그대로 둡니다.
/// </summary>
public static class UiFontScale
{
    public const float Factor = 1.3f;

    public static float Size(float baseSize) => baseSize * Factor;
}

/// <summary>코드에서 1.3배로 만든 글자에 같은 배율이 다시 곱해지지 않게 합니다.</summary>
[DisallowMultipleComponent]
public sealed class UiFontScaledMarker : MonoBehaviour { }
