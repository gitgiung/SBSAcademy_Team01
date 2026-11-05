using NUnit.Framework;
using UnityEngine;

public class BasicBulletTests
{
    private GameObject owner;
    private GameObject enemy;
    private GameObject prefab;
    private PlayerData data;
    private PlayerCombat combat;

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        Managers.Pool = new PoolManager();
        owner = new GameObject("Basic bullet owner");
        data = owner.AddComponent<PlayerData>();
        enemy = new GameObject("Basic bullet target");
        enemy.tag = "Respawn";
        enemy.transform.position = Vector3.right * 2f;
        enemy.AddComponent<CircleCollider2D>();
        prefab = new GameObject("Basic bullet prefab");
        prefab.SetActive(false);
        prefab.AddComponent<Bullet>();
        Physics2D.SyncTransforms();

        combat = new PlayerCombat(owner.transform, data, prefab, enemy.tag);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        Managers.Pool.Dispose();
        Managers.Pool = null;
        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(enemy);
        Object.DestroyImmediate(owner);
    }

    [Test]
    public void EmptyLoadoutFiresBasicBulletOnExistingCooldownWithoutSpendingMP()
    {
        data.maxMP = 100f;
        data.InitMP();
        data.attackCooldown = 1f;
        combat.Tick(.1f);
        Assert.That(Managers.Pool.GetActiveCount(prefab), Is.EqualTo(1));
        combat.Tick(.5f);
        Assert.That(Managers.Pool.GetActiveCount(prefab), Is.EqualTo(1));
        combat.Tick(.5f);
        Assert.That(Managers.Pool.GetActiveCount(prefab), Is.EqualTo(2));
        Assert.That(data.CurrentMP, Is.EqualTo(100f));
    }

    [Test]
    public void BasicBulletRequiresEnemyInRangeAndRunningCombat()
    {
        enemy.transform.position = Vector3.right * (data.attackRange + 2f);
        Physics2D.SyncTransforms();
        combat.Tick(1f);
        Assert.That(Managers.Pool.GetActiveCount(prefab), Is.Zero);

        enemy.transform.position = Vector3.right * 2f;
        Physics2D.SyncTransforms();
        Time.timeScale = 0f;
        combat.Tick(1f);
        Assert.That(Managers.Pool.GetActiveCount(prefab), Is.Zero);

        Time.timeScale = 1f;
        combat.Tick(.1f);
        Assert.That(Managers.Pool.GetActiveCount(prefab), Is.EqualTo(1));
    }
}
