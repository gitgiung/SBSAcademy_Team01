using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

int tests=0, failures=0;
void Test(string name,Action action) { tests++; try { action(); Console.WriteLine("PASS "+name); } catch(Exception e) { failures++;Console.WriteLine("FAIL "+name+": "+e.Message); } }
void Check(bool condition) { if(!condition)throw new Exception("assertion failed"); }
Test("blank JSON icon loads the default icon by skill ID",()=>{
    UnityEngine.Resources.Reset(); Managers.Initialized=false;
    var icon=new UnityEngine.Sprite();UnityEngine.Resources.Items["SkillIcons/Skill_10011"]=icon;
    Check(ReferenceEquals(icon,SkillCardIcons.Get(10011,"")));
});
Test("already preloaded custom icon takes precedence",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=true;Managers.Resource.Items.Clear();
    var icon=new UnityEngine.Sprite();Managers.Resource.Items["Custom/Icon"]=icon;
    Check(ReferenceEquals(icon,SkillCardIcons.Get(10011,"Custom/Icon")));
    Check(UnityEngine.Resources.Loads==0);
});
Test("Resources custom icon takes precedence over default icon",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=false;
    var icon=new UnityEngine.Sprite();UnityEngine.Resources.Items["Custom/Icon"]=icon;
    Check(ReferenceEquals(icon,SkillCardIcons.Get(10011,"Custom/Icon")));
});
Test("unloaded legacy key falls back to default icon",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=true;Managers.Resource.Items.Clear();
    var icon=new UnityEngine.Sprite();UnityEngine.Resources.Items["SkillIcons/Skill_20007"]=icon;
    Check(ReferenceEquals(icon,SkillCardIcons.Get(20007,"SkillIcons/SkillIcon_20007")));
});
Test("missing optional icon returns null",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=false;
    Check(SkillCardIcons.Get(0,null)==null);Check(UnityEngine.Resources.Loads==0);
    Check(SkillCardIcons.Get(30009,"missing")==null);
});
Test("all 76 skills have unique 256px runtime and source icons",()=>{
    using var json=JsonDocument.Parse(File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
    var hashes=new HashSet<string>();var ids=new HashSet<int>();var guids=new HashSet<string>();
    foreach(var skill in json.RootElement.GetProperty("skills").EnumerateArray()){
        int id=skill.GetProperty("Id").GetInt32();Check(ids.Add(id));
        Check(skill.GetProperty("Icon").GetString()=="SkillIcons/SkillIcon_"+id);
        foreach(string path in new[]{ $"Assets/Resources/SkillIcons/Skill_{id}.png", $"Assets/2.Resources/Art/SkillIcons/SkillIcon_{id}.png" }){
            var bytes=File.ReadAllBytes(path);
            Check(bytes.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}));
            Check(System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4))==256);
            Check(System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4))==256);
            Check(hashes.Add(Convert.ToHexString(SHA256.HashData(bytes))));
            string meta=File.ReadAllText(path+".meta");
            Check(meta.Contains("textureType: 8")&&meta.Contains("spriteMode: 1")&&meta.Contains("filterMode: 0"));
            Check(guids.Add(meta.Split('\n').Single(l=>l.StartsWith("guid: ")).Trim()));
        }
    }
    Check(ids.Count==76);Check(hashes.Count==152);Check(guids.Count==152);
});
Test("table keys fall back to corresponding shipped runtime icons",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=false;
    using var json=JsonDocument.Parse(File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
    foreach(var skill in json.RootElement.GetProperty("skills").EnumerateArray()){
        int id=skill.GetProperty("Id").GetInt32();var icon=new UnityEngine.Sprite();
        UnityEngine.Resources.Items["SkillIcons/Skill_"+id]=icon;
        Check(ReferenceEquals(icon,SkillCardIcons.Get(id,skill.GetProperty("Icon").GetString())));
    }
});
Console.WriteLine($"{tests-failures}/{tests} passed");return failures==0?0:1;

namespace UnityEngine {
    public class Sprite { }
    public static class Resources {
        public static readonly Dictionary<string,Sprite> Items=new();public static int Loads;
        public static void Reset(){Items.Clear();Loads=0;}
        public static T Load<T>(string path) where T:class { Loads++;return Items.TryGetValue(path,out var s)?s as T:null; }
    }
}
public static class Managers {
    public static bool Initialized;
    public static ResourceCache Resource=new();
    public class ResourceCache {
        public readonly Dictionary<string,UnityEngine.Sprite> Items=new();
        public T Load<T>(string path) where T:class => Items.TryGetValue(path,out var s)?s as T:null;
    }
}
