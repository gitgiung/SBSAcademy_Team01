using NUnit.Framework;
using UnityEngine;

public sealed class HordeLordRulesTests
{
    [Test]
    public void HalfHealthStartsPhaseTwo()
    {
        Assert.IsFalse(HordeLordRules.IsPhaseTwo(13f, 25f));
        Assert.IsTrue(HordeLordRules.IsPhaseTwo(12.5f, 25f));
        Assert.IsTrue(HordeLordRules.IsPhaseTwo(1f, 25f));
        Assert.IsFalse(HordeLordRules.IsPhaseTwo(25f, 0f));
    }

    [Test]
    public void PhaseTwoSummonsMoreAndThrowsASpread()
    {
        Assert.AreEqual(3, HordeLordRules.SummonCount(false));
        Assert.AreEqual(5, HordeLordRules.SummonCount(true));
        Assert.AreEqual(1, HordeLordRules.OrbCount(false));
        Assert.AreEqual(3, HordeLordRules.OrbCount(true));
        Assert.AreEqual(6, HordeLordRules.AliveCap(false));
        Assert.AreEqual(10, HordeLordRules.AliveCap(true));
    }

    [Test]
    public void StanceHoldsABandAndStepsBackWhenClose()
    {
        Assert.AreEqual(HordeLordRules.Stance.Retreat, HordeLordRules.ChooseStance(2f, 3.5f, 7f));
        Assert.AreEqual(HordeLordRules.Stance.Hold, HordeLordRules.ChooseStance(5f, 3.5f, 7f));
        Assert.AreEqual(HordeLordRules.Stance.Advance, HordeLordRules.ChooseStance(9f, 3.5f, 7f));
    }

    [Test]
    public void SummonsSpreadInFrontAndOrbsLeaveTheCenter()
    {
        Vector2 forward = Vector2.down;
        Vector2 middle = HordeLordRules.SummonOffset(1, 3, forward, 1.6f);
        Assert.AreEqual(0f, middle.x, 0.001f);
        Assert.Less(middle.y, 0f);

        Vector2 left = HordeLordRules.SummonOffset(0, 3, forward, 1.6f);
        Vector2 right = HordeLordRules.SummonOffset(2, 3, forward, 1.6f);
        Assert.Less(left.x, 0f);
        Assert.Greater(right.x, 0f);

        Vector2 centerOrb = HordeLordRules.OrbDirection(1, 3, Vector2.up);
        Assert.AreEqual(0f, centerOrb.x, 0.001f);
        Assert.Greater(centerOrb.y, 0.9f);
    }
}
