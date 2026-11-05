// 순수 계산/PlayerData 계약 검증용 경계. Unity 수명·UI·직렬화를 대신하지 않습니다.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class MonoBehaviour { }
    public class Sprite { }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class ContextMenu : Attribute { public ContextMenu(string value) { } }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
    }
}
public static class Define { public enum EEffectSize { } }
public interface ILoader<TKey, TValue> { Dictionary<TKey, TValue> MakeDict(); }
