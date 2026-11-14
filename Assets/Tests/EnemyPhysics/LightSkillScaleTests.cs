#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class LightSkillScaleTests
{
    [TestCase(10011)]
    [TestCase(20007)]
    [TestCase(20009)]
    public void RangeCardScalesBakedArtAtEveryLevel(int skillId)
    {
        Assert.IsTrue(CombatSkillTable.TryGet(skillId, out var record));
        Assert.IsTrue(LightSkillCast.TryFrom(record, out var skill));
        Assert.IsNotNull(LightSkillVisuals.Baked(LightSkillVisuals.BakedClipName(skillId)), "실제 구운 그림이 필요합니다.");
        var root = CreateFx();
        try
        {
            var fx = root.GetComponent<LightSkillFx>();
            fx.Play(skill, Vector2.right, 1f, "Enemy");
            Vector3 baseline = root.transform.localScale;
            for (int level = 1; level <= 5; level++)
            {
                fx.OnPoolReturn();
                fx.OnPoolRent();
                float multiplier = 1f + level * 0.1f;
                fx.Play(skill.WithStatUpgrades(0, multiplier), Vector2.right, 1f, "Enemy");
                Assert.That(root.transform.localScale.x, Is.EqualTo(baseline.x * multiplier).Within(0.0001f));
                Assert.That(root.transform.localScale.y, Is.EqualTo(baseline.y * multiplier).Within(0.0001f));
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    [TestCase(10006)]
    [TestCase(10009)]
    public void ProjectileSizeCardScalesArtAndWorldHitRadiusAtEveryLevel(int skillId)
    {
        Assert.IsTrue(CombatSkillTable.TryGet(skillId, out var record));
        Assert.IsTrue(LightSkillCast.TryFrom(record, out var skill));
        Assert.IsNotNull(LightSkillVisuals.Baked(LightSkillVisuals.BakedClipName(skillId)), "실제 구운 그림이 필요합니다.");
        var root = CreateFx();
        try
        {
            var fx = root.GetComponent<LightSkillFx>();
            var collider = root.GetComponent<CircleCollider2D>();
            fx.Play(skill, Vector2.right, 1f, "Enemy");
            Vector3 baseline = root.transform.localScale;
            float hitRadius = collider.radius * root.transform.lossyScale.x;
            for (int level = 1; level <= 5; level++)
            {
                fx.OnPoolReturn();
                fx.OnPoolRent();
                float multiplier = 1f + level * 0.1f;
                fx.Play(skill, Vector2.right, 1f, "Enemy", multiplier);
                Assert.That(root.transform.localScale.x, Is.EqualTo(baseline.x * multiplier).Within(0.0001f));
                Assert.That(root.transform.localScale.y, Is.EqualTo(baseline.y * multiplier).Within(0.0001f));
                Assert.That(collider.radius * root.transform.lossyScale.x,
                    Is.EqualTo(hitRadius * multiplier).Within(0.0001f));
            }
            // 같은 풀 객체가 다른 스킬로 재사용돼도 커진 크기가 남지 않아야 합니다.
            fx.Play(skill, Vector2.right, 1f, "Enemy", 1f);
            Assert.That(root.transform.localScale.x, Is.EqualTo(baseline.x).Within(0.0001f));
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static GameObject CreateFx()
    {
        var root = new GameObject("Skill Scale Test");
        root.SetActive(false);
        root.AddComponent<SpriteRenderer>();
        root.AddComponent<CircleCollider2D>();
        root.AddComponent<LightSkillFx>();
        return root;
    }
}
#endif
