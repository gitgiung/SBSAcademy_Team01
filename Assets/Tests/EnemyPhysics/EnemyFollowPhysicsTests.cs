using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class EnemyFollowPhysicsTests
{
    private Scene scene;
    private PhysicsScene2D physics;
    private const float Step = 0.02f;
    private float previousCaptureDeltaTime;

    [SetUp]
    public void SetUp()
    {
        previousCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = Step;
        scene = SceneManager.CreateScene("Enemy physics regression", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        physics = scene.GetPhysicsScene2D();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);
        yield return SceneManager.UnloadSceneAsync(scene);
        Time.captureDeltaTime = previousCaptureDeltaTime;
    }

    private GameObject Create(string name, Vector2 position)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = position;
        return go;
    }

    private EnemyFollow CreateEnemy()
    {
        return Create("enemy or boss", Vector2.zero).AddComponent<EnemyFollow>();
    }

    private Rigidbody2D CreatePlayer(Vector2 position, RigidbodyType2D type = RigidbodyType2D.Kinematic)
    {
        GameObject player = Create("player", position);
        var body = player.AddComponent<Rigidbody2D>();
        body.bodyType = type;
        body.gravityScale = 0;
        player.AddComponent<BoxCollider2D>();
        return body;
    }

    private static void FollowStep(EnemyFollow follow)
    {
        typeof(EnemyFollow).GetMethod("FixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(follow, null);
    }

    [TestCase(true, RigidbodyType2D.Kinematic)]
    [TestCase(false, RigidbodyType2D.Kinematic)]
    [TestCase(true, RigidbodyType2D.Dynamic)]
    [TestCase(false, RigidbodyType2D.Dynamic)]
    public void PlayerCannotPushEnemyEvenWhenFollowIsDisabledForBossAttack(bool following, RigidbodyType2D playerType)
    {
        EnemyFollow follow = CreateEnemy();
        var body = follow.GetComponent<Rigidbody2D>();
        follow.enabled = following;
        var player = CreatePlayer(new Vector2(-1.2f, 0), playerType);
        var movement = new CollisionMovement2D(player);
        for (int i = 0; i < 40; i++)
        {
            if (following) FollowStep(follow);
            movement.Move(Vector2.right * 0.04f);
            physics.Simulate(Step);
        }
        Assert.That(body.position.sqrMagnitude, Is.LessThan(0.000001f), "Player contact must not move an idle enemy/boss.");
        Assert.That(player.position.x, Is.LessThanOrEqualTo(-0.999f), "The player must remain on the near side of the enemy.");
    }

    [TestCase(false, RigidbodyType2D.Kinematic)]
    [TestCase(true, RigidbodyType2D.Kinematic)]
    [TestCase(false, RigidbodyType2D.Dynamic)]
    [TestCase(true, RigidbodyType2D.Dynamic)]
    public void ContactDamageStillWorksForPlayer(bool trigger, RigidbodyType2D playerType)
    {
        EnemyFollow follow = CreateEnemy();
        follow.GetComponent<BoxCollider2D>().isTrigger = trigger;
        follow.gameObject.AddComponent<EnemyContactDamage>();
        var player = CreatePlayer(new Vector2(0.8f, 0), playerType);
        var receiver = player.gameObject.AddComponent<EnemyPhysicsDamageReceiver>();
        for (int i = 0; i < 5; i++) physics.Simulate(Step);
        Assert.That(receiver.Hits, Is.EqualTo(1), "Contact callbacks and the damage cooldown must both remain active.");
        Assert.That(receiver.Damage, Is.EqualTo(10f));
    }

    [Test]
    public void FollowingStillMovesAndStopsAtConfiguredDistance()
    {
        EnemyFollow follow = CreateEnemy();
        var body = follow.GetComponent<Rigidbody2D>();
        follow.SetTarget(Create("target", new Vector2(2, 0)).transform, 0.5f);
        for (int i = 0; i < 80; i++)
        {
            FollowStep(follow);
            physics.Simulate(Step);
        }
        Assert.That(body.position.x, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(body.position.y, Is.Zero.Within(0.001f));
    }

    [Test]
    public void BossCanMoveBodyDuringDashWithFollowDisabledAndAfterReuse()
    {
        EnemyFollow follow = CreateEnemy();
        var body = follow.GetComponent<Rigidbody2D>();
        follow.enabled = false;
        follow.MoveTo(Vector2.right * 0.1f);
        physics.Simulate(Step);
        Assert.That(body.position.x, Is.EqualTo(0.1f).Within(0.001f));
        follow.gameObject.SetActive(false);
        follow.gameObject.SetActive(true);
        body.AddForce(Vector2.left * 100, ForceMode2D.Impulse);
        physics.Simulate(Step);
        Assert.That(body.position.x, Is.EqualTo(0.1f).Within(0.001f), "Reuse must preserve resistance to external forces.");
    }

    [UnityTest]
    public IEnumerator KeyboardMovementCannotPassOrPushEnemy() => RunPlayerMovement(false);

    [UnityTest]
    public IEnumerator DashCannotPassOrPushEnemy() => RunPlayerMovement(true);

    private IEnumerator RunPlayerMovement(bool dash)
    {
        var enemy = CreateEnemy().GetComponent<Rigidbody2D>();
        var player = CreatePlayer(new Vector2(-2, 0));
        var dataType = System.Type.GetType("PlayerData, Assembly-CSharp");
        var movementType = System.Type.GetType("PlayerMovement, Assembly-CSharp");
        Assert.That(dataType, Is.Not.Null);
        Assert.That(movementType, Is.Not.Null);
        Component data = player.gameObject.AddComponent(dataType);
        object movement = System.Activator.CreateInstance(movementType,
            new object[] { player.transform, data, (System.Func<float>)(() => 1f) });
        yield return null;
        if (dash)
        {
            movementType.GetMethod("TryBeginDash").Invoke(movement, new object[] { Vector2.right, 0.15f, 15f, 0f });
            var routine = (IEnumerator)movementType.GetMethod("DashRoutine").Invoke(movement, null);
            for (int i = 0; i < 100 && routine.MoveNext(); i++)
            {
                physics.Simulate(Step);
                yield return null;
            }
        }
        else
        {
            for (int i = 0; i < 100; i++)
            {
                movementType.GetMethod("Tick").Invoke(movement, new object[] { Vector2.right });
                Physics2D.SyncTransforms();
                physics.Simulate(Step);
                yield return null;
            }
        }
        Assert.That(player.position.x, Is.LessThanOrEqualTo(-0.999f), "Keyboard movement/dash must stop at the enemy body.");
        Assert.That(player.position.x, Is.GreaterThan(-1.1f), "Movement must actually reach the obstacle, not remain at its initial position.");
        Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(-0.999f));
        Assert.That(enemy.position.sqrMagnitude, Is.LessThan(0.000001f));
    }

    [Test]
    public void FollowingCannotEnterThePlayersSolidBody()
    {
        var follow = CreateEnemy();
        var player = CreatePlayer(new Vector2(2, 0));
        follow.SetTarget(player.transform, 0.1f);
        for (int i = 0; i < 100; i++)
        {
            FollowStep(follow);
            physics.Simulate(Step);
        }
        Assert.That(follow.GetComponent<Rigidbody2D>().position.x, Is.LessThanOrEqualTo(1.001f));
    }

    [Test]
    public void FollowingCannotPassThroughAnotherEnemy()
    {
        var follow = CreateEnemy();
        var blocker = Create("blocking enemy", new Vector2(2, 0)).AddComponent<EnemyFollow>();
        follow.SetTarget(Create("target", new Vector2(4, 0)).transform, 0);
        for (int i = 0; i < 100; i++)
        {
            FollowStep(follow);
            physics.Simulate(Step);
        }
        Assert.That(follow.GetComponent<Rigidbody2D>().position.x, Is.LessThanOrEqualTo(1.001f));
        Assert.That(blocker.GetComponent<Rigidbody2D>().position.x, Is.EqualTo(2f).Within(0.001f));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StoppingAtBodyStillAppliesContactDamageAndIgnoresTriggerRadius(bool addTrigger)
    {
        var follow = CreateEnemy();
        follow.gameObject.AddComponent<EnemyContactDamage>();
        if (addTrigger)
        {
            var trigger = follow.gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 2;
        }
        var player = CreatePlayer(new Vector2(-3, 0));
        var receiver = player.gameObject.AddComponent<EnemyPhysicsDamageReceiver>();
        new CollisionMovement2D(player).Move(Vector2.right * 10);
        for (int i = 0; i < 5; i++) physics.Simulate(Step);
        Assert.That(player.position.x, Is.EqualTo(-1.001f).Within(0.01f));
        Assert.That(receiver.Hits, Is.EqualTo(1));
        Assert.That(follow.GetComponent<Rigidbody2D>().position, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void MovementSlidesAlongWallAndCanRetreatFromContact()
    {
        var wall = Create("wall", Vector2.zero).AddComponent<BoxCollider2D>();
        wall.size = new Vector2(1, 10);
        var player = CreatePlayer(new Vector2(-2, -2));
        var movement = new CollisionMovement2D(player);
        movement.Move(new Vector2(2, 2));
        Assert.That(player.position.x, Is.LessThanOrEqualTo(-0.999f));
        Assert.That(player.position.y, Is.EqualTo(0).Within(0.001f));
        float contactX = player.position.x;
        movement.Move(Vector2.left);
        Assert.That(player.position.x, Is.LessThan(contactX - 0.99f));
    }

    [Test]
    public void BossDashMovementStopsAtPlayerWhileFollowIsDisabled()
    {
        var follow = CreateEnemy();
        follow.enabled = false;
        var player = CreatePlayer(new Vector2(2, 0));
        follow.MoveTo(Vector2.right * 10);
        physics.Simulate(Step);
        Assert.That(follow.GetComponent<Rigidbody2D>().position.x, Is.EqualTo(0.999f).Within(0.01f));
        Assert.That(player.position.x, Is.EqualTo(2f).Within(0.001f));
    }
}
