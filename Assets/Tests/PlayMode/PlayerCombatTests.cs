#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class PlayerCombatTests : InputTestFixture
{
    private readonly List<GameObject> owned = new List<GameObject>();
    private GameObject player;
    private PlayerMeleeAttack attack;
    private Keyboard keyboard;
    private Mouse mouse;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        player = Make("Player");
        player.SetActive(false);
        attack = player.AddComponent<PlayerMeleeAttack>();
        var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        SetReference(attack, "inputActions", actions);
        var movement = player.AddComponent<PlayerMvpMovement>();
        SetReference(movement, "inputActions", actions);
        var camera = Make("Test Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = 5;
        SetReference(attack, "aimCamera", camera);
        player.SetActive(true);
    }

    [TearDown]
    public override void TearDown()
    {
        foreach (var item in owned)
            if (item != null) Object.DestroyImmediate(item);
        owned.Clear();
        base.TearDown();
    }

    [Test]
    public void MeleeDamagesFrontOncePerTargetAndIgnoresBehindAndOutOfRange()
    {
        var front = Enemy(new Vector2(0.8f, 0));
        front.gameObject.AddComponent<BoxCollider2D>();
        var behind = Enemy(new Vector2(-0.8f, 0));
        var far = Enemy(new Vector2(4, 0));
        Physics2D.SyncTransforms();
        Assert.That(attack.TryAttack(Vector2.right), Is.True);
        Assert.That(front.CurrentHealth, Is.EqualTo(2));
        Assert.That(behind.CurrentHealth, Is.EqualTo(3));
        Assert.That(far.CurrentHealth, Is.EqualTo(3));
        Assert.That(attack.TryAttack(Vector2.right), Is.False);
        Assert.That(front.CurrentHealth, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator AttackCanRepeatAfterCooldown()
    {
        var front = Enemy(new Vector2(0.8f, 0));
        Physics2D.SyncTransforms();
        attack.TryAttack(Vector2.right);
        yield return new WaitForSeconds(0.35f);
        Assert.That(attack.TryAttack(Vector2.right), Is.True);
        Assert.That(front.CurrentHealth, Is.EqualTo(1));
    }

    [Test]
    public void LeftClickUsesMouseDirectionAndDoesNotAutoRepeatWhileHeld()
    {
        var front = Enemy(new Vector2(0.8f, 0));
        Physics2D.SyncTransforms();
        var camera = Object.FindFirstObjectByType<Camera>();
        Set(mouse.position, (Vector2)camera.WorldToScreenPoint(new Vector3(3, 0, 0)));
        Press(mouse.leftButton);
        Assert.That(front.CurrentHealth, Is.EqualTo(2));
        InputSystem.Update();
        Assert.That(front.CurrentHealth, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator WasdMovesAtSameSpeedDiagonallyAndStopsWhenDisabled()
    {
        Press(keyboard.dKey);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Vector2 before = player.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        float straight = Vector2.Distance(before, player.transform.position);
        // DeltaStateEvent reads the current input buffer; resume a dynamic frame after physics.
        yield return null;
        Press(keyboard.wKey);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        before = player.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        float diagonal = Vector2.Distance(before, player.transform.position);
        Assert.That(straight, Is.GreaterThan(0));
        Assert.That(diagonal, Is.EqualTo(straight).Within(0.002f));
        player.GetComponent<PlayerMvpMovement>().enabled = false;
        yield return new WaitForFixedUpdate();
        before = player.transform.position;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        Assert.That((Vector2)player.transform.position, Is.EqualTo(before));
    }

    [Test]
    public void DisabledAttackCannotDealDamage()
    {
        var front = Enemy(new Vector2(0.8f, 0));
        attack.enabled = false;
        Physics2D.SyncTransforms();
        Assert.That(attack.TryAttack(Vector2.right), Is.False);
        Assert.That(front.CurrentHealth, Is.EqualTo(3));
    }

    private EnemyHealth Enemy(Vector2 position)
    {
        var enemy = Make("Enemy");
        enemy.transform.position = position;
        enemy.AddComponent<CircleCollider2D>().radius = 0.2f;
        return enemy.AddComponent<EnemyHealth>();
    }

    private GameObject Make(string name)
    {
        var item = new GameObject(name);
        owned.Add(item);
        return item;
    }

    private static void SetReference(Object component, string field, Object value)
    {
        var settings = new SerializedObject(component);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
