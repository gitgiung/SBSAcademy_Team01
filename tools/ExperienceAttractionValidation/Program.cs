using System;
using System.Reflection;
using UnityEngine;

// 실제 ExperiencePickup 소스를 실행합니다. Unity 물리/수명/풀 동작은 대역입니다.
static class Program
{
    static int failures, count;
    static void Set(ExperiencePickup p, string field, object value) => typeof(ExperiencePickup)
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(p, value);
    static void Call(ExperiencePickup p, string method, params object[] args) => typeof(ExperiencePickup)
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(p, args);
    static void Near(float expected, float actual)
    { if (Math.Abs(expected - actual) > .0001f) throw new Exception($"expected {expected}, actual {actual}"); }
    static void Test(string name, Action action)
    {
        count++;
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    static ExperiencePickup New(float acceleration = 2, float speed = 1)
    {
        var p = new ExperiencePickup();
        var player = new PlayerData { experiencePickupDistance = 1000 };
        player.transform.position = new Vector3(100, 0, 0);
        Set(p, "playerData", player); Set(p, "attractionSpeed", speed); Set(p, "attractionAcceleration", acceleration);
        return p;
    }
    static float X(ExperiencePickup p) => p.transform.position.x;
    static void Step(ExperiencePickup p, float dt) => Call(p, "UpdateAttraction", dt);
    static int Main()
    {
        Test("가속도 0은 기존 등속 이동", () => { var p=New(0); Step(p,1); Near(1,X(p)); Step(p,1); Near(2,X(p)); });
        Test("같은 시간의 두 번째 이동이 더 빠름", () => { var p=New(); Step(p,1); Near(2,X(p)); Step(p,1); Near(6,X(p)); });
        Test("프레임 간격 독립", () => { var a=New(); var b=New(); Step(a,1); for(int i=0;i<10;i++) Step(b,.1f); Near(X(a),X(b)); });
        Test("일시정지 동안 누적 가속 없음", () => { var p=New(); Step(p,1); Step(p,0); Step(p,1); Near(6,X(p)); });
        Test("흡입 중 범위를 벗어나도 가속을 유지", () => {
            var p=New(); Step(p,1); var d=(PlayerData)typeof(ExperiencePickup).GetField("playerData",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(p);
            d.experiencePickupDistance=1; Step(p,1); Near(6,X(p)); });
        Test("비활성 후 누적 속도 초기화", () => { var p=New(); Step(p,1); Call(p,"OnDisable"); Step(p,1); Near(4,X(p)); });
        Test("풀 재대여 시 누적 속도 초기화", () => { var p=New(); Step(p,1); p.OnPoolReturn(); p.OnPoolRent(); var d=new PlayerData{experiencePickupDistance=1000}; d.transform.position=new Vector3(100,0,0); Set(p,"playerData",d); Step(p,1); Near(4,X(p)); });
        Test("시작 속도 0은 기존처럼 흡입 끔", () => { var p=New(10,0); Step(p,1); Near(0,X(p)); });
        Test("목표 위치를 지나치지 않음", () => { var p=New(1000); Step(p,1); Near(100,X(p)); });
        Test("잘못된 가속도는 등속으로 처리", () => { foreach(float a in new[]{-1f,float.NaN,float.PositiveInfinity}) { var p=New(a); Step(p,1); Near(1,X(p)); } });
        Console.WriteLine($"{count-failures}/{count} 통과"); return failures==0 ? 0 : 1;
    }
}
public interface IPooledExperiencePickup { }
public interface IPoolable { }
public class PlayerData : MonoBehaviour
{
    public bool IsDead;
    public float experiencePickupDistance;
    public float PickupDistance => experiencePickupDistance;
    public bool TryCollectExperience(float raw, out float awarded) { awarded=raw; return true; }
}
public class PooledObject { public uint LeaseVersion; public bool IsRented; public void ReturnToPool(uint lease) { } }
namespace UnityEngine
{
    public class Object
    {
        public static T FindAnyObjectByType<T>() where T:class => null;
        public static void Destroy(object value) { }
    }
    public class Transform { public Vector3 position; public Vector3 localScale; public T GetComponent<T>() where T:class => null; public T GetComponentInParent<T>() where T:class => null; public T GetComponentInChildren<T>() where T:class => null; }
    public class MonoBehaviour : Object
    {
        public Transform transform = new Transform(); public object gameObject; public bool isActiveAndEnabled = true;
        public T GetComponent<T>() where T:class => null;
        public T GetComponentInChildren<T>(bool inactive=false) where T:class => null;
    }
    public class Collider2D { public T GetComponentInParent<T>() where T:class => null; }
    public class SpriteRenderer { public Color color; }
    public class DisallowMultipleComponent : Attribute { }
    public class SerializeField : Attribute { }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public class ContextMenu : Attribute { public ContextMenu(string value) { } }
    public static class Time { public static float deltaTime; }
    public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); public static float Sin(float value)=>(float)Math.Sin(value); }
    public struct Color { public static Color white => new Color(); }
    public struct Color32 { public Color32(byte r,byte g,byte b,byte a) { } public static implicit operator Color(Color32 c)=>new Color(); }
    public struct Vector2
    {
        public float x,y; public float sqrMagnitude=>x*x+y*y;
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new Vector2{x=a.x-b.x,y=a.y-b.y};
        public static explicit operator Vector2(Vector3 v)=>new Vector2{x=v.x,y=v.y};
    }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 MoveTowards(Vector3 a,Vector3 b,float distance)
        {
            float x=b.x-a.x,y=b.y-a.y,z=b.z-a.z; float length=(float)Math.Sqrt(x*x+y*y+z*z);
            if(length<=distance || length==0) return b;
            return new Vector3(a.x+x/length*distance,a.y+y/length*distance,a.z+z/length*distance);
        }
    }
}
