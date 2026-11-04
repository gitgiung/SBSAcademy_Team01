using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PlayerParryFeedbackTests
{
    readonly List<Object> owned = new List<Object>();
    Component player;
    static Type Runtime(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Call(object obj,string name,params object[] args) => obj.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Single(m=>m.Name==name && m.GetParameters().Length==args.Length).Invoke(obj,args);
    static void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(obj,value);
    static object Field(object obj,string name) => obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(obj);
    GameObject New(string name) { var go=new GameObject(name);owned.Add(go);return go; }
    void CreatePlayer()
    {
        var go=New("parry feedback player");go.SetActive(false);
        var body=go.AddComponent<Rigidbody2D>();body.gravityScale=0;body.bodyType=RigidbodyType2D.Kinematic;
        go.AddComponent<BoxCollider2D>().size=Vector2.one*0.2f;
        go.AddComponent(Runtime("PlayerData"));
        var asset=ScriptableObject.CreateInstance<InputActionAsset>();owned.Add(asset);
        var map=new InputActionMap("Player");map.AddAction("Move",InputActionType.Value);map.AddAction("Dash",InputActionType.Button);map.AddAction("Parry",InputActionType.Button);asset.AddActionMap(map);
        player=go.AddComponent(Runtime("Player"));Set(player,"inputActions",asset);go.SetActive(true);
    }
    GameObject Parry()
    {
        var attacker=New("attacker");attacker.transform.position=Vector3.right*2;
        Assert.That(Call(player,"TryParry"),Is.True);
        Call(player,"TakeDamage",1f,attacker);
        return attacker;
    }
    Component PopupController(out Component pause,out Component level)
    {
        var root=New("feedback popup controller");root.SetActive(false);
        var c=root.AddComponent(Runtime("CombatPopupController"));
        pause=New("feedback pause").AddComponent(Runtime("UI_CombatPopup"));pause.gameObject.SetActive(false);
        level=New("feedback level").AddComponent(Runtime("UI_CombatPopup"));level.gameObject.SetActive(false);
        Set(c,"pausePopup",pause);Set(c,"levelUpPopup",level);Set(c,"pauseButton",New("pause button").AddComponent<Button>());
        root.SetActive(true);return c;
    }
    [SetUp] public void SetUp(){Time.timeScale=1;CreatePlayer();}
    [UnityTearDown] public IEnumerator TearDown()
    {
        foreach(var item in owned.OfType<GameObject>())if(item!=null)Object.Destroy(item);
        foreach(var item in owned.Where(o=>!(o is GameObject)))if(item!=null)Object.Destroy(item);
        owned.Clear();Object.Destroy(GameObject.Find("@UI_Root"));Object.Destroy(GameObject.Find("@Managers"));
        yield return null;Time.timeScale=1;
    }
    [UnityTest] public IEnumerator SuccessfulContactParryFreezesThenRecoilsAwayOnce()
    {
        var source=Parry();Assert.That(Time.timeScale,Is.Zero);
        Vector3 start=player.transform.position;
        Call(player,"TakeDamage",1f,source);
        yield return new WaitForSecondsRealtime(0.03f);
        Assert.That(player.transform.position,Is.EqualTo(start));
        yield return new WaitForSecondsRealtime(0.35f);
        Assert.That(Time.timeScale,Is.EqualTo(1));
        Assert.That(player.transform.position.x,Is.InRange(-0.36f,-0.3f));
        Assert.That(Mathf.Abs(player.transform.position.y),Is.LessThan(0.001f));
    }
    [UnityTest] public IEnumerator GuardDoesNotFreezeOrRecoil()
    {
        Assert.That(Call(player,"TryParry"),Is.True);
        var defense=Field(player,"defense");Set(defense,"isParrying",false);Set(defense,"isGuarding",true);
        var attacker=New("guard attacker");attacker.transform.position=Vector3.right;
        Call(player,"TakeDamage",1f,attacker);Assert.That(Time.timeScale,Is.EqualTo(1));
        yield return new WaitForSecondsRealtime(0.3f);Assert.That(player.transform.position.x,Is.Zero);
    }
    [UnityTest] public IEnumerator RecoilStopsAtSolidWall()
    {
        var wall=New("recoil wall");wall.transform.position=new Vector3(-0.3f,0,0);wall.AddComponent<BoxCollider2D>().size=new Vector2(0.1f,3);
        Physics2D.SyncTransforms();Parry();yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(player.transform.position.x,Is.GreaterThanOrEqualTo(-0.151f));
        Assert.That(player.transform.position.x,Is.LessThan(-0.05f));
    }
    [UnityTest] public IEnumerator PopupDuringHitStopKeepsPausedAndRestoresSlowMotion()
    {
        Component pause,level;var controller=PopupController(out pause,out level);
        Time.timeScale=0.4f;Parry();Assert.That(Time.timeScale,Is.Zero);
        Call(controller,"AcquirePause",pause);Call(controller,"AcquirePause",level);
        yield return new WaitForSecondsRealtime(0.2f);Assert.That(Time.timeScale,Is.Zero);
        Call(controller,"ReleasePause",pause);Assert.That(Time.timeScale,Is.Zero);
        Call(controller,"ReleasePause",level);Assert.That(Time.timeScale,Is.EqualTo(0.4f).Within(0.0001f));
    }
    [UnityTest] public IEnumerator DisablingPlayerDuringHitStopRestoresTime()
    {
        Parry();player.gameObject.SetActive(false);Assert.That(Time.timeScale,Is.EqualTo(1));
        yield return new WaitForSecondsRealtime(0.15f);Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DefeatDuringHitStopDoesNotResumeGame()
    {
        var manager=New("feedback game manager").AddComponent(Runtime("GameManager"));
        Parry();var state=Runtime("GameState");Call(manager,"FinishGame",Enum.Parse(state,"Defeat"));
        yield return new WaitForSecondsRealtime(0.2f);Assert.That(Time.timeScale,Is.Zero);
    }
    [UnityTest] public IEnumerator HitStopRestoresPreviousNonzeroTimeScale()
    {
        Time.timeScale=0.5f;Parry();Assert.That(Time.timeScale,Is.Zero);
        yield return new WaitForSecondsRealtime(0.15f);Assert.That(Time.timeScale,Is.EqualTo(0.5f));
    }
    [UnityTest] public IEnumerator ParryWithoutIncomingAttackDoesNotFreezeOrRecoil()
    {
        Assert.That(Call(player,"TryParry"),Is.True);
        yield return new WaitForSecondsRealtime(0.45f);
        Assert.That(Time.timeScale,Is.EqualTo(1));
        Assert.That(player.transform.position,Is.EqualTo(Vector3.zero));
    }
}