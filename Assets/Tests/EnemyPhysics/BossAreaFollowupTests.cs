#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BossAreaFollowupTests
{
    [Test]
    public void AreaFollowupAllowsDashAndPreservesPendingNormalPattern()
    {
        var root = new GameObject("Boss Area Followup Test");
        root.SetActive(false);
        try
        {
            var boss = root.AddComponent<BossController>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BossController).GetField("closeRange", flags).SetValue(boss, 4f);
            typeof(BossController).GetField("specialPatternDistance", flags).SetValue(boss, 14.01f);
            typeof(BossController).GetField("areaFollowupPending", flags).SetValue(boss, true);
            var next = typeof(BossController).GetField("nextNormalPattern", flags);
            var select = typeof(BossController).GetMethod("SelectAvailablePattern", flags);
            foreach (var expected in new[] { BossPattern.Pattern1, BossPattern.Pattern2 })
            {
                next.SetValue(boss, expected);
                Assert.AreEqual(BossPattern.Pattern3, select.Invoke(boss, new object[] { 20f }));
                Assert.AreEqual(expected, next.GetValue(boss));
                Assert.AreEqual(BossPattern.None, select.Invoke(boss, new object[] { 7f }));
                Assert.AreEqual(expected, select.Invoke(boss, new object[] { 3f }));
            }
        }
        finally { Object.DestroyImmediate(root); }
    }
}
#endif
