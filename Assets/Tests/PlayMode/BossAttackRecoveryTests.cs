#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class BossAttackRecoveryTests
{
    readonly List<Object> owned=new List<Object>();Component boss,follow;Animator animator;GameObject target;
    const string Art="Assets/2.Resources/Art/Beast_Zombie_Boss/Animator/";
    static Type Runtime(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Single(m=>m.Name==n&&m.GetParameters().Length==args.Length).Invoke(o,args);
    bool Attacking=>(bool)boss.GetType().GetField("attackAnimationRunning",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(boss);
    GameObject New(string name){var go=new GameObject(name);owned.Add(go);return go;}
    void Create(float distance=8)
    {
        target=New("recovery target");target.transform.position=Vector3.right*8;
        var go=New("recovery boss");go.SetActive(false);go.AddComponent<SpriteRenderer>();
        animator=go.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Art+"Boss.controller");
        go.AddComponent(Runtime("BossPatternRuleSelector"));boss=go.AddComponent(Runtime("BossController"));
        Set(boss,"closeRange",4f);Set(boss,"specialPatternDistance",12.01f);Set(boss,"target",target.transform);
        go.SetActive(true);((MonoBehaviour)boss).StopAllCoroutines();follow=go.GetComponent(Runtime("EnemyFollow"));
        target.transform.position=Vector3.right*distance;
    }
    void BeginSlash(){target.transform.position=Vector3.right*2;((MonoBehaviour)boss).StartCoroutine((IEnumerator)Call(boss,"PlaySlashAnimation",false));Assert.That(Attacking,Is.True);}
    void AssertRecovered(){Assert.That(Attacking,Is.False);Assert.That(((Behaviour)follow).enabled,Is.True);}
    [UnityTearDown]public IEnumerator Cleanup(){Time.timeScale=1;foreach(var o in owned)if(o!=null)Object.Destroy(o);owned.Clear();yield return null;}
    [TestCase(4.00005f)][TestCase(3.99f)]public void StoppingBoundaryAllowsMeleeWithoutExpandingRange(float distance)
    {
        Create(distance);for(int i=0;i<60;i++)Call(follow,"FixedUpdate");
        float actual=Vector2.Distance(boss.GetComponent<Rigidbody2D>().position,target.transform.position);
        Assert.That(actual,Is.LessThanOrEqualTo(4f));Assert.That(Call(boss,"SelectAvailablePattern",actual).ToString(),Is.EqualTo("Pattern1"));
        Assert.That(Call(boss,"SelectAvailablePattern",4.00005f).ToString(),Is.EqualTo("None"));
    }
    [UnityTest]public IEnumerator DisabledAnimatorReleasesAttackWait(){Create();BeginSlash();animator.enabled=false;yield return null;yield return null;AssertRecovered();}
    [UnityTest]public IEnumerator RemovedControllerReleasesAttackWait(){Create();BeginSlash();animator.runtimeAnimatorController=null;yield return null;yield return null;AssertRecovered();}
    [UnityTest]public IEnumerator ChangedAnimatorStateReleasesAttackWait(){Create();BeginSlash();animator.Play("Base Layer.BossIdle",0,0);animator.Update(0);yield return null;yield return null;AssertRecovered();}
    [UnityTest]public IEnumerator MissingFinishEventStillReleasesAtAnimationEnd()
    {
        Create();var original=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"Clips/Boss_Swipe_East.anim");
        var clip=Object.Instantiate(original);owned.Add(clip);clip.events=clip.events.Where(e=>e.functionName!="AnimationEvent_AttackAnimationFinished").ToArray();
        var controller=new AnimatorOverrideController(animator.runtimeAnimatorController);owned.Add(controller);controller[original.name]=clip;animator.runtimeAnimatorController=controller;
        BeginSlash();float end=Time.realtimeSinceStartup+3;while(Attacking&&Time.realtimeSinceStartup<end)yield return null;AssertRecovered();
    }
    [UnityTest]public IEnumerator OrdinaryPauseKeepsAttackUntilResumed()
    {
        Create();BeginSlash();Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);
        Assert.That(Attacking,Is.True);Assert.That(((Behaviour)follow).enabled,Is.False);
        Time.timeScale=1;float end=Time.realtimeSinceStartup+3;while(Attacking&&Time.realtimeSinceStartup<end)yield return null;AssertRecovered();
    }
}
#endif