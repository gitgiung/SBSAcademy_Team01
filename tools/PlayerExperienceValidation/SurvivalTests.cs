using System;

static class SurvivalTests
{
    static dynamic Settings() => Activator.CreateInstance(typeof(PlayerData).Assembly.GetType("SurvivalBalanceSettings")
        ?? throw new Exception("생존 스케줄 설정 없음"));
    static dynamic Bonus(dynamic settings) => Activator.CreateInstance(typeof(PlayerData).Assembly.GetType("SurvivalExperienceBonus")
        ?? throw new Exception("성장 보정 상태 없음"), (object)settings);
    static void Equal(object expected, object actual) { if(!expected.Equals(actual)) throw new Exception($"expected {expected}, actual {actual}"); }
    public static void Run(Action<string,Action> test)
    {
        test("36구간 전체 시도8100/첫50/끝400",()=>{dynamic s=Settings(); int sum=0; for(int i=0;i<36;i++) sum+=(int)s.TargetAttempts(i); Equal(8100,sum); Equal(50,(int)s.TargetAttempts(0)); Equal(400,(int)s.TargetAttempts(35));});
        test("30초 구간 경계/18분 클램프",()=>{dynamic s=Settings(); Equal(0,(int)s.WaveIndex(29.999)); Equal(1,(int)s.WaveIndex(30d)); Equal(35,(int)s.WaveIndex(1080d)); Equal(8100,(int)s.ScheduledAttempts(1080d)); Equal(8100,(int)s.ScheduledAttempts(9999d));});
        test("구간내 분산/시작 몰아생성 없음",()=>{dynamic s=Settings(); Equal(0,(int)s.ScheduledAttempts(0d)); Equal(0,(int)s.ScheduledAttempts(.299)); Equal(1,(int)s.ScheduledAttempts(.3)); Equal(25,(int)s.ScheduledAttempts(15d)); Equal(50,(int)s.ScheduledAttempts(29.999)); Equal(50,(int)s.ScheduledAttempts(30d));});
        test("생성시 XP의180초 경계/6단계",()=>{dynamic s=Settings(); Equal(2f,(float)s.RawXP(179.999)); Equal(3f,(float)s.RawXP(180d)); Equal(4f,(float)s.RawXP(360d)); Equal(6f,(float)s.RawXP(540d)); Equal(7f,(float)s.RawXP(720d)); Equal(9f,(float)s.RawXP(900d));});
        test("체크포인트 전 보정 없음/차이3ON",()=>{dynamic b=Bonus(Settings()); b.Update(179.999,1); Equal(false,(bool)b.Active); b.Update(180d,16); Equal(true,(bool)b.Active);});
        test("차이1OFF/차이2켜짐유지",()=>{dynamic b=Bonus(Settings()); b.Update(180d,16); b.Update(360d,33); Equal(true,(bool)b.Active); b.Update(540d,50); Equal(false,(bool)b.Active);});
        test("차이2꺼짐유지",()=>{dynamic b=Bonus(Settings()); b.Update(180d,17); Equal(false,(bool)b.Active);});
        test("2XP두번125퍼센트는합5",()=>{dynamic b=Bonus(Settings()); b.Update(180d,16); Equal(2d,(double)b.Award(2d)); Equal(.5d,(double)b.FractionalCarry); Equal(3d,(double)b.Award(2d)); Equal(0d,(double)b.FractionalCarry);});
        test("보정해제carry보존",()=>{dynamic b=Bonus(Settings()); b.Update(180d,16); b.Award(2d); b.Update(360d,34); Equal(false,(bool)b.Active); Equal(2d,(double)b.Award(2d)); Equal(.5d,(double)b.FractionalCarry);});
        test("최대레벨에서보정종료",()=>{dynamic b=Bonus(Settings()); b.Update(180d,16); b.Update(181d,100); Equal(false,(bool)b.Active);});
        test("설정변경 반영/상태 독립",()=>{dynamic s=Settings(); s.firstWaveAttempts=2; s.attemptsIncrease=0; Equal(72,(int)s.ScheduledAttempts(1080d)); dynamic a=Bonus(s); dynamic b=Bonus(s); a.Update(180d,1); Equal(false,(bool)b.Active);});
    }
}
