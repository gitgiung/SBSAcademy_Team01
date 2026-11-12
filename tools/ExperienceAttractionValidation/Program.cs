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
    static PlayerData Player(ExperiencePickup p) => (PlayerData)typeof(ExperiencePickup)
        .GetField("playerData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(p);
    // 플레이어가 획득 범위(1) 밖인 100 거리에 있는 보석입니다.
    static ExperiencePickup Far(float acceleration = 0, float speed = 1, float pickupDistance = 1)
    { var p = New(acceleration, speed); Player(p).experiencePickupDistance = pickupDistance; return p; }
    static void True(bool value, string what) { if (!value) throw new Exception(what); }
    static void MagnetTests()
    {
        Test("자석 없이는 획득 범위 밖 보석이 움직이지 않음", () => { var p=Far(); Step(p,1); Near(0,X(p)); });
        Test("자석은 획득 범위 밖 보석도 끌어당김", () => { var p=Far(); True(p.BeginMagnetAttraction(5),"거부됨"); Step(p,1); Near(5,X(p)); Step(p,1); Near(10,X(p)); });
        Test("자석은 기본 흡입 속도 0·획득 범위 0이어도 최소 속도로 끌어당김", () => { var p=Far(0,0,0); Step(p,1); Near(0,X(p)); True(p.BeginMagnetAttraction(4),"거부됨"); Step(p,1); Near(4,X(p)); });
        Test("자석 최소 속도보다 빠른 보석은 원래 속도를 유지", () => { var p=Far(0,10); True(p.BeginMagnetAttraction(4),"거부됨"); Step(p,1); Near(10,X(p)); });
        Test("자석 흡입에도 보석의 가속이 적용됨", () => { var p=Far(2); True(p.BeginMagnetAttraction(5),"거부됨"); Step(p,1); Near(6,X(p)); Step(p,1); Near(14,X(p)); });
        Test("이미 흡입 중이던 보석은 자석 후에도 누적 가속 유지", () => { var p=New(); Step(p,1); Near(2,X(p)); True(p.BeginMagnetAttraction(1),"거부됨"); Step(p,1); Near(6,X(p)); });
        Test("약한 자석이 더 강한 자석 속도를 낮추지 않음", () => { var p=Far(); True(p.BeginMagnetAttraction(8),"거부됨"); True(p.BeginMagnetAttraction(3),"거부됨"); Step(p,1); Near(8,X(p)); });
        Test("잘못된 자석 속도는 거부하고 보석이 움직이지 않음", () => {
            foreach(float speed in new[]{0f,-1f,float.NaN,float.PositiveInfinity}) { var p=Far(); True(!p.BeginMagnetAttraction(speed),"허용됨 "+speed); Step(p,1); Near(0,X(p)); } });
        Test("사망한 플레이어·이미 획득된 보석에는 자석이 걸리지 않음", () => {
            var dead=Far(); Player(dead).IsDead=true; True(!dead.BeginMagnetAttraction(5),"사망 허용");
            var taken=Far(); Set(taken,"collected",true); True(!taken.BeginMagnetAttraction(5),"획득 후 허용"); Step(taken,1); Near(0,X(taken)); });
        Test("자석 흡입 중 플레이어가 죽으면 멈춤", () => { var p=Far(); p.BeginMagnetAttraction(5); Step(p,1); Near(5,X(p)); Player(p).IsDead=true; Step(p,1); Near(5,X(p)); });
        Test("자석 흡입도 목표 위치를 지나치지 않음", () => { var p=Far(); p.BeginMagnetAttraction(1000); Step(p,1); Near(100,X(p)); });
        Test("일시정지 동안 자석 흡입이 진행되지 않음", () => { var p=Far(2); p.BeginMagnetAttraction(5); Step(p,1); Step(p,0); Step(p,1); Near(14,X(p)); });
        Test("풀 반납·재대여 후 자석 상태 초기화", () => { var p=Far(); p.BeginMagnetAttraction(5); p.OnPoolReturn(); p.OnPoolRent(); var d=new PlayerData{experiencePickupDistance=1}; d.transform.position=new Vector3(100,0,0); Set(p,"playerData",d); Step(p,1); Near(0,X(p)); });
        Test("비활성 후 자석 상태 초기화", () => { var p=Far(); p.BeginMagnetAttraction(5); Call(p,"OnDisable"); Step(p,1); Near(0,X(p)); });
        Test("새로 스폰(Initialize)하면 자석 상태 초기화", () => { var p=Far(); var d=Player(p); p.BeginMagnetAttraction(5); p.Initialize(null); Set(p,"playerData",d); Step(p,1); Near(0,X(p)); });
    }
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
        MagnetTests();
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
