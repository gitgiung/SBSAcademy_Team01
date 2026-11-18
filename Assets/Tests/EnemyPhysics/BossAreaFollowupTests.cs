#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BossAreaFollowupTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private GameObject target;
    private BossController boss;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Boss Area Followup Test");
        root.SetActive(false);
        target = new GameObject("Boss Target");
        boss = root.AddComponent<BossController>();
        SetField("health", root.GetComponent<EnemyHealth>());
        SetField("target", target.transform);
        SetField("closeRange", 4f);
        SetField("specialPatternDistance", 14.01f);
        SetPhase(2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(target);
    }

    [Test]
    public void DistanceAloneNeverTriggersArea()
    {
        SetField("nextNormalPattern", BossPattern.Pattern2);
        Assert.AreEqual(BossPattern.Pattern3, SelectPattern(20f));
        Assert.AreEqual(BossPattern.None, SelectPattern(7f));
        Assert.AreEqual(BossPattern.Pattern2, SelectPattern(3f));
    }

    [TestCase(1, false, BossPattern.Pattern1)]
    [TestCase(2, true, BossPattern.Pattern1)]
    [TestCase(2, false, BossPattern.PhaseTwoArea)]
    public void AreaRequiresACompletedPhaseTwoComboWithoutDamageOrParry(
        int phase, bool dealtDamageOrWasParried, BossPattern expected)
    {
        SetPhase(phase);
        SetField("patternSelector", root.AddComponent<BossPatternRuleSelector>());
        SetField("normalComboDealtDamageOrWasParried", dealtDamageOrWasParried);
        CompleteCombo(true);
        Assert.AreEqual(expected, SelectPattern(3f));
        if (expected == BossPattern.PhaseTwoArea)
            Assert.AreEqual(expected, SelectPattern(20f));
    }

    [Test]
    public void MissedComboWaitsForAreaCooldown()
    {
        SetField("missedComboAreaPending", true);
        SetField("nextAreaPatternTime", float.PositiveInfinity);
        Assert.AreEqual(BossPattern.None, SelectPattern(20f));
        SetField("nextAreaPatternTime", 0f);
        Assert.AreEqual(BossPattern.PhaseTwoArea, SelectPattern(20f));
    }

    [Test]
    public void PhaseEntryAreaRunsWithoutCompletedCombo()
    {
        SetField("phaseEntryAreaPending", true);
        Assert.AreEqual(BossPattern.PhaseTwoArea, SelectPattern(7f));
        Assert.AreEqual(BossPattern.None, SelectPattern(7f));
    }

    [Test]
    public void MissingSwipeImpactDoesNotReserveArea()
    {
        CompleteCombo(false);
        Assert.AreEqual(BossPattern.None, SelectPattern(7f));
    }

    [Test]
    public void SlashChecksAllCollidersBeforeDeduplicatingReceiver()
    {
        var receiver = target.AddComponent<BossSlashTestReceiver>();
        AddCollider("Outside arc", new Vector3(0f, 1f, -1f));
        AddCollider("Inside arc", Vector3.right);
        SetField("body", root.GetComponent<Rigidbody2D>());
        Physics2D.SyncTransforms();

        typeof(BossController).GetMethod("PerformForwardSlash", PrivateInstance)
            .Invoke(boss, new object[] { Vector2.right, 2f });

        Assert.AreEqual(1, receiver.Hits);
    }

    [Test]
    public void DashStopsSafelyWhenTargetIsDestroyedDuringPhysicsWait()
    {
        SetField("body", root.GetComponent<Rigidbody2D>());
        SetField("follow", root.GetComponent<EnemyFollow>());
        target.transform.position = Vector3.right * 20f;
        var dash = (System.Collections.IEnumerator)typeof(BossController)
            .GetMethod("DashToTrackedTarget", PrivateInstance).Invoke(boss, null);
        Assert.IsTrue(dash.MoveNext());
        Object.DestroyImmediate(target);
        Assert.DoesNotThrow(() => Assert.IsFalse(dash.MoveNext()));
    }

    [Test]
    public void PhaseResetRestoresBaseMovementSpeed()
    {
        var follow = root.GetComponent<EnemyFollow>();
        follow.moveSpeed = 2f;
        root.GetComponent<EnemyHealth>().EnsureInitialized();
        typeof(BossController).GetMethod("Awake", PrivateInstance).Invoke(boss, null);
        typeof(BossController).GetMethod("UpdatePhase", PrivateInstance).Invoke(boss, null);
        SetPhase(2);
        follow.moveSpeed = 3f;
        root.GetComponent<EnemyHealth>().PrepareForSpawn(null);
        typeof(BossController).GetMethod("UpdatePhase", PrivateInstance).Invoke(boss, null);
        Assert.AreEqual(1, boss.CurrentPhase);
        Assert.AreEqual(2f, follow.moveSpeed);
    }

    private void CompleteCombo(bool allSwipesResolved)
    {
        var complete = typeof(BossController).GetMethod("CompleteNormalCombo", PrivateInstance);
        complete.Invoke(boss, new object[] { allSwipesResolved });
    }

    private void AddCollider(string name, Vector3 position)
    {
        var child = new GameObject(name);
        child.transform.SetParent(target.transform);
        child.transform.localPosition = position;
        child.AddComponent<CircleCollider2D>().radius = .25f;
    }


    private BossPattern SelectPattern(float distance)
    {
        return (BossPattern)typeof(BossController).GetMethod("SelectAvailablePattern", PrivateInstance)
            .Invoke(boss, new object[] { distance });
    }

    private void SetPhase(int phase)
    {
        typeof(BossController).GetProperty(nameof(BossController.CurrentPhase)).SetValue(boss, phase);
    }

    private void SetField(string name, object value)
    {
        typeof(BossController).GetField(name, PrivateInstance).SetValue(boss, value);
    }
}
public sealed class BossSlashTestReceiver : MonoBehaviour, IContactDamageReceiver
{
    public int Hits { get; private set; }

    public void TakeContactDamage(float damage, GameObject source) => Hits++;
}
#endif
