#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BasicBulletVisualTests
{
    [Test]
    public void OnlyTheBasicPrefabUsesTheNewVisual()
    {
        var basic = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/Bullet.prefab");
        var legacy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/SkillProjectiles/FlameBullet.prefab");
        Assert.IsNotNull(basic.GetComponent<BasicBulletVisual>());
        Assert.IsNull(legacy.GetComponent<BasicBulletVisual>());
    }

    [Test]
    public void FlightPreservesTheColliderAndResetsOnReuse()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/1.Prefabs/Bullet.prefab");
        var root = Object.Instantiate(prefab);
        try
        {
            var bullet = root.GetComponent<Bullet>();
            Vector3 scale = root.transform.localScale;
            float radius = root.GetComponent<CircleCollider2D>().radius;
            bullet.InitDirection(Vector2.right, 7f, 5f);
            var renderer = root.GetComponentInChildren<SpriteRenderer>();
            Assert.IsNotNull(renderer.sprite);
            Assert.IsFalse(root.GetComponent<MeshRenderer>().enabled);
            Assert.AreEqual(scale, root.transform.localScale);
            Assert.AreEqual(radius, root.GetComponent<CircleCollider2D>().radius);
            Assert.That(Vector3.Dot(renderer.transform.right, Vector3.right), Is.GreaterThan(.99f));
            var visual = root.GetComponent<BasicBulletVisual>();
            var tick = typeof(BasicBulletVisual).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
            Sprite first = renderer.sprite;
            tick.Invoke(visual, new object[] { .1f });
            Assert.AreNotSame(first, renderer.sprite);
            Sprite advanced = renderer.sprite;
            tick.Invoke(visual, new object[] { 0f });
            Assert.AreSame(advanced, renderer.sprite);
            tick.Invoke(visual, new object[] { .4f });
            Assert.AreSame(advanced, renderer.sprite);
            bullet.OnPoolReturn();
            Assert.IsFalse(renderer.enabled);
            bullet.OnPoolRent();
            bullet.InitDirection(Vector2.up, 11f, 9f);
            Assert.AreSame(first, renderer.sprite);
            Assert.That(Vector3.Dot(renderer.transform.right, Vector3.up), Is.GreaterThan(.99f));
            Assert.AreEqual(1, root.GetComponentsInChildren<SpriteRenderer>(true).Length);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void FlightLoopsAndTheSeparateHitAnimationEndsQuickly()
    {
        var flight = LightSkillVisuals.Baked("BasicLightBolt");
        var hit = LightSkillVisuals.Baked("BasicLightHit");
        Assert.IsNotNull(flight);
        Assert.IsNotNull(hit);
        Assert.IsTrue(flight.Loop);
        Assert.IsFalse(hit.Loop);
        Assert.That(flight.Frames.Length, Is.GreaterThan(1));
        Assert.That(hit.Duration, Is.GreaterThan(0f).And.LessThan(.3f));
    }
}
#endif
