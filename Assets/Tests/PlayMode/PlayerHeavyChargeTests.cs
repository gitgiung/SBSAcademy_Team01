using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class PlayerHeavyChargeTests
{
    private GameObject actor;
    private Component data;
    private object charge;
    private object settings;
    private bool valid;
    private static Type Runtime(string name)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);
        Assert.That(type, Is.Not.Null, $"Missing charge foundation: {name}"); return type;
    }
    private static object Call(object target, string name, params object[] args) => target.GetType()
        .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static T Read<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
    [SetUp]
    public void Setup()
    {
        Time.timeScale = 1; valid = true;
        actor = new GameObject("charge data"); data = actor.AddComponent(Runtime("PlayerData"));
        Set(data, "maxMP", 100f); Call(data, "InitMP");
        settings = Activator.CreateInstance(Runtime("PlayerHeavyChargeSettings"));
        Set(settings, "Configured", true); Set(settings, "MPCost", 10f);
        Set(settings, "StageTimes", new[] { 0f, .4f, .8f });
        Set(settings, "StageBonuses", new[] { .1f, .1f, .5f });
        charge = Activator.CreateInstance(Runtime("PlayerHeavyCharge"), data, (Func<bool>)(() => valid));
    }
    private bool Begin() => (bool)Call(charge, "TryBegin", settings, Vector2.right);
    private int Id => Read<int>(charge, "CurrentId");
    private bool Release(int id, out object result)
    {
        object[] args = { id, Activator.CreateInstance(Runtime("HeavyChargeRelease")) };
        bool accepted = (bool)Call(charge, "TryRelease", args); result = args[1]; return accepted;
    }
    [UnityTearDown]
    public IEnumerator Cleanup() { Time.timeScale = 1; if (actor != null) Object.Destroy(actor); yield return null; }
    [Test]
    public void ChargesCumulativeStagesAndSpendsMPOnlyAtStart()
    {
        Assert.That(Begin(), Is.True); Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90));
        Assert.That(Read<int>(charge, "Stage"), Is.EqualTo(1)); Assert.That(Read<float>(charge, "Multiplier"), Is.EqualTo(1.1f).Within(.001f));
        Assert.That(Begin(), Is.False);
        Call(charge, "Tick", .4f, Vector2.up); Assert.That(Read<int>(charge, "Stage"), Is.EqualTo(2));
        Assert.That(Read<float>(charge, "Multiplier"), Is.EqualTo(1.2f).Within(.001f));
        Call(charge, "Tick", 10f, Vector2.up); Assert.That(Read<int>(charge, "Stage"), Is.EqualTo(3));
        Assert.That(Read<float>(charge, "Multiplier"), Is.EqualTo(1.7f).Within(.001f));
        Assert.That(Read<float>(charge, "Elapsed"), Is.EqualTo(.8f)); Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90));
    }
    [Test]
    public void ReleaseSnapshotsDirectionStageAndRejectsDuplicateOrOldId()
    {
        Begin(); int old = Id; Call(charge, "Tick", .5f, Vector2.up);
        Assert.That(Release(old, out object result), Is.True);
        Assert.That(Read<int>(result, "Stage"), Is.EqualTo(2)); Assert.That(Read<Vector2>(result, "Direction"), Is.EqualTo(Vector2.up));
        Assert.That(Read<float>(result, "Multiplier"), Is.EqualTo(1.2f).Within(.001f));
        Assert.That(Release(old, out _), Is.False); Assert.That(Begin(), Is.True);
        Assert.That(Release(old, out _), Is.False); Assert.That(Read<bool>(charge, "IsCharging"), Is.True);
    }
    [Test]
    public void PausedChargeFreezesAndCannotReleaseUntilResumed()
    {
        Begin(); Call(charge, "Tick", .3f, Vector2.right); Time.timeScale = 0;
        Call(charge, "Tick", 5f, Vector2.up); Assert.That(Read<float>(charge, "Elapsed"), Is.EqualTo(.3f));
        Assert.That(Release(Id, out _), Is.False); Time.timeScale = 1;
        Assert.That(Release(Id, out _), Is.True);
    }
    [Test]
    public void CancelDoesNotRefundAndLateReleaseDoesNotStartAttack()
    {
        Begin(); int old = Id; Call(charge, "Cancel"); Assert.That(Read<bool>(charge, "IsCharging"), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90)); Assert.That(Release(old, out _), Is.False);
    }
    [Test]
    public void ConfigurationIsCapturedBeforeMPChangedCallbacks()
    {
        data.GetType().GetEvent("MPChanged").AddEventHandler(data, (Action<float, float>)((_, __) =>
        { Set(settings, "StageTimes", new[] { 0f, 4f, 8f }); Set(settings, "StageBonuses", new[] { 5f, 5f, 5f }); }));
        Begin(); Call(charge, "Tick", .8f, Vector2.right);
        Assert.That(Read<float>(charge, "Multiplier"), Is.EqualTo(1.7f).Within(.001f));
    }
    [Test]
    public void InterruptedStartRefundsOnlyUnstartedCostAndRejectsReentry()
    {
        bool nested = true;
        data.GetType().GetEvent("MPChanged").AddEventHandler(data, (Action<float, float>)((current, _) =>
        { if (current == 90) { nested = Begin(); valid = false; Call(charge, "Cancel"); } }));
        Assert.That(Begin(), Is.False); Assert.That(nested, Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100)); Assert.That(Read<bool>(charge, "IsCharging"), Is.False);
    }
    [Test]
    public void InsufficientMPNeverBegins()
    { Call(data, "TrySpendMP", 95f); Assert.That(Begin(), Is.False); Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(5)); }
    [Test]
    public void UnconfiguredOrUnorderedStagesNeverSpendMP()
    {
        Set(settings, "Configured", false); Assert.That(Begin(), Is.False);
        Set(settings, "Configured", true); Set(settings, "StageTimes", new[] { 0f, .8f, .4f }); Assert.That(Begin(), Is.False);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100));
    }
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-1f)]
    public void InvalidCostNeverSpendsMP(float cost)
    { Set(settings, "MPCost", cost); Assert.That(Begin(), Is.False); Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(100)); }
    [Test]
    public void DeadOrInvalidOwnerCancelsAndClearsStageWithoutRefund()
    {
        Begin(); valid = false; Call(charge, "Tick", .2f, Vector2.right);
        Assert.That(Read<bool>(charge, "IsCharging"), Is.False); Assert.That(Read<int>(charge, "Stage"), Is.Zero);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(90));
    }
}
