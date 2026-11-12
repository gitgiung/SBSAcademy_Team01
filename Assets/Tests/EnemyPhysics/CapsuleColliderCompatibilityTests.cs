#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CapsuleColliderCompatibilityTests
{
    private readonly List<GameObject> objects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = objects.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }

    [Test]
    public void CapsuleEnemyFollowDoesNotAddAnUnwantedBox()
    {
        GameObject actor = CreateActor();
        actor.AddComponent<CapsuleCollider2D>();
        Assert.That(actor.AddComponent<EnemyFollow>(), Is.Not.Null);

        Assert.That(actor.GetComponent<Rigidbody2D>(), Is.Not.Null);
        Assert.That(actor.GetComponent<BoxCollider2D>(), Is.Null,
            "EnemyFollow must preserve a capsule body without adding a second solid shape.");
    }

    [TestCase(CapsuleDirection2D.Vertical)]
    [TestCase(CapsuleDirection2D.Horizontal)]
    public void CapsuleBodyStopsAtWallAndCanMoveAway(CapsuleDirection2D direction)
    {
        GameObject actor = CreateActor();
        CapsuleCollider2D capsule = actor.AddComponent<CapsuleCollider2D>();
        capsule.direction = direction;
        capsule.size = direction == CapsuleDirection2D.Vertical
            ? new Vector2(1f, 2f) : new Vector2(2f, 1f);
        Rigidbody2D body = actor.GetComponent<Rigidbody2D>();
        CreateWall(false);
        Physics2D.SyncTransforms();
        var movement = new CollisionMovement2D(body);

        movement.Move(Vector2.right * 5f);
        float expectedX = 2.75f - capsule.size.x * 0.5f;
        Assert.That(body.position.x, Is.EqualTo(expectedX).Within(0.01f));

        float contactX = body.position.x;
        movement.Move(Vector2.left);
        Assert.That(body.position.x, Is.EqualTo(contactX - 1f).Within(0.01f));
    }

    [Test]
    public void CapsuleEnemyFollowMovesWithoutABox()
    {
        GameObject actor = CreateActor();
        actor.AddComponent<CapsuleCollider2D>().size = new Vector2(1f, 2f);
        EnemyFollow follow = actor.AddComponent<EnemyFollow>();
        // EditMode does not run the normal scene Awake lifecycle.
        typeof(EnemyFollow).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(follow, null);
        CreateWall(false);
        Physics2D.SyncTransforms();

        follow.MoveTo(Vector2.right * 5f);

        Assert.That(actor.GetComponent<Rigidbody2D>().position.x,
            Is.EqualTo(2.25f).Within(0.01f));
        Assert.That(actor.GetComponent<BoxCollider2D>(), Is.Null);
    }

    [Test]
    public void CapsuleBodyIgnoresTriggerObstacles()
    {
        GameObject actor = CreateActor();
        actor.AddComponent<CapsuleCollider2D>();
        CreateWall(true);
        Physics2D.SyncTransforms();
        Rigidbody2D body = actor.GetComponent<Rigidbody2D>();

        new CollisionMovement2D(body).Move(Vector2.right * 5f);

        Assert.That(body.position.x, Is.EqualTo(5f).Within(0.01f));
    }

    [Test]
    public void ExistingCircleBodyAndBoxTriggerStillWorkWithEnemyFollow()
    {
        GameObject actor = CreateActor();
        actor.AddComponent<CircleCollider2D>().radius = 0.5f;
        BoxCollider2D trigger = actor.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = Vector2.one * 4f;
        EnemyFollow follow = actor.AddComponent<EnemyFollow>();
        typeof(EnemyFollow).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(follow, null);
        CreateWall(false);
        Physics2D.SyncTransforms();

        follow.MoveTo(Vector2.right * 5f);

        Assert.That(actor.GetComponent<Rigidbody2D>().position.x,
            Is.EqualTo(2.25f).Within(0.01f));
        Assert.That(trigger.isTrigger, Is.True);
    }

    private GameObject CreateActor()
    {
        var actor = new GameObject("Capsule compatibility actor");
        objects.Add(actor);
        Rigidbody2D body = actor.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.useFullKinematicContacts = true;
        return actor;
    }

    private void CreateWall(bool isTrigger)
    {
        var wall = new GameObject("Capsule compatibility wall");
        objects.Add(wall);
        wall.transform.position = Vector3.right * 3f;
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.5f, 20f);
        collider.isTrigger = isTrigger;
    }
}
#endif
