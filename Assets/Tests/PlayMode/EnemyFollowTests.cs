#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyFollowTests
{
    private GameObject enemy;
    private GameObject target;
    private EnemyFollow follow;

    [SetUp]
    public void SetUp()
    {
        enemy = new GameObject("Enemy");
        follow = enemy.AddComponent<EnemyFollow>();
        target = new GameObject("Target");
        target.transform.position = new Vector3(3, 0, 0);
        follow.SetTarget(target.transform);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(target);
    }

    [UnityTest]
    public IEnumerator FollowsAndChangesDirectionWhenTargetMoves()
    {
        yield return Steps(4);
        Assert.That(enemy.transform.position.x, Is.GreaterThan(0));
        float before = enemy.transform.position.x;
        target.transform.position = new Vector3(-3, 0, 0);
        yield return Steps(4);
        Assert.That(enemy.transform.position.x, Is.LessThan(before));
        Assert.That(enemy.transform.position.z, Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator StopsWithoutOvershootAndResumesWhenTargetMovesAway()
    {
        SetFloat("moveSpeed", 100);
        target.transform.position = new Vector3(0.6f, 0, 0);
        yield return Steps(4);
        Assert.That(Vector2.Distance(enemy.transform.position, target.transform.position),
            Is.EqualTo(0.5f).Within(0.001f));
        float before = enemy.transform.position.x;
        target.transform.position = new Vector3(4, 0, 0);
        yield return Steps(4);
        Assert.That(enemy.transform.position.x, Is.GreaterThan(before));
    }

    [UnityTest]
    public IEnumerator StopsWhenTargetIsInactiveAndCanBeReassigned()
    {
        yield return Steps(3);
        target.SetActive(false);
        yield return Steps(2);
        Vector3 stopped = enemy.transform.position;
        yield return Steps(3);
        Assert.That(enemy.transform.position, Is.EqualTo(stopped));
        target.SetActive(true);
        follow.SetTarget(target.transform);
        yield return Steps(3);
        Assert.That(enemy.transform.position.x, Is.GreaterThan(stopped.x));
    }

    [UnityTest]
    public IEnumerator StopsWhenTargetIsDestroyed()
    {
        yield return Steps(3);
        Object.DestroyImmediate(target);
        yield return Steps(2);
        Vector3 stopped = enemy.transform.position;
        yield return Steps(3);
        Assert.That(enemy.transform.position, Is.EqualTo(stopped));
    }

    [UnityTest]
    public IEnumerator ZeroSpeedDoesNotMove()
    {
        SetFloat("moveSpeed", 0);
        yield return Steps(3);
        Assert.That(enemy.transform.position, Is.EqualTo(Vector3.zero));
    }

    private void SetFloat(string name, float value)
    {
        var settings = new SerializedObject(follow);
        settings.FindProperty(name).floatValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static IEnumerator Steps(int count)
    {
        for (int i = 0; i < count; i++)
            yield return new WaitForFixedUpdate();
    }
}
#endif
