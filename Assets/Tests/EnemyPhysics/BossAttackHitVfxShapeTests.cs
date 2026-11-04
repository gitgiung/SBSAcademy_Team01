using NUnit.Framework;
using UnityEngine;

public sealed class BossAttackHitVfxShapeTests
{
    [Test]
    public void SlashShapeMatchesHitRadiusAndAngle()
    {
        Assert.AreEqual(180f, BossAttackHitVfxShape.RadialStartAngle);
        Assert.AreEqual(135f, BossAttackHitVfxShape.RadialClip(90f), 0.001f);
        Assert.AreEqual(4f, BossAttackHitVfxShape.Diameter(2f), 0.001f);
        Assert.AreEqual(120f, BossAttackHitVfxShape.RadialClip(120f), 0.001f);
        Assert.AreEqual(12f, BossAttackHitVfxShape.Diameter(6f), 0.001f);
        Assert.AreEqual(0f, BossAttackHitVfxShape.RadialClip(360f), 0.001f);

        Vector3 scale = BossAttackHitVfxShape.LocalScale(6f, new Vector3(4f, 4f, 4f));
        Assert.AreEqual(3f, scale.x, 0.001f);
        Assert.AreEqual(3f, scale.y, 0.001f);
        Assert.AreEqual(0.25f, scale.z, 0.001f);
    }
}
