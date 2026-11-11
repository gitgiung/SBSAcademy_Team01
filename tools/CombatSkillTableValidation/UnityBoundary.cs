// 독립 OOXML 검증용 경계. Unity 기능 검증을 대신하지 않습니다.
using System;
using System.Text.Json;

namespace UnityEngine
{
    public class MonoBehaviour { }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string title) { } }
    public sealed class ContextMenuAttribute : Attribute { public ContextMenuAttribute(string name) { } }
    public class Sprite { }
    public sealed class SerializeField : Attribute { }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public class TextAsset { public string text; }
    public static class Resources { public static T Load<T>(string path) where T : class => null; }
    public static class Application { public static string dataPath => System.IO.Path.GetFullPath("Assets"); }
    public static class Time { public static float timeScale = 1f; }
    public readonly struct Vector2(float x, float y)
    {
        public readonly float x = x;
        public readonly float y = y;
        public static Vector2 down => new Vector2(0f, -1f);
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized => new Vector2(x / MathF.Sqrt(sqrMagnitude), y / MathF.Sqrt(sqrMagnitude));
    }
    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine(message);
        public static void LogError(object message) => Console.Error.WriteLine(message);
    }
    public static class Mathf
    {
        public static int RoundToInt(float n) => (int)Math.Round(n);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
    }
    public static class JsonUtility
    {
        public static string ToJson(object value, bool pretty) => JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            IncludeFields = true, WriteIndented = pretty, IndentSize = 4,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        public static T FromJson<T>(string value) => JsonSerializer.Deserialize<T>(value, new JsonSerializerOptions { IncludeFields = true });
    }
}
#if !PLAYER_STATS_VALIDATION
internal sealed class PlayerData
{
    public float MP { get; private set; } = 20f;
    public bool IsDead => false;
    public bool TrySpendMP(float amount)
    {
        if (amount > MP) return false;
        MP -= amount;
        return true;
    }
    public void RestoreMP(float amount) => MP += amount;
}
#endif
public static class Define { public enum EEffectSize { } }
public interface ILoader<TKey, TValue> { System.Collections.Generic.Dictionary<TKey, TValue> MakeDict(); }
namespace UnityEditor
{
    public sealed class MenuItem : Attribute { public MenuItem(string path) { } }
    public static class AssetDatabase { public static void Refresh() { } }
}
