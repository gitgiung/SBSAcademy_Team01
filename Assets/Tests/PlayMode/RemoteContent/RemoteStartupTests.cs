using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RemoteStartupTests
{
    private static Type Runtime(string name)
    {
        Type type = Type.GetType(name + ", Assembly-CSharp");
        Assert.That(type, Is.Not.Null, name + " 계약이 필요합니다.");
        return type;
    }

    private static string Origin => Environment.GetEnvironmentVariable("SBSA_REMOTE_ORIGIN");
    private static string Manifest(string catalogHash = null, string platform = "Windows", string compatibility = "1")
    {
        string hash = catalogHash ?? new string('a', 64);
        return "{\"formatVersion\":1,\"platform\":\"" + platform + "\",\"appCompatibility\":\"" + compatibility
            + "\",\"contentVersion\":\"pilot-v1\",\"catalogUrl\":\"" + (Origin ?? "http://127.0.0.1:18763")
            + "/catalog.bin\",\"catalogSha256\":\"" + hash + "\",\"requiredLabel\":\"remote-startup\"}";
    }

    private static bool Parse(string json, bool allowHttp, out object release, out string error)
    {
        var args = new object[] { json, "Windows", "1", allowHttp, null, null };
        bool ok = (bool)Runtime("RemoteContentRelease").GetMethod("TryParse").Invoke(null, args);
        release = args[4]; error = (string)args[5];
        return ok;
    }

    [Test]
    public void CompatibleReleaseParsesWithExplicitDevelopmentHttp()
    {
        Assert.That(Parse(Manifest(), true, out object release, out string error), Is.True, error);
        Assert.That(release.GetType().GetField("contentVersion").GetValue(release), Is.EqualTo("pilot-v1"));
    }

    [TestCase("{}")]
    [TestCase("not json")]
    [TestCase("null")]
    public void MissingOrMalformedReleaseFails(string json)
    {
        Assert.That(Parse(json, true, out object release, out string error), Is.False);
        Assert.That(release, Is.Null);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void DifferentPlatformOrAppCompatibilityIsRejected()
    {
        Assert.That(Parse(Manifest(platform: "Android"), true, out _, out string platformError), Is.False);
        Assert.That(platformError, Is.Not.Empty);
        Assert.That(Parse(Manifest(compatibility: "2"), true, out _, out string compatibilityError), Is.False);
        Assert.That(compatibilityError, Is.Not.Empty);
    }

    [Test]
    public void InvalidHashAndUnsupportedFormatAreRejected()
    {
        Assert.That(Parse(Manifest("not-a-hash"), true, out _, out _), Is.False);
        Assert.That(Parse(Manifest().Replace("\"formatVersion\":1", "\"formatVersion\":2"), true, out _, out _), Is.False);
    }

    [Test]
    public void HttpNeedsExplicitDevelopmentPermission()
    {
        Assert.That(Parse(Manifest(), false, out _, out string error), Is.False);
        Assert.That(error, Is.Not.Empty);
        Assert.That(Parse(Manifest().Replace("http://", "https://"), false, out _, out _), Is.True);
    }

    private object resource;
    private static readonly BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static Delegate Capture<T>(Action<object, string> done) => new Action<T, string>((value, error) => done(value, error));
    private static object ReleaseCallback(Action<object, string> done)
        => typeof(RemoteStartupTests).GetMethod("Capture", Members).MakeGenericMethod(Runtime("RemoteContentRelease")).Invoke(null, new object[] { done });

    private object Resource => resource ?? (resource = Activator.CreateInstance(Runtime("ResourceManager")));
    private IEnumerator Fetch(string path, Action<object, string> done)
    {
        MethodInfo method = Runtime("ResourceManager").GetMethod("FetchRemoteReleaseAsync");
        Assert.That(method, Is.Not.Null, "HTTP 릴리스 조회 API가 필요합니다.");
        return RemotePreparationTests.Flatten((IEnumerator)method.Invoke(Resource, new object[] { Origin + path, "Windows", "1", true, ReleaseCallback(done) }));
    }
    private IEnumerator Catalog(object release, Action<bool, string> done)
    {
        MethodInfo method = Runtime("ResourceManager").GetMethod("LoadRemoteCatalogAsync");
        Assert.That(method, Is.Not.Null, "검증된 카탈로그 등록 API가 필요합니다.");
        return RemotePreparationTests.Flatten((IEnumerator)method.Invoke(Resource, new object[] { release, done }));
    }
    private static void RequireFixture()
    {
        if (string.IsNullOrEmpty(Origin)) Assert.Ignore("RunUnity.ps1의 격리 HTTP 환경에서 실행하세요.");
    }

    [TearDown]
    public void Cleanup()
    {
        if (resource != null) Runtime("ResourceManager").GetMethod("Clear").Invoke(resource, null);
        resource = null;
    }

    [UnityTest]
    public IEnumerator HttpReleaseChecksCompatibilityAndAllowsRetryAfter404()
    {
        RequireFixture();
        object release = null; string error = null;
        yield return Fetch("/missing", (value, reason) => { release = value; error = reason; });
        Assert.That(release, Is.Null);
        Assert.That(error, Is.Not.Empty);
        yield return Fetch("/release.json", (value, reason) => { release = value; error = reason; });
        Assert.That(release, Is.Not.Null, error);
        Assert.That(error, Is.Null);
    }

    [UnityTest]
    public IEnumerator TamperedCatalogIsRejectedBeforeAddressablesRegistration()
    {
        RequireFixture();
        Assert.That(Parse(Manifest(new string('0', 64)), true, out object release, out _), Is.True);
        bool success = true; string error = null;
        yield return Catalog(release, (ok, reason) => { success = ok; error = reason; });
        Assert.That(success, Is.False);
        Assert.That(error, Does.Contain("SHA-256"));
    }

    [UnityTest]
    public IEnumerator VerifiedCatalogProvidesLabelAndClearRemovesIt()
    {
        RequireFixture();
        object release = null;
        yield return Fetch("/release.json", (value, error) => release = value);
        Assert.That(release, Is.Not.Null);
        bool success = false; string error = null;
        yield return Catalog(release, (ok, reason) => { success = ok; error = reason; });
        Assert.That(success, Is.True, error);
        long size = -1;
        yield return (IEnumerator)Runtime("ResourceManager").GetMethod("GetDownloadSizeAsync").Invoke(Resource,
            new object[] { "remote-startup", new Action<long, string>((bytes, reason) => size = bytes) });
        Assert.That(size, Is.GreaterThanOrEqualTo(0));
        Runtime("ResourceManager").GetMethod("Clear").Invoke(Resource, null);
        yield return (IEnumerator)Runtime("ResourceManager").GetMethod("GetDownloadSizeAsync").Invoke(Resource,
            new object[] { "remote-startup", new Action<long, string>((bytes, reason) => size = bytes) });
        Assert.That(size, Is.EqualTo(-1));
    }

    [UnityTest]
    public IEnumerator ClearDuringCatalogRegistrationDoesNotLeaveLateLocatorAndAllowsReload()
    {
        RequireFixture();
        object release = null;
        yield return Fetch("/release.json", (value, error) => release = value);
        Assert.That(release, Is.Not.Null);
        bool primed = false;
        yield return Catalog(release, (ok, error) => primed = ok);
        Assert.That(primed, Is.True);
        Runtime("ResourceManager").GetMethod("Clear").Invoke(Resource, null);
        bool oldOk = true;
        IEnumerator old = Catalog(release, (ok, error) => oldOk = ok);
        old.MoveNext();
        object registration = Runtime("ResourceManager").GetField("catalogRequest", Members).GetValue(Resource);
        Assert.That(registration, Is.Not.Null);
        Assert.That((bool)registration.GetType().GetField("Settled").GetValue(registration), Is.False,
            "카탈로그 파일을 받은 뒤 실제 등록 작업 진행 중에 정리해야 합니다.");
        Runtime("ResourceManager").GetMethod("Clear").Invoke(Resource, null);
        yield return old;
        Assert.That(oldOk, Is.False);
        bool newOk = false; string errorMessage = null;
        yield return Catalog(release, (ok, error) => { newOk = ok; errorMessage = error; });
        Assert.That(newOk, Is.True, errorMessage);
        int locators = 0;
        foreach (var locator in UnityEngine.AddressableAssets.Addressables.ResourceLocators)
            if (locator.Locate("RemoteStartupPayload", typeof(TextAsset), out _)) locators++;
        Assert.That(locators, Is.EqualTo(1), "취소된 이전 등록은 locator를 남기거나 새 등록을 제거하면 안 됩니다.");
        var previousHandle = (UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator>)
            registration.GetType().GetField("Handle").GetValue(registration);
        Assert.That(previousHandle.IsValid(), Is.False);
    }

    [UnityTest]
    public IEnumerator OversizedOrRedirectedReleaseIsRejected()
    {
        RequireFixture();
        foreach (string route in new[] { "/large-release.json", "/chunked-release.json", "/redirect-release" })
        {
            object release = null; string error = null;
            yield return Fetch(route, (value, reason) => { release = value; error = reason; });
            Assert.That(release, Is.Null, route);
            Assert.That(error, Is.Not.Null.And.Not.Empty, route);
        }
    }

    [Test]
    public void MetadataReceiverRejectsOverflowBeforeRetainingTheChunk()
    {
        Type handlerType = Runtime("ResourceManager").GetNestedType("LimitedDownloadHandler", BindingFlags.NonPublic);
        Assert.That(handlerType, Is.Not.Null, "수신 단계에서 메타데이터 메모리 상한을 지켜야 합니다.");
        var handler = (IDisposable)Activator.CreateInstance(handlerType, new object[] { 65536 });
        try
        {
            MethodInfo receive = handlerType.GetMethod("ReceiveData", Members);
            var chunk = new byte[40000];
            Assert.That((bool)receive.Invoke(handler, new object[] { chunk, chunk.Length }), Is.True);
            Assert.That((bool)receive.Invoke(handler, new object[] { chunk, chunk.Length }), Is.False);
            var bytes = (byte[])handlerType.GetMethod("GetBytes").Invoke(handler, null);
            Assert.That(bytes.Length, Is.EqualTo(40000));
        }
        finally { handler.Dispose(); }
    }

    [UnityTest]
    public IEnumerator SameProcessRejectsDifferentContentRelease()
    {
        RequireFixture();
        object release = null;
        yield return Fetch("/release.json", (value, error) => release = value);
        bool loaded = false;
        yield return Catalog(release, (ok, error) => loaded = ok);
        Assert.That(loaded, Is.True);
        string json = File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE"), "release.json"));
        Assert.That(Parse(json.Replace("pilot-v1", "pilot-v2"), true, out object otherRelease, out _), Is.True);
        bool changed = true; string errorMessage = null;
        yield return Catalog(otherRelease, (ok, error) => { changed = ok; errorMessage = error; });
        Assert.That(changed, Is.False);
        Assert.That(errorMessage, Is.Not.Empty);
    }

    [UnityTest]
    public IEnumerator ClearAbortsInFlightReleaseRequestWithoutPublishingMetadata()
    {
        RequireFixture();
        object release = null; string error = null;
        IEnumerator request = Fetch("/slow-release.json", (value, reason) => { release = value; error = reason; });
        request.MoveNext();
        Runtime("ResourceManager").GetMethod("Clear").Invoke(Resource, null);
        yield return request;
        Assert.That(release, Is.Null);
        Assert.That(error, Is.Not.Empty);
    }
}
