using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerBulletPoolingTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private object pool;
    private GameObject prefab;
    private Transform target;
    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies()
        .Select(assembly => assembly.GetType(name)).First(type => type != null);
    private static object Call(object instance, string name, params object[] args) =>
        instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(instance, args);
    private static void Set(object instance, string field, object value) =>
        instance.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(instance, value);

    private GameObject Create(string name)
    {
        var go = new GameObject(name);
        objects.Add(go);
        return go;
    }

    [SetUp]
    public void SetUp()
    {
        Time.timeScale = 1f;
        var camera = Create("bullet test camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 20f;
        camera.transform.position = new Vector3(0, 0, -10);
        pool = Activator.CreateInstance(RuntimeType("PoolManager"), new object[] { null });
        prefab = Create("bullet test prefab");
        prefab.SetActive(false);
        prefab.AddComponent(RuntimeType("Bullet"));
        target = Create("bullet test target").transform;
        target.position = Vector3.right * 5f;
    }

    private GameObject Rent(float lifetime = 6f, float damage = 1f, float speed = 0f)
    {
        Set(prefab.GetComponent(RuntimeType("Bullet")), "DeleteTime", lifetime);
        return (GameObject)Call(pool, "Pop", prefab, Vector3.zero, Quaternion.identity, null,
            (Action<GameObject>)(go => Call(go.GetComponent(RuntimeType("Bullet")), "Init", target, damage, speed)));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        ((IDisposable)pool).Dispose();
        foreach (var go in objects) if (go != null) UnityEngine.Object.Destroy(go);
        objects.Clear();
        var managers = GameObject.Find("@Managers");
        if (managers != null) UnityEngine.Object.Destroy(managers);
        yield return null;
    }

    [Test]
    public void PlayerCombatRentsFromExistingManagersPool()
    {
        var owner = Create("bullet test player");
        var data = owner.AddComponent(RuntimeType("PlayerData"));
        var combat = Activator.CreateInstance(RuntimeType("PlayerCombat"),
            owner.transform, data, prefab, "Enemy");
        Call(combat, "FireAt", target);
        var managersType = RuntimeType("Managers");
        var sharedPool = managersType.GetProperty("Pool").GetValue(null);
        Assert.That(Call(sharedPool, "GetActiveCount", prefab), Is.EqualTo(1),
            "Player fire must rent a bullet instead of instantiating an unowned clone.");
    }

    [Test]
    public void HitReturnsOnceAndRentedBulletCanDamageAgain()
    {
        var enemy = Create("bullet test enemy");
        enemy.tag = "Enemy";
        var collider = enemy.AddComponent<BoxCollider2D>();
        var health = enemy.AddComponent(RuntimeType("EnemyHealth"));
        GameObject bullet = Rent();
        Call(bullet.GetComponent(RuntimeType("Bullet")), "OnTriggerEnter2D", collider);
        Call(bullet.GetComponent(RuntimeType("Bullet")), "OnTriggerEnter2D", collider);
        Assert.That(Call(pool, "GetActiveCount", prefab), Is.EqualTo(0));
        Assert.That(Call(pool, "GetInactiveCount", prefab), Is.EqualTo(1));
        Assert.That((float)health.GetType().GetProperty("CurrentHealth").GetValue(health), Is.EqualTo(2f));
        GameObject reused = Rent();
        Assert.That(reused, Is.SameAs(bullet));
        Call(reused.GetComponent(RuntimeType("Bullet")), "OnTriggerEnter2D", collider);
        Assert.That((float)health.GetType().GetProperty("CurrentHealth").GetValue(health), Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator LifetimeReturnsAndNewLeaseGetsFullLifetime()
    {
        GameObject bullet = Rent(0.04f);
        yield return new WaitForSeconds(0.12f);
        Assert.That(bullet != null, Is.True, "Expired bullets must be retained in the pool.");
        Assert.That(bullet.activeSelf, Is.False);
        Set(bullet.GetComponent(RuntimeType("Bullet")), "DeleteTime", 0.5f);
        GameObject reused = Rent(0.5f);
        Assert.That(reused, Is.SameAs(bullet));
        yield return new WaitForSeconds(0.15f);
        Assert.That(reused != null && reused.activeSelf, Is.True,
            "A previous lease must not destroy or expire the next shot.");
    }

    [UnityTest]
    public IEnumerator LeavingViewportReturnsToPool()
    {
        GameObject bullet = Rent();
        bullet.transform.position = Vector3.right * 1000f;
        yield return null;
        yield return null;
        Assert.That(bullet != null && !bullet.activeSelf, Is.True);
        Assert.That(Call(pool, "GetInactiveCount", prefab), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator ShotKeepsItsDirectionAfterTargetMovesOrDies()
    {
        GameObject bullet = Rent(speed: 2f);
        target.position = Vector3.up * 5f;
        UnityEngine.Object.Destroy(target.gameObject);
        yield return null;
        yield return null;
        Assert.That(bullet != null && bullet.activeSelf, Is.True);
        Assert.That(bullet.transform.position.x, Is.GreaterThan(0f));
        Assert.That(bullet.transform.position.y, Is.EqualTo(0f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator MissingCameraDoesNotBreakLifetimeReturn()
    {
        UnityEngine.Object.Destroy(Camera.main.gameObject);
        yield return null;
        GameObject bullet = Rent(0.04f);
        yield return new WaitForSeconds(0.12f);
        Assert.That(bullet != null && !bullet.activeSelf, Is.True);
    }
}
