using System;
using UnityEngine;

// 실제 범위/스케일 계산 소스 검증. Unity 렌더링과 보스 애니메이션은 실행하지 않습니다.
static class Program
{
    static int count, failures;
    static void Near(float expected,float actual) { if(Math.Abs(expected-actual)>.001f) throw new Exception($"expected {expected}, actual {actual}"); }
    static void Test(string name,Action body) { count++; try {body(); Console.WriteLine("PASS "+name);} catch(Exception e){failures++;Console.WriteLine("FAIL "+name+": "+e.Message);} }
    static int Main()
    {
        Test("근처 카메라는 지정한 최소 크기 유지",()=>Near(100,BossAttackHitVfxShape.EntranceBurstRadius(100,new Vector2(0,0),new Vector2(3,4))));
        Test("멀리 있는 카메라까지 확장",()=>Near(2000,BossAttackHitVfxShape.EntranceBurstRadius(100,new Vector2(0,0),new Vector2(300,400))));
        Test("보스가 맵 원점 밖에 있어도 거리로 계산",()=>Near(2000,BossAttackHitVfxShape.EntranceBurstRadius(100,new Vector2(-500,100),new Vector2(-200,500))));
        Test("여러 모서리 중 최대 반경 유지",()=>{float size=100;foreach(var p in new[]{new Vector2(300,400),new Vector2(3,4)}) size=BossAttackHitVfxShape.EntranceBurstRadius(size,new Vector2(0,0),p);Near(2000,size);});
        Test("기존 공격 반경/직경 계약 유지",()=>Near(16,BossAttackHitVfxShape.Diameter(8)));
        Test("부모 배율과 무관한 월드 표시 크기",()=>{var scale=BossAttackHitVfxShape.LocalScale(100,new Vector3(4,2,1));Near(200,scale.x*4);Near(200,scale.y*2);});
        Console.WriteLine($"{count-failures}/{count} 통과");return failures==0?0:1;
    }
}
namespace UnityEngine
{
    public static class Mathf {public const float Epsilon=float.Epsilon; public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float v)=>Math.Abs(v);public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;} public static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
}
