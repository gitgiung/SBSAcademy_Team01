using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LightSkillCastTests
{
    [Test]
    public void CatalogCoversTwentyOneLightSkillsAndUsesTableCosts()
    {
        Assert.That(LightSkillCatalog.Count, Is.EqualTo(21));
        Assert.That(LightSkillCatalog.TryGet(10001, out LightSkillCast bullet), Is.True);
        Assert.That(bullet.MpCost, Is.EqualTo(5f));
        Assert.That(bullet.Shape, Is.EqualTo(LightSkillShape.Projectile));
        Assert.That(bullet.Damage(10f), Is.EqualTo(20f));
        Assert.That(LightSkillCatalog.TryGet(10003, out LightSkillCast smash), Is.True);
        Assert.That(smash.Shape, Is.EqualTo(LightSkillShape.Circle));
        Assert.That(smash.MpCost, Is.EqualTo(8f));
        Assert.That(LightSkillCatalog.TryGet(10014, out LightSkillCast ring), Is.True);
        Assert.That(ring.Shape, Is.EqualTo(LightSkillShape.Ring));
        Assert.That(LightSkillCatalog.TryGet(10018, out LightSkillCast ice), Is.True);
        Assert.That(ice.Element, Is.EqualTo("ELEM_ICE"));
    }

    [UnityTest]
    public IEnumerator UnarmedLightCastSpendsFlameBulletCostAndUnlocks()
    {
        var go = new GameObject("light skill player");
        go.SetActive(false);
        var data = go.AddComponent<PlayerData>();
        data.maxMP = 10f;
        data.attackDamage = 10f;
        var asset = ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputActionAsset>();
        var map = new UnityEngine.InputSystem.InputActionMap("Player");
        map.AddAction("Move", UnityEngine.InputSystem.InputActionType.Value);
        map.AddAction("Dash", UnityEngine.InputSystem.InputActionType.Button);
        map.AddAction("Parry", UnityEngine.InputSystem.InputActionType.Button);
        asset.AddActionMap(map);
        var player = go.AddComponent<Player>();
        var input = typeof(Player).GetField("inputActions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        input.SetValue(player, asset);
        go.SetActive(true);

        Assert.That(player.TryCastLightSkill(Vector2.right), Is.True);
        Assert.That(data.CurrentMP, Is.EqualTo(5f));
        Assert.That(player.IsInputLocked, Is.True);
        yield return new WaitForSeconds(0.6f);
        Assert.That(player.IsInputLocked, Is.False);

        Object.Destroy(go);
        Object.Destroy(asset);
    }
}
