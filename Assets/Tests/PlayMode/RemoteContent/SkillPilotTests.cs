using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class SkillPilotTests
{
    private static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    [Test]
    public void PilotCompatibilityCannotSkipRequiredContentProfile()
    {
        string json = "{\"formatVersion\":1,\"platform\":\"Windows\",\"appCompatibility\":\"skill-pilot-v1\",\"contentVersion\":\"v1\",\"catalogUrl\":\"https://example.com/catalog.bin\",\"catalogSha256\":\"" + new string('a',64) + "\",\"requiredLabel\":\"skill-pilot\"}";
        var args = new object[] { json, "Windows", "skill-pilot-v1", false, null, null };
        Assert.That((bool)Runtime("RemoteContentRelease").GetMethod("TryParse").Invoke(null, args), Is.False);
        Assert.That(args[5], Is.Not.Null.And.Not.Empty);
    }
    [Test]
    public void LocalEditorCanReadMovedGameplayTableTextureAndIcon()
    {
        var method = Runtime("Managers").GetMethod("LoadGameplayAsset");
        Assert.That(method, Is.Not.Null, "이전한 에셋을 위한 중앙 공급 경로가 필요합니다.");
        var table = method.MakeGenericMethod(typeof(TextAsset)).Invoke(null, new object[] { "Data/CombatSkillTable" });
        var texture = method.MakeGenericMethod(typeof(Texture2D)).Invoke(null, new object[] { "LightSkills/Fx/WaterArrow" });
        var icon = method.MakeGenericMethod(typeof(Sprite)).Invoke(null, new object[] { "SkillIcons/SkillIcon_10025" });
        Assert.That(table, Is.Not.Null);
        Assert.That(texture, Is.TypeOf<Texture2D>());
        Assert.That(icon, Is.TypeOf<Sprite>());
    }

    [Test]
    public void DuplicateSkillIdsCannotReplacePublishedTable()
    {
        Type type = Runtime("CombatSkillTable");
        MethodInfo install = type.GetMethod("TryInstall");
        Assert.That(install, Is.Not.Null, "검증한 전투 표만 게시해야 합니다.");
        var asset = new TextAsset("{\"skills\":[{\"Id\":10025,\"Name\":\"a\"},{\"Id\":10025,\"Name\":\"b\"}]}");
        try
        {
            var valid = new TextAsset("{\"skills\":[{\"Id\":10025,\"Name\":\"kept\",\"AreaType\":\"Projectile\"}]}");
            try { Assert.That((bool)install.Invoke(null, new object[] { valid, null }), Is.True); }
            finally { UnityEngine.Object.DestroyImmediate(valid); }
            var args = new object[] { asset, null };
            Assert.That((bool)install.Invoke(null, args), Is.False);
            Assert.That(args[1], Is.Not.Null.And.Not.Empty);
            var read = new object[] { 10025, null };
            Assert.That((bool)type.GetMethod("TryGet").Invoke(null, read), Is.True);
            Assert.That(read[1].GetType().GetField("Name").GetValue(read[1]), Is.EqualTo("kept"));
        }
        finally { UnityEngine.Object.DestroyImmediate(asset); }
    }

    [Test]
    public void UnsupportedPilotAttackShapeCannotBePublished()
    {
        var invalid = new TextAsset("{\"skills\":[{\"Id\":10025,\"Name\":\"수살\",\"AreaType\":\"Bogus\"}]}");
        try { Assert.That((bool)Runtime("CombatSkillTable").GetMethod("TryInstall").Invoke(null, new object[] { invalid, null }), Is.False); }
        finally { UnityEngine.Object.DestroyImmediate(invalid); }
    }

    [Test]
    public void MalformedFramesAreRejectedAndValidRetryBuildsFreshSprites()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE"))) Assert.Ignore("격리 도구로 실행하세요.");
        object resource = Runtime("Managers").GetProperty("Resource").GetValue(null);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        resource.GetType().GetField("skillPilotRequired", flags).SetValue(resource, true);
        var cache = (IDictionary)resource.GetType().GetField("_resources", flags).GetValue(resource);
        var texture = new Texture2D(2,2);
        var invalid = new TextAsset("{\"fps\":30,\"count\":10,\"columns\":1,\"frameWidth\":999,\"frameHeight\":2}");
        var valid = new TextAsset("{\"fps\":30,\"count\":1,\"columns\":1,\"frameWidth\":2,\"frameHeight\":2,\"pivotX\":0.5,\"pivotY\":0.5}");
        try
        {
            cache["SkillPilot/WaterArrowTexture"] = texture;
            cache["SkillPilot/WaterArrowFrames"] = invalid;
            MethodInfo prepare = Runtime("LightSkillVisuals").GetMethod("TryPrepareBaked");
            Assert.That((bool)prepare.Invoke(null, new object[] { "WaterArrow", null }), Is.False);
            cache["SkillPilot/WaterArrowFrames"] = valid;
            Assert.That((bool)prepare.Invoke(null, new object[] { "WaterArrow", null }), Is.True);
            object clip = Runtime("LightSkillVisuals").GetMethod("Baked").Invoke(null, new object[] { "WaterArrow" });
            Assert.That(((Sprite[])clip.GetType().GetField("Frames").GetValue(clip)).Length, Is.EqualTo(1));
        }
        finally
        {
            Runtime("LightSkillVisuals").GetMethod("ClearBaked").Invoke(null, new object[] { "WaterArrow" });
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(invalid);
            UnityEngine.Object.DestroyImmediate(valid);
        }
    }

    [UnityTest]
    public IEnumerator PilotMemoryPreparationCannotDownloadBeforeConsent()
    {
        string fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        if (string.IsNullOrEmpty(fixture)) Assert.Ignore("격리 도구로 실행하세요.");
        var hash = Hash128.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(fixture, "pilot/hash.txt")).Trim());
        Caching.ClearCachedVersion("skill-pilot-fixture", hash);
        Type managers = Runtime("Managers");
        var config = ScriptableObject.CreateInstance(Runtime("RemoteContentSettings"));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        config.GetType().GetField("useRemoteContent", fields).SetValue(config, true);
        config.GetType().GetField("allowDevelopmentHttp", fields).SetValue(config, true);
        config.GetType().GetField("appCompatibility", fields).SetValue(config, "skill-pilot-v1");
        config.GetType().GetField("releaseUrl", fields).SetValue(config, Environment.GetEnvironmentVariable("SBSA_REMOTE_ORIGIN") + "/pilot/release.json");
        try
        {
            var prepare = managers.GetMethod("PrepareRemoteContent", new[] { config.GetType() });
            IEnumerator request = RemotePreparationTests.Flatten((IEnumerator)prepare.Invoke(null, new object[] { config }));
            request.MoveNext();
            float deadline = Time.realtimeSinceStartup + 15;
            object Status() => managers.GetProperty("RemoteStatus").GetValue(null);
            string State() => Status().GetType().GetProperty("State").GetValue(Status()).ToString();
            while (State() != "AwaitingConsent" && State() != "Failed" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(State(), Is.EqualTo("AwaitingConsent"));
            object resource = managers.GetProperty("Resource").GetValue(null);
            bool loaded = true;
            yield return (IEnumerator)resource.GetType().GetMethod("PrepareSkillPilotAsync").Invoke(resource,
                new object[] { new Action<bool,string>((ok,error) => loaded = ok) });
            Assert.That(loaded, Is.False);
            Assert.That(Caching.IsVersionCached("skill-pilot-fixture", hash), Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(config); }
    }

    [UnityTest]
    public IEnumerator MissingRemotePilotAssetsFailWithoutLocalFallback()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE"))) Assert.Ignore("격리 도구로 실행하세요.");
        object resource = Runtime("Managers").GetProperty("Resource").GetValue(null);
        MethodInfo prepare = resource.GetType().GetMethod("PrepareSkillPilotAsync");
        Assert.That(prepare, Is.Not.Null, "실제 스킬 에셋 준비가 필요합니다.");
        bool success = true; string error = null;
        yield return (IEnumerator)prepare.Invoke(resource, new object[] { new Action<bool,string>((ok, reason) => { success = ok; error = reason; }) });
        Assert.That(success, Is.False);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
        var load = Runtime("Managers").GetMethod("LoadGameplayAsset").MakeGenericMethod(typeof(Texture2D));
        Assert.That(load.Invoke(null, new object[] { "LightSkills/Fx/WaterArrow" }), Is.Null,
            "원격 준비 실패를 로컬 그림으로 감추면 안 됩니다.");
    }

    [UnityTest]
    public IEnumerator ActualRemoteSkillUsesDownloadedTableTextureFramesAndIconAndRepreparesAfterClear()
    {
        string fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        if (string.IsNullOrEmpty(fixture)) Assert.Ignore("격리 도구로 실행하세요.");
        var hash = Hash128.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(fixture, "pilot/hash.txt")).Trim());
        Caching.ClearCachedVersion("skill-pilot-fixture", hash);
        Type managers = Runtime("Managers");
        var config = ScriptableObject.CreateInstance(Runtime("RemoteContentSettings"));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        config.GetType().GetField("useRemoteContent", fields).SetValue(config, true);
        config.GetType().GetField("allowDevelopmentHttp", fields).SetValue(config, true);
        config.GetType().GetField("appCompatibility", fields).SetValue(config, "skill-pilot-v1");
        config.GetType().GetField("releaseUrl", fields).SetValue(config, Environment.GetEnvironmentVariable("SBSA_REMOTE_ORIGIN") + "/pilot/release.json");
        try
        {
            var prepare = managers.GetMethod("PrepareRemoteContent", new[] { config.GetType() });
            IEnumerator request = RemotePreparationTests.Flatten((IEnumerator)prepare.Invoke(null, new object[] { config }));
            request.MoveNext();
            float deadline = Time.realtimeSinceStartup + 15;
            string State() { object s = managers.GetProperty("RemoteStatus").GetValue(null); return s.GetType().GetProperty("State").GetValue(s).ToString(); }
            while (State() != "AwaitingConsent" && State() != "Failed" && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(State(), Is.EqualTo("AwaitingConsent"));
            managers.GetMethod("ApproveRemoteDownload").Invoke(null, null);
            yield return request;
            object status = managers.GetProperty("RemoteStatus").GetValue(null);
            Assert.That(State(), Is.EqualTo("Ready"), status.GetType().GetProperty("Error").GetValue(status) as string);
            object resource = managers.GetProperty("Resource").GetValue(null);
            Texture2D texture = (Texture2D)resource.GetType().GetMethod("Load").MakeGenericMethod(typeof(Texture2D)).Invoke(resource, new object[] { "SkillPilot/WaterArrowTexture" });
            Assert.That(texture, Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("LightSkills/Fx/WaterArrow"), Is.Null);
            object clip = Runtime("LightSkillVisuals").GetMethod("Baked").Invoke(null, new object[] { "WaterArrow" });
            var frames = (Sprite[])clip.GetType().GetField("Frames").GetValue(clip);
            Assert.That(frames.Length, Is.EqualTo(12));
            Assert.That(frames[0].texture, Is.SameAs(texture));
            Assert.That((bool)clip.GetType().GetField("Loop").GetValue(clip), Is.True);
            var args = new object[] { 10025, null };
            Assert.That((bool)Runtime("CombatSkillTable").GetMethod("TryGet").Invoke(null, args), Is.True);
            Assert.That(args[1].GetType().GetField("Name").GetValue(args[1]), Is.EqualTo("수살"));
            object icon = Runtime("SkillCardIcons").GetMethod("Get").Invoke(null, new object[] { 10025, "SkillIcons/SkillIcon_10025" });
            object cachedIcon = resource.GetType().GetMethod("Load").MakeGenericMethod(typeof(Sprite)).Invoke(resource, new object[] { "SkillPilot/Icon10025" });
            Assert.That(icon, Is.SameAs(cachedIcon).And.Not.Null);
            resource.GetType().GetMethod("Clear").Invoke(resource, null);
            yield return null;
            Assert.That(frames[0] == null, Is.True, "파생 Sprite를 원본 핸들보다 먼저 정리해야 합니다.");
            yield return (IEnumerator)prepare.Invoke(null, new object[] { config });
            Assert.That(State(), Is.EqualTo("Ready"));
        }
        finally { UnityEngine.Object.DestroyImmediate(config); }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        var manager = GameObject.Find("@Managers");
        if (manager != null) UnityEngine.Object.DestroyImmediate(manager);
        Runtime("CombatSkillTable").GetMethod("ClearCached")?.Invoke(null, null);
        string fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        if (!string.IsNullOrEmpty(fixture))
        {
            string hashFile = System.IO.Path.Combine(fixture, "pilot/hash.txt");
            if (System.IO.File.Exists(hashFile))
            {
                var hash = Hash128.Parse(System.IO.File.ReadAllText(hashFile).Trim());
                float deadline = Time.realtimeSinceStartup + 5;
                while (Caching.IsVersionCached("skill-pilot-fixture", hash) && Time.realtimeSinceStartup < deadline)
                {
                    Caching.ClearCachedVersion("skill-pilot-fixture", hash);
                    yield return null;
                }
            }
        }
    }
}
