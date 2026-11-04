// 모델 검증용 Unity 경계. UI, 직렬화, 실제 플레이를 검증하지 않습니다.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Sprite { }
    public class SerializeField : Attribute { }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public static class Mathf { public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max); }
}
public static class Define { public enum EEffectSize { } }
public interface ILoader<TKey, TValue> { Dictionary<TKey, TValue> MakeDict(); }
