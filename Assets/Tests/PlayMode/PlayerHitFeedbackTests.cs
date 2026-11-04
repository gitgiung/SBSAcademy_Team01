using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class PlayerHitFeedbackTests
{
    readonly List<Object> owned = new List<Object>();
    Component player, feedback;
    SpriteRenderer renderer;
    readonly Color original = new Color(0.6f,0.8f,1f,0.7f);
    static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Call(object obj,string name,params object[] args) => obj.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Single(m=>m.Name==name && m.GetParameters().Length==args.Length).Invoke(obj,args);
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(obj,value);
    GameObject New(string name){var go=new GameObject(name);owned.Add(go);return go;}
    void EnableFeedback(){feedback=player.gameObject.AddComponent(Runtime("PlayerHitFeedbackTest"));}
    void Hit(){var source=New("hit source");source.transform.position=Vector3.right*2;Call(player,"TakeDamage",1f,source);}
    [UnitySetUp] public IEnumerator SetUp()
    {
        Time.timeScale=1;
        var go=New("hit feedback player");go.SetActive(false);
        renderer=go.AddComponent<SpriteRenderer>();renderer.color=original;
        var rb=go.AddComponent<Rigidbody2D>();rb.gravityScale=0;rb.bodyType=RigidbodyType2D.Kinematic;
        go.AddComponent<BoxCollider2D>().size=Vector2.one*0.2f;
        go.AddComponent(Runtime("PlayerData"));
        var asset=ScriptableObject.CreateInstance<InputActionAsset>();owned.Add(asset);
        var map=new InputActionMap("Player");map.AddAction("Move",InputActionType.Value);map.AddAction("Dash",InputActionType.Button);map.AddAction("Parry",InputActionType.Button);asset.AddActionMap(map);
        player=go.AddComponent(Runtime("Player"));Set(player,"inputActions",asset);go.SetActive(true);
        yield return null;
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        foreach(var item in owned.OfType<GameObject>())if(item!=null)Object.Destroy(item);
        foreach(var item in owned.Where(o=>!(o is GameObject)))if(item!=null)Object.Destroy(item);
        owned.Clear();yield return null;Time.timeScale=1;
    }
    [UnityTest] public IEnumerator RealDamageTintsFreezesRecoilsAndRestoresOriginalColor()
    {
        EnableFeedback();Hit();Assert.That(Time.timeScale,Is.Zero);
        Assert.That(renderer.color.r,Is.EqualTo(1));Assert.That(renderer.color.g,Is.Zero);Assert.That(renderer.color.a,Is.EqualTo(original.a));
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(player.transform.position.x,Is.InRange(-0.26f,-0.2f));
        Assert.That(renderer.color,Is.EqualTo(original));
    }
    [UnityTest] public IEnumerator ComponentIsOptionalAndDoesNotChangePlainDamage()
    {
        Hit();Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(renderer.color,Is.EqualTo(original));
        yield return new WaitForSecondsRealtime(0.3f);Assert.That(player.transform.position.x,Is.Zero);
    }
    [UnityTest] public IEnumerator BlockedParryDoesNotTriggerRedDamageFlash()
    {
        EnableFeedback();Assert.That(Call(player,"TryParry"),Is.True);Hit();
        Assert.That(renderer.color,Is.EqualTo(original));
        yield return new WaitForSecondsRealtime(0.4f);Assert.That(renderer.color,Is.EqualTo(original));
    }
    [UnityTest] public IEnumerator DisablingComponentRestoresTimeColorAndMovementLock()
    {
        EnableFeedback();Hit();((Behaviour)feedback).enabled=false;
        Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(renderer.color,Is.EqualTo(original));
        Assert.That(player.GetType().GetProperty("CanAcceptGameplayInput").GetValue(player),Is.True);
        yield return new WaitForSecondsRealtime(0.3f);Assert.That(player.transform.position.x,Is.Zero);
    }
    [UnityTest] public IEnumerator RepeatedDamageDoesNotKeepTheRedColorAsOriginal()
    {
        EnableFeedback();Hit();Hit();yield return new WaitForSecondsRealtime(0.5f);
        Assert.That(renderer.color,Is.EqualTo(original));Assert.That(Time.timeScale,Is.EqualTo(1));
    }
}