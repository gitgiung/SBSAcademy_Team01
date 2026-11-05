using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class ExperiencePickupColorTests
{
    GameObject root;
    ExperiencePickup pickup;
    SpriteRenderer sprite;
    static FieldInfo Field(string name) => typeof(ExperiencePickup).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance)
        ?? throw new Exception("색상 설정 기능 없음: " + name);
    static bool Amount(ExperiencePickup p,float value)
    {
        var method=typeof(ExperiencePickup).GetMethod("SetExperienceAmount");
        Assert.That(method,Is.Not.Null,"경험치량과 색을 함께 설정하는 API 필요");
        return (bool)method.Invoke(p,new object[]{value});
    }
    static void Refresh(ExperiencePickup p)
    {
        var method=typeof(ExperiencePickup).GetMethod("RefreshExperienceColor");
        Assert.That(method,Is.Not.Null,"설정 변경 후 외형 갱신 필요"); method.Invoke(p,null);
    }
    [SetUp] public void Setup()
    {
        root=new GameObject("PickupColor"); root.SetActive(false);
        sprite=root.AddComponent<SpriteRenderer>(); pickup=root.AddComponent<ExperiencePickup>();
    }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(root); }
    [Test] public void ExistingSerializedAmountColorsOnEnable()
    {
        Field("experienceAmount").SetValue(pickup,1f); root.SetActive(true);
        Assert.That(sprite.color,Is.EqualTo((Color)new Color32(77,166,255,255)));
    }
    [TestCase(2f,77,166,255)]
    [TestCase(2.999f,77,166,255)]
    [TestCase(3f,82,214,129)]
    [TestCase(4f,255,212,92)]
    [TestCase(5.999f,255,212,92)]
    [TestCase(6f,181,140,255)]
    [TestCase(7f,255,158,74)]
    [TestCase(8.999f,255,158,74)]
    [TestCase(9f,255,94,126)]
    [TestCase(100f,255,94,126)]
    public void AmountUsesCorrectInclusiveBand(float value,int r,int g,int b)
    {
        Assert.That(Amount(pickup,value),Is.True);
        Assert.That(sprite.color,Is.EqualTo((Color)new Color32((byte)r,(byte)g,(byte)b,255)));
    }
    [Test] public void InspectorBandsCanBeRecoloredAndReordered()
    {
        var bands=(Array)Field("experienceColors").GetValue(pickup);
        var first=bands.GetValue(0); var last=bands.GetValue(bands.Length-1);
        var type=first.GetType(); type.GetField("minimumExperience").SetValue(last,5f); type.GetField("color").SetValue(last,Color.magenta);
        bands.SetValue(last,0); bands.SetValue(first,bands.Length-1);
        Amount(pickup,5); Assert.That(sprite.color,Is.EqualTo(Color.magenta));
        type.GetField("color").SetValue(last,Color.cyan); Refresh(pickup); Assert.That(sprite.color,Is.EqualTo(Color.cyan));
    }
    [Test] public void EmptyBandsRestoreOriginalSpriteTint()
    {
        var field=Field("experienceColors"); field.SetValue(pickup,Array.CreateInstance(field.FieldType.GetElementType(),0));
        sprite.color=Color.red; Amount(pickup,9); Assert.That(sprite.color,Is.EqualTo(Color.white));
    }
    [Test] public void RejectsInvalidAmountWithoutChangingRewardOrColor()
    {
        Amount(pickup,4); var before=sprite.color;
        foreach(var value in new[]{-1f,float.NaN,float.PositiveInfinity}) Assert.That(Amount(pickup,value),Is.False);
        Assert.That(sprite.color,Is.EqualTo(before));
        Assert.That((float)Field("experienceAmount").GetValue(pickup),Is.EqualTo(4));
    }
    [Test] public void InitializationAndReenableReapplyCurrentAmountColor()
    {
        root.SetActive(true); Amount(pickup,7); sprite.color=Color.white;
        pickup.Initialize(null); Assert.That(sprite.color,Is.EqualTo((Color)new Color32(255,158,74,255)));
        root.SetActive(false); sprite.color=Color.white; root.SetActive(true);
        Assert.That(sprite.color,Is.EqualTo((Color)new Color32(255,158,74,255)));
    }
    [Test] public void ChildRendererIsFoundAndMissingRendererIsSafe()
    {
        UnityEngine.Object.DestroyImmediate(sprite);
        Assert.DoesNotThrow(()=>Amount(pickup,3));
        var child=new GameObject("Visual"); child.transform.SetParent(root.transform); var childSprite=child.AddComponent<SpriteRenderer>();
        Refresh(pickup); Assert.That(childSprite.color,Is.EqualTo((Color)new Color32(82,214,129,255)));
    }
}
