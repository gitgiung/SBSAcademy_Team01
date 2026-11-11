using System;
using UnityEngine;

// 실제 PlayerMovement 소스로 시간/재사용 계약을 검사합니다. Unity 이동·충돌·UI는 대역입니다.
static class Program
{
    static int failures, count;
    static void Check(bool value) { if(!value) throw new Exception("기대값 불일치"); }
    static void Near(float expected,float actual) => Check(Math.Abs(expected-actual)<.0001f);
    static void Test(string name,Action body)
    { count++; try { Time.time=0; body(); Console.WriteLine("PASS "+name); } catch(Exception e) { failures++; Console.WriteLine("FAIL "+name+": "+e.Message); } }
    static PlayerMovement New()=>new PlayerMovement(new Transform(),new PlayerData(),()=>1f);
    static int Main()
    {
        Test("처음에는 준비 완료",()=>{var m=New(); Near(0,m.DashCooldownRemaining); Near(1,m.DashCooldownProgress);});
        Test("성공 직후 0에서 시작",()=>{var m=New(); Check(m.TryBeginDash(new Vector2(1,0),.1f,2,3)); Near(3,m.DashCooldownRemaining); Near(0,m.DashCooldownProgress);});
        Test("절반/완료/완료 이후",()=>{var m=New(); m.TryBeginDash(new Vector2(1,0),.1f,2,3); Time.time=1.5f; Near(.5f,m.DashCooldownProgress); Time.time=3; Near(1,m.DashCooldownProgress); Time.time=10; Near(0,m.DashCooldownRemaining); Near(1,m.DashCooldownProgress);});
        Test("실패한 재입력은 타이머를 다시 시작하지 않음",()=>{var m=New(); m.TryBeginDash(new Vector2(1,0),.1f,2,3); m.Reset(); Time.time=1; Check(!m.TryBeginDash(new Vector2(1,0),.1f,2,3)); Near(2,m.DashCooldownRemaining);});
        Test("방향 없는 실패는 쿨타임 없음",()=>{var m=New(); Check(!m.TryBeginDash(Vector2.zero,.1f,2,3)); Near(1,m.DashCooldownProgress);});
        Test("쿨타임 0은 즉시 준비 표시",()=>{var m=New(); m.TryBeginDash(new Vector2(1,0),.1f,2,0); Near(0,m.DashCooldownRemaining); Near(1,m.DashCooldownProgress);});
        Test("게임 시간이 멈추면 표시도 유지",()=>{var m=New(); m.TryBeginDash(new Vector2(1,0),.1f,2,3); Time.time=1; float before=m.DashCooldownProgress; Near(before,m.DashCooldownProgress);});
        Test("재사용 후 새 쿨타임 반영",()=>{var m=New(); m.TryBeginDash(new Vector2(1,0),.1f,2,3); m.Reset(); Time.time=3; Check(m.TryBeginDash(new Vector2(1,0),.1f,2,6)); Time.time=6; Near(.5f,m.DashCooldownProgress);});
        Console.WriteLine($"{count-failures}/{count} 통과"); return failures==0?0:1;
    }
}
public class PlayerData { public float speed=5; public float MoveSpeed => speed; }
public class CollisionMovement2D { public CollisionMovement2D(UnityEngine.Rigidbody2D body){} public void Move(UnityEngine.Vector2 delta){} }
namespace UnityEngine
{
    public class Rigidbody2D { }
    public class Transform { public Vector3 position; public T GetComponent<T>() where T:class=>null; }
    public static class Time { public static float time,deltaTime; }
    public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); public static float Clamp01(float v)=>Math.Clamp(v,0,1); }
    public struct Vector2
    {
        public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero=>new Vector2(); public float sqrMagnitude=>x*x+y*y;
        public Vector2 normalized=>sqrMagnitude>0?this*(1f/(float)Math.Sqrt(sqrMagnitude)):zero;
        public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
        public static Vector2 ClampMagnitude(Vector2 a,float n)=>a.sqrMagnitude>n*n?a.normalized*n:a;
        public static implicit operator Vector3(Vector2 a)=>new Vector3{x=a.x,y=a.y};
    }
    public struct Vector3 { public float x,y,z; public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3{x=a.x+b.x,y=a.y+b.y,z=a.z+b.z}; }
}
