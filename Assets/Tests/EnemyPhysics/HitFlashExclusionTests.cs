using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class HitFlashExclusionTests
{
    [Test]
    public void FlashAndRestorePreserveExcludedVfxColor()
    {
        var root = new GameObject("Hit Flash Exclusion Test");
        root.SetActive(false);
        try
        {
            var body = root.AddComponent<SpriteRenderer>();
            body.color = Color.green;
            var child = new GameObject("Attack VFX");
            child.transform.SetParent(root.transform);
            var vfx = child.AddComponent<SpriteRenderer>();
            child.AddComponent<HitFlashExcluded>();
            Color originalVfx = new Color(1f, 0.35f, 0.08f, 0.4f);
            vfx.color = originalVfx;
            var health = root.AddComponent<EnemyHealth>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(EnemyHealth).GetMethod("EnsureInitialized", flags).Invoke(health, null);
            var flash = (IEnumerator)typeof(EnemyHealth).GetMethod("Flash", flags).Invoke(health, null);
            Assert.IsTrue(flash.MoveNext());
            Assert.AreEqual(Color.white, body.color);
            Assert.AreEqual(originalVfx, vfx.color);
            Assert.IsFalse(flash.MoveNext());
            Assert.AreEqual(Color.green, body.color);
            Assert.AreEqual(originalVfx, vfx.color);
            health.PrepareForSpawn(null);
            Assert.AreEqual(originalVfx, vfx.color);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
