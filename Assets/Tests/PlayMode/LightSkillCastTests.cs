using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class LightSkillCastTests
{
    // 플레이어와 약공격 표는 Assembly-CSharp에 있다. 테스트 어셈블리는 그 어셈블리를 참조하지 않으므로 리플렉션으로 읽는다.
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);

    private static object Call(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Public | BindingFlags.Instance)
        .Invoke(target, args);

    private static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .SetValue(target, value);

    private static T Read<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);

    private static object Field(object target, string name) => target.GetType().GetField(name).GetValue(target);

    private static object Skill(int id)
    {
        object[] args = { id, null };
        Assert.That(Runtime("LightSkillCatalog").GetMethod("TryGet").Invoke(null, args), Is.True);
        return args[1];
    }

    [Test]
    public void CatalogCoversTwentyOneLightSkillsAndUsesTableCosts()
    {
        Assert.That(Runtime("LightSkillCatalog").GetProperty("Count").GetValue(null), Is.EqualTo(21));
        object bullet = Skill(10001);
        Assert.That(Field(bullet, "MpCost"), Is.EqualTo(5f));
        Assert.That(Field(bullet, "Shape").ToString(), Is.EqualTo("Projectile"));
        Assert.That(bullet.GetType().GetMethod("Damage").Invoke(bullet, new object[] { 10f }), Is.EqualTo(20f));
        object smash = Skill(10003);
        Assert.That(Field(smash, "Shape").ToString(), Is.EqualTo("Circle"));
        Assert.That(Field(smash, "MpCost"), Is.EqualTo(8f));
        object ring = Skill(10014);
        Assert.That(Field(ring, "Shape").ToString(), Is.EqualTo("Ring"));
        object ice = Skill(10018);
        Assert.That(Field(ice, "Element"), Is.EqualTo("ELEM_ICE"));
    }

    [UnityTest]
    public IEnumerator UnarmedLightCastSpendsFlameBulletCostAndUnlocks()
    {
        var go = new GameObject("light skill player");
        go.SetActive(false);
        Component data = go.AddComponent(Runtime("PlayerData"));
        Set(data, "maxMP", 10f);
        Set(data, "attackDamage", 10f);
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        map.AddAction("Move", InputActionType.Value);
        map.AddAction("Dash", InputActionType.Button);
        map.AddAction("Parry", InputActionType.Button);
        asset.AddActionMap(map);
        Component player = go.AddComponent(Runtime("Player"));
        Set(player, "inputActions", asset);
        go.SetActive(true);

        Assert.That(Call(player, "TryCastLightSkill", Vector2.right), Is.True);
        Assert.That(Read<float>(data, "CurrentMP"), Is.EqualTo(5f));
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.True);
        yield return new WaitForSeconds(0.6f);
        Assert.That(Read<bool>(player, "IsInputLocked"), Is.False);

        Object.Destroy(go);
        Object.Destroy(asset);
    }
}
