using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;

int tests=0, failures=0;
void Test(string name,Action action) { tests++; try { action(); Console.WriteLine("PASS "+name); } catch(Exception e) { failures++;Console.WriteLine("FAIL "+name+": "+e.Message); } }
void Check(bool condition) { if(!condition)throw new Exception("assertion failed"); }
Test("blank JSON icon loads the PPT icon by skill ID",()=>{
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
Test("Resources custom icon takes precedence over PPT default",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=false;
    var icon=new UnityEngine.Sprite();UnityEngine.Resources.Items["Custom/Icon"]=icon;
    Check(ReferenceEquals(icon,SkillCardIcons.Get(10011,"Custom/Icon")));
});
Test("unloaded legacy key falls back to PPT icon",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=true;Managers.Resource.Items.Clear();
    var icon=new UnityEngine.Sprite();UnityEngine.Resources.Items["SkillIcons/Skill_20007"]=icon;
    Check(ReferenceEquals(icon,SkillCardIcons.Get(20007,"SkillIcons/SkillIcon_20007")));
});
Test("missing optional icon returns null",()=>{
    UnityEngine.Resources.Reset();Managers.Initialized=false;
    Check(SkillCardIcons.Get(0,null)==null);Check(UnityEngine.Resources.Loads==0);
    Check(SkillCardIcons.Get(30009,"missing")==null);
});
Test("all 46 runtime skills have exact PPT pictures and Sprite import settings",()=>{
    using var json=JsonDocument.Parse(File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
    using var ppt=ZipFile.OpenRead("Document/스킬_개요.pptx");
    var hashes=ppt.Entries.Where(e=>e.FullName.StartsWith("ppt/media/")).Select(e=>{using var s=e.Open();return Convert.ToHexString(SHA256.HashData(s));}).ToHashSet();
    int count=0;
    foreach(var skill in json.RootElement.GetProperty("skills").EnumerateArray()){
        int id=skill.GetProperty("Id").GetInt32();string path=$"Assets/Resources/SkillIcons/Skill_{id}.png";
        Check(File.Exists(path));Check(hashes.Contains(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))));
        string meta=File.ReadAllText(path+".meta");Check(meta.Contains("textureType: 8")&&meta.Contains("spriteMode: 1")&&meta.Contains("filterMode: 0"));count++;
    }
    Check(count==46);
});
Test("PPT picture labels map to the correct skill IDs",()=>{
    using var json=JsonDocument.Parse(File.ReadAllText("Assets/Resources/Data/CombatSkillTable.json"));
    var names=json.RootElement.GetProperty("skills").EnumerateArray().ToDictionary(r=>r.GetProperty("Name").GetString().Replace(" ",""),r=>r.GetProperty("Id").GetInt32());
    using var ppt=ZipFile.OpenRead("Document/스킬_개요.pptx");
    XDocument Read(string path){using var s=ppt.GetEntry(path).Open();return XDocument.Load(s);}
    XNamespace a="http://schemas.openxmlformats.org/drawingml/2006/main",p="http://schemas.openxmlformats.org/presentationml/2006/main",r="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    var seen=new HashSet<int>();
    foreach(int slide in new[]{6,7,8}){
        var rels=Read($"ppt/slides/_rels/slide{slide}.xml.rels").Root.Elements().ToDictionary(x=>(string)x.Attribute("Id"),x=>(string)x.Attribute("Target"));
        string picture=null;
        foreach(var shape in Read($"ppt/slides/slide{slide}.xml").Descendants(p+"spTree").Single().Elements()){
            var image=shape.Descendants(a+"blip").FirstOrDefault();
            if(image!=null){picture="ppt/"+rels[(string)image.Attribute(r+"embed")].Replace("../","");continue;}
            string label=string.Concat(shape.Descendants(a+"t").Select(x=>x.Value)).Replace(" ","");
            if(picture==null||!names.TryGetValue(label,out int id))continue;
            Check(seen.Add(id));using var media=ppt.GetEntry(picture).Open();
            Check(SHA256.HashData(media).SequenceEqual(SHA256.HashData(File.ReadAllBytes($"Assets/Resources/SkillIcons/Skill_{id}.png"))));picture=null;
        }
    }
    Check(seen.Count==46);
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
