// 독립 OOXML 검증용 경계. Unity 기능 검증을 대신하지 않습니다.
using System;
using System.Text.Json;

namespace UnityEngine
{
    public class TextAsset { public string text; }
    public static class Resources { public static T Load<T>(string path) where T : class => null; }
    public static class Application { public static string dataPath => System.IO.Path.GetFullPath("Assets"); }
    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine(message);
        public static void LogError(object message) => Console.Error.WriteLine(message);
    }
    public static class Mathf { public static int RoundToInt(float n) => (int)Math.Round(n); }
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
namespace UnityEditor
{
    public sealed class MenuItem : Attribute { public MenuItem(string path) { } }
    public static class AssetDatabase { public static void Refresh() { } }
}
