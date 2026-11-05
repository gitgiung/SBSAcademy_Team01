using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class PlayerParryCameraTests
{
    readonly List<Object> owned=new List<Object>(); Component player; Camera camera;
    static Type Runtime(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(obj,value);
    static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(obj,args);
    GameObject New(string name){var go=new GameObject(name);owned.Add(go);return go;}
    [UnitySetUp] public IEnumerator Setup()
    {
        Time.timeScale=1; camera=New("zoom main camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=10;
        var go=New("zoom player");go.SetActive(false);go.AddComponent(Runtime("PlayerData"));
        var asset=ScriptableObject.CreateInstance<InputActionAsset>();owned.Add(asset);var map=new InputActionMap("Player");
        map.AddAction("Move",InputActionType.Value);map.AddAction("Dash",InputActionType.Button);map.AddAction("Parry",InputActionType.Button);asset.AddActionMap(map);
        player=go.AddComponent(Runtime("Player"));Set(player,"inputActions",asset);go.SetActive(true);yield return null;
    }
    void Parry(){Assert.That(Call(player,"TryParry"),Is.True);var source=New("zoom attacker");source.transform.position=Vector3.right;Call(player,"TakeDamage",1f,source);}
    [UnityTearDown] public IEnumerator Cleanup(){foreach(var o in owned)if(o!=null)Object.Destroy(o);owned.Clear();yield return null;Time.timeScale=1;}
    [UnityTest] public IEnumerator OrthographicZoomRunsDuringHitStopAndRestores()
    {
        Parry();Assert.That(Time.timeScale,Is.Zero);yield return null;
        Assert.That(camera.orthographicSize,Is.LessThan(10));yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(camera.orthographicSize,Is.EqualTo(10).Within(0.0001f));
    }
    [UnityTest] public IEnumerator PerspectiveZoomRestoresFieldOfView()
    {
        camera.orthographic=false;camera.fieldOfView=60;Parry();yield return null;
        Assert.That(camera.fieldOfView,Is.LessThan(60));yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(camera.fieldOfView,Is.EqualTo(60).Within(0.0001f));
    }
    [UnityTest] public IEnumerator DisablingPlayerRestoresZoomImmediately()
    {
        Parry();yield return null;player.gameObject.SetActive(false);
        Assert.That(camera.orthographicSize,Is.EqualTo(10).Within(0.0001f));
    }
    [UnityTest] public IEnumerator ParryWithoutHitDoesNotZoom()
    {
        Assert.That(Call(player,"TryParry"),Is.True);yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(camera.orthographicSize,Is.EqualTo(10));
    }
    [UnityTest] public IEnumerator CinemachineActiveLensAndMainOutputZoomThenRestore()
    {
        camera.gameObject.AddComponent(Runtime("Unity.Cinemachine.CinemachineBrain"));
        var virtualCamera=New("zoom virtual camera").AddComponent(Runtime("Unity.Cinemachine.CinemachineCamera"));
        var lensField=virtualCamera.GetType().GetField("Lens");var lens=lensField.GetValue(virtualCamera);
        Set(lens,"OrthographicSize",10f);var mode=lens.GetType().GetField("ModeOverride");mode.SetValue(lens,Enum.Parse(mode.FieldType,"Orthographic"));lensField.SetValue(virtualCamera,lens);
        yield return null;yield return null;Parry();yield return new WaitForSecondsRealtime(0.04f);
        lens=lensField.GetValue(virtualCamera);Assert.That((float)lens.GetType().GetField("OrthographicSize").GetValue(lens),Is.LessThan(10));
        Assert.That(camera.orthographicSize,Is.LessThan(10));yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(camera.orthographicSize,Is.EqualTo(10).Within(0.0001f));
    }
}