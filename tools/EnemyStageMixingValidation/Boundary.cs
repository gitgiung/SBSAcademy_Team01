// 실제 계산/Director 소스를 실행하는 경계. Unity 엔진 수명·물리·에셋 임포트를 검증하지 않습니다.
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
namespace UnityEngine
{
    public class Object
    {
        public static void Destroy(Object value) { }
        public static T Instantiate<T>(T template, Transform parent) where T : Object, new() => new T();
    }
    public class Component : Object
    {
        private GameObject owner;
        public GameObject gameObject { get => owner ??= new GameObject(); set => owner = value; }
        public Transform transform => gameObject.transform;
    }
    public class MonoBehaviour : Component { public bool enabled = true; public bool isActiveAndEnabled => enabled; }
    public class Transform : Object { }
    public class Camera : Object { }
    public struct Vector3 { }
    public class GameObject : Object
    {
        public string name;
        public Transform transform = new Transform();
        private readonly Dictionary<Type, object> components = new Dictionary<Type, object>();
        public GameObject(string name = "test") { this.name = name; }
        public T AddComponent<T>() where T : new() { var c = new T(); components[typeof(T)] = c; return c; }
        public bool TryGetComponent<T>(out T value)
        {
            bool found = components.TryGetValue(typeof(T), out var c); value = found ? (T)c : default; return found;
        }
        public void SetActive(bool value) { }
    }
    public class SerializeField : Attribute { }
    public class HideInInspector : Attribute { }
    public class DisallowMultipleComponent : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public static class Time { public static float deltaTime = .25f; public static float timeScale = 1f; }
    public static class Debug { public static void LogError(object message, object context) => throw new InvalidOperationException(message.ToString()); }
}
namespace UnityEngine.Serialization { public class FormerlySerializedAsAttribute : Attribute { public FormerlySerializedAsAttribute(string old) { } } }
namespace UnityEngine.SceneManagement { public struct Scene { } }
public sealed class EnemyHealth { }
public sealed class EnemyFollow { }
public sealed class EnemySpawner : UnityEngine.MonoBehaviour
{
    private IEnemySpawnPool pool;
    private UnityEngine.GameObject[] types;
    public void Configure(IEnemySpawnPool pool, UnityEngine.Transform player, UnityEngine.GameObject[] types,
        float interval, int total, int batch, UnityEngine.Camera camera, float margin) { this.pool = pool; this.types = types; }
    public bool TrySpawnScheduled(int index, out EnemySpawnFailure failure)
    {
        bool result = pool.TrySpawn(types[index % types.Length], default, null, default);
        failure = ((IEnemySpawnDiagnostics)pool).LastSpawnFailure;
        return result;
    }
    public void StopSpawning() { }
    public void ResumeSpawning() { }
}
