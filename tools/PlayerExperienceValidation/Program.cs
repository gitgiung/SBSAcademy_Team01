using System;
using System.Collections.Generic;
using System.Reflection;

static class Program
{
    static int failed;
    static int count;
    static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"expected {expected}, actual {actual}");
    }
    static int Read(PlayerData data, string name) => (int)(typeof(PlayerData).GetProperty(name)
        ?? throw new Exception($"성장 기능 없음: {name}")).GetValue(data);
    static bool Complete(PlayerData data) => (bool)(typeof(PlayerData).GetMethod("TryCompleteLevelUp")
        ?? throw new Exception("선택 완료 계약 없음")).Invoke(data, null);
    static PlayerData New() { var data = new PlayerData(); data.InitEXP(); return data; }
    static void Test(string name, Action test)
    {
        count++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }
    static int Main()
    {
        Test("시작은 레벨1/0XP/요구13", () => { var d = New(); Equal(13f, d.maxEXP); Equal(1, Read(d,"CurrentLevel")); Equal(0f,d.CurrentEXP); });
        Test("정확히13XP는 레벨2/0XP/요구15", () => { var d=New(); d.AddEXP(13); Equal(2,Read(d,"CurrentLevel")); Equal(0f,d.CurrentEXP); Equal(15f,d.maxEXP); });
        Test("15XP는 레벨2/잔여2", () => { var d=New(); d.AddEXP(15); Equal(2,Read(d,"CurrentLevel")); Equal(2f,d.CurrentEXP); });
        Test("30XP는 레벨3/잔여2/선택2회", () => { var d=New(); d.AddEXP(30); Equal(3,Read(d,"CurrentLevel")); Equal(2f,d.CurrentEXP); Equal(2,Read(d,"PendingLevelUps")); });
        Test("선택 완료는 한 건씩/중복 완료 거절", () => { var d=New(); d.AddEXP(30); Equal(true,Complete(d)); Equal(1,Read(d,"PendingLevelUps")); Equal(true,Complete(d)); Equal(false,Complete(d)); });
        Test("총33032XP는 레벨99/잔여867", () => { var d=New(); d.AddEXP(33032); Equal(99,Read(d,"CurrentLevel")); Equal(867f,d.CurrentEXP); Equal(868f,d.maxEXP); });
        Test("총33033XP는 레벨100/선택99회", () => { var d=New(); d.AddEXP(33033); Equal(100,Read(d,"CurrentLevel")); Equal(0f,d.CurrentEXP); Equal(99,Read(d,"PendingLevelUps")); Equal(0f,d.maxEXP); });
        Test("상한 초과XP 보존/추가레벨업 없음", () => { var d=New(); d.AddEXP(34033); d.AddEXP(20); Equal(100,Read(d,"CurrentLevel")); Equal(1020f,d.CurrentEXP); Equal(99,Read(d,"PendingLevelUps")); });
        Test("소수XP 호환 유지", () => { var d=New(); d.AddEXP(12.5f); d.AddEXP(.75f); Equal(2,Read(d,"CurrentLevel")); Equal(.25f,d.CurrentEXP); });
        Test("음수/NaN/무한/0 거절", () => { var d=New(); foreach(float v in new[]{-1f,float.NaN,float.PositiveInfinity,0f}) Equal(false,d.AddEXP(v)); Equal(0f,d.CurrentEXP); });
        Test("사망 후 XP 거절", () => { var d=New(); d.ApplyDamage(100); Equal(false,d.AddEXP(13)); Equal(1,Read(d,"CurrentLevel")); });
        Test("초기화는 레벨/XP/선택 대기 초기화", () => { var d=New(); d.AddEXP(30); d.InitEXP(); Equal(1,Read(d,"CurrentLevel")); Equal(0f,d.CurrentEXP); Equal(0,Read(d,"PendingLevelUps")); });
        Test("SetEXP 초과분도 성장 공식 적용", () => { var d=New(); Equal(true,d.SetEXP(15,999)); Equal(2,Read(d,"CurrentLevel")); Equal(2f,d.CurrentEXP); Equal(15f,d.maxEXP); Equal(false,d.SetEXP(0,float.NaN)); });
        Test("최종EXP 통지 전에 모든 선택 대기 기록", () => { var d=New(); int pending=-1; d.EXPChanged+=(a,b)=>pending=Read(d,"PendingLevelUps"); d.AddEXP(30); Equal(2,pending); });
        Test("중간 레벨 통지 순서 보존", () => { var d=New(); var levels=new List<int>(); typeof(PlayerData).GetEvent("LevelChanged").AddEventHandler(d,(Action<int>)levels.Add); d.AddEXP(30); Equal("2,3",string.Join(",",levels)); });
        Test("모든 요구량 합계33033/범위 밖0", () => { var settings=new PlayerExperienceSettings(); int sum=0; for(int i=1;i<100;i++) sum+=settings.RequiredXP(i); Equal(33033,sum); Equal(0,settings.RequiredXP(0)); Equal(0,settings.RequiredXP(100)); Equal(37,settings.RequiredXP(10)); Equal(736,settings.RequiredXP(90)); });
        Test("변경 가능한 공식 계수/상태 독립", () => { var s=new PlayerExperienceSettings{baseNumerator=20000,linearNumerator=0,quadraticNumerator=0}; var a=new PlayerExperienceProgress(s); var b=new PlayerExperienceProgress(s); a.Add(5); Equal(3,a.Level); Equal(1d,a.Experience); Equal(1,b.Level); Equal(0d,b.Experience); });
        Console.WriteLine($"{count-failed}/{count} 통과");
        return failed==0?0:1;
    }
}
