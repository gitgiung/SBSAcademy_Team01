using System;

static class Program
{
    static int failures;
    static int count;

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static void Test(string name, Action body)
    {
        count++;
        try
        {
            body();
            Console.WriteLine("PASS " + name);
        }
        catch (Exception exception)
        {
            failures++;
            Console.WriteLine("FAIL " + name + ": " + exception.Message);
        }
    }

    static int Main()
    {
        Test("패링은 간격 안에서도 울리고 다음 가벼운 진동을 미룬다", () =>
        {
            long next = 500;
            Check(HapticSchedule.Accept(HapticCue.Parry, 100, ref next), "패링이 거절됨");
            Check(next == 100 + HapticSchedule.SoftGapMilliseconds, "간격이 갱신되지 않음");
            Check(!HapticSchedule.Accept(HapticCue.Guard, next - 1, ref next), "가드가 바로 겹침");
            Check(!HapticSchedule.Accept(HapticCue.Hit, next - 1, ref next), "적중이 바로 겹침");
        });

        Test("피격은 간격 안에서도 울린다", () =>
        {
            long next = 500;
            Check(HapticSchedule.Accept(HapticCue.Hurt, 100, ref next), "피격이 거절됨");
            Check(next == 190, "피격 뒤 간격");
        });

        Test("가드 막기와 공격 적중은 간격을 지킨다", () =>
        {
            long next = 0;
            Check(HapticSchedule.Accept(HapticCue.Guard, 0, ref next), "첫 가드");
            Check(!HapticSchedule.Accept(HapticCue.Hit, 89, ref next), "89ms 적중");
            Check(HapticSchedule.Accept(HapticCue.Hit, 90, ref next), "90ms 적중");
            Check(!HapticSchedule.Accept(HapticCue.Guard, 179, ref next), "179ms 가드");
            Check(HapticSchedule.Accept(HapticCue.Guard, 180, ref next), "180ms 가드");
        });

        Test("거절된 진동은 간격을 움직이지 않는다", () =>
        {
            long next = 200;
            Check(!HapticSchedule.Accept(HapticCue.Hit, 199, ref next), "거절이어야 함");
            Check(next == 200, "간격이 바뀜");
        });

        Test("알 수 없는 종류는 울리지 않는다", () =>
        {
            long next = 0;
            Check(!HapticSchedule.Accept((HapticCue)99, 0, ref next), "알 수 없는 종류");
            Check(next == 0, "간격이 바뀜");
        });

        Test("패링이 가장 강하고 길고, 적중이 가장 가볍다", () =>
        {
            Check(HapticSchedule.Milliseconds(HapticCue.Parry) == 40, "패링 길이");
            Check(HapticSchedule.Milliseconds(HapticCue.Parry) > HapticSchedule.Milliseconds(HapticCue.Hurt), "피격 길이");
            Check(HapticSchedule.Milliseconds(HapticCue.Hurt) > HapticSchedule.Milliseconds(HapticCue.Guard), "가드 길이");
            Check(HapticSchedule.Milliseconds(HapticCue.Guard) >= HapticSchedule.Milliseconds(HapticCue.Hit), "적중 길이");
            Check(HapticSchedule.Milliseconds(HapticCue.Hit) > 0, "적중 길이 0");
            Check(HapticSchedule.Amplitude(HapticCue.Parry) > HapticSchedule.Amplitude(HapticCue.Hurt), "피격 세기");
            Check(HapticSchedule.Amplitude(HapticCue.Hurt) > HapticSchedule.Amplitude(HapticCue.Guard), "가드 세기");
            Check(HapticSchedule.Amplitude(HapticCue.Guard) > HapticSchedule.Amplitude(HapticCue.Hit), "적중 세기");
            Check(HapticSchedule.Amplitude(HapticCue.Hit) > 0, "적중 세기 0");
            Check(HapticSchedule.Milliseconds((HapticCue)99) == 0, "알 수 없는 길이");
            Check(HapticSchedule.Amplitude((HapticCue)99) == 0, "알 수 없는 세기");
            Check(HapticSchedule.SoftGapMilliseconds == 90, "간격");
        });

        Console.WriteLine($"{count - failures}/{count} 통과");
        return failures == 0 ? 0 : 1;
    }
}
