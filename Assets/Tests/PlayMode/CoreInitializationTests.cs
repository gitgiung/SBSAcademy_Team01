using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CoreInitializationTests
{
    // Framework lives in Assembly-CSharp. Reflection avoids moving runtime code
    // into a new assembly just to make it visible to a test assembly.
    private static Type ManagersType => Type.GetType("Managers, Assembly-CSharp", true);

    private static object Service(string name) => ManagersType.GetProperty(name).GetValue(null);

    private static object Call(object target, string method, params object[] arguments)
    {
        Type type = target as Type ?? target.GetType();
        MethodInfo info = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Single(candidate => candidate.Name == method && candidate.GetParameters().Length == arguments.Length);
        return info.Invoke(target is Type ? null : target, arguments);
    }

    private static void Init() => Call(ManagersType, "Init");

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        foreach (GameObject root in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (root.name == "@Managers" || root.name == "@SoundRoot")
                UnityEngine.Object.Destroy(root);
        yield return null;
    }

    [Test]
    public void InitMakesSoundUsable()
    {
        Init();
        object bgm = Enum.Parse(Type.GetType("Define+ESound, Assembly-CSharp", true), "Bgm");
        Assert.DoesNotThrow(() => Call(Service("Sound"), "Stop", bgm));
    }

    [Test]
    public void RepeatedInitKeepsOneOwnedSoundRoot()
    {
        Init();
        Init();
        GameObject[] roots = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name == "@SoundRoot").ToArray();
        Assert.That(roots, Has.Length.EqualTo(1));
        Assert.That(roots[0].transform.parent, Is.EqualTo(GameObject.Find("@Managers").transform));
        Assert.That(roots[0].GetComponentsInChildren<AudioSource>(), Has.Length.EqualTo(2));
    }

    [Test]
    public void InitReconnectsAnExistingSoundRoot()
    {
        GameObject root = new GameObject("@SoundRoot");
        new GameObject("Bgm").AddComponent<AudioSource>().transform.SetParent(root.transform);
        Init();
        object effect = Enum.Parse(Type.GetType("Define+ESound, Assembly-CSharp", true), "Effect");
        Assert.DoesNotThrow(() => Call(Service("Sound"), "Stop", effect));
        Assert.That(root.transform.parent, Is.EqualTo(GameObject.Find("@Managers").transform));
        Assert.That(root.GetComponentsInChildren<AudioSource>(), Has.Length.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator DestroyingManagersAlsoDestroysItsSoundRoot()
    {
        Init();
        GameObject soundRoot = GameObject.Find("@SoundRoot");
        Assert.That(soundRoot, Is.Not.Null);
        UnityEngine.Object.Destroy(GameObject.Find("@Managers"));
        yield return null;
        Assert.That(soundRoot == null, Is.True);
    }
}
