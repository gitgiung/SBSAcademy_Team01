using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

// 실제 피격 컴포넌트를 실행하는 독립 검증. Unity 검색/코루틴/렌더링은 아래 대역이다.
static class Program
{
    static int failures;
    static int count;
    static Type feedbackType = Assembly.GetExecutingAssembly().GetTypes()
        .Single(t => t.Name == "PlayerHitFeedback" || t.Name == "PlayerHitFeedbackTest");
    static void Call(object value, string method, params object[] args) => feedbackType
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(value, args);
    static void Set(object value, string name, object field) => feedbackType
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(value, field);
    static void Check(bool condition) { if (!condition) throw new Exception("기대값 불일치"); }
    static void Test(string name, Action body)
    {
        count++;
        try { body(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    static void Verify(EncounterPhase? phase, bool enabled, int scene, bool expected)
    {
        var player = new Player();
        MonoBehaviour.Component = player;
        MonoBehaviour.Objects = phase.HasValue ? new[] { new BossEncounterController
            { Phase = phase.Value, isActiveAndEnabled = enabled, gameObject = new GameObject { scene = scene } } }
            : Array.Empty<BossEncounterController>();
        var feedback = (MonoBehaviour)Activator.CreateInstance(feedbackType);
        var renderer = new SpriteRenderer { color = new Color(1, 1, 1, .7f) };
        Set(feedback, "targetRenderer", renderer);
        Set(feedback, "hitStopDuration", .13f);
        Set(feedback, "recoilDistance", .8f);
        Set(feedback, "recoilDuration", .3f);
        Call(feedback, "OnEnable");
        player.Hit();
        Check(player.Calls == (expected ? 1 : 0));
        if (expected) Check(player.Stop == .13f && player.Distance == .8f && player.Duration == .3f);
        Check(renderer.color.r == 1 && renderer.color.g == 0 && renderer.color.a == .7f);
        Call(feedback, "OnDisable");
        Check(renderer.color.g == 1);
        player.Hit();
        Check(player.Calls == (expected ? 1 : 0));
    }
    static int Main()
    {
        Test("일반 전투는 히트스톱/밀림 없이 색 표시", () => Verify(EncounterPhase.TimedCombat, true, 1, false));
        Test("보스전은 Inspector 설정값 사용", () => Verify(EncounterPhase.BossFight, true, 1, true));
        Test("보스 사망 후 밀림 없음", () => Verify(EncounterPhase.AfterBoss, true, 1, false));
        Test("Encounter 없는 씬은 밀림 없음", () => Verify(null, true, 1, false));
        Test("다른 씬 보스전은 무시", () => Verify(EncounterPhase.BossFight, true, 2, false));
        Test("비활성 Encounter는 무시", () => Verify(EncounterPhase.BossFight, false, 1, false));
        Test("컴포넌트 이름에서 Test 제거", () => Check(feedbackType.Name == "PlayerHitFeedback"));
        Console.WriteLine($"{count - failures}/{count} 통과");
        return failures == 0 ? 0 : 1;
    }
}

public enum EncounterPhase { TimedCombat, BossFight, AfterBoss }
public class BossEncounterController : MonoBehaviour { public EncounterPhase Phase { get; set; } }
public class Player : MonoBehaviour
{
    public event Action<float, GameObject> Damaged;
    public int Calls;
    public float Stop, Distance, Duration;
    public void Hit() => Damaged?.Invoke(1, new GameObject());
    public void BeginHitFeedback(MonoBehaviour owner, GameObject source, float stop, float distance, float duration)
        { Calls++; Stop = stop; Distance = distance; Duration = duration; }
    public void CancelHitFeedback(MonoBehaviour owner) { }
}
namespace UnityEngine
{
    public class DisallowMultipleComponent : Attribute { }
    public class SerializeField : Attribute { }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class GameObject { public int scene = 1; }
    public class Coroutine { }
    public class MonoBehaviour
    {
        public static Player Component;
        public static BossEncounterController[] Objects = Array.Empty<BossEncounterController>();
        public bool enabled = true;
        public bool isActiveAndEnabled = true;
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : class => Component as T;
        public T GetComponentInChildren<T>() where T : class => null;
        public static T[] FindObjectsByType<T>() => Objects.Cast<T>().ToArray();
        public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
        public void StopCoroutine(Coroutine routine) { }
    }
    public class SpriteRenderer { public Color color; }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color red => new Color(1, 0, 0, 1);
    }
    public static class Debug { public static void LogWarning(string message, object context) { } }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
    public class WaitForSeconds { public WaitForSeconds(float value) { } }
}
