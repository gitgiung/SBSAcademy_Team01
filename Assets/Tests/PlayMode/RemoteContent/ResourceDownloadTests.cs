using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;

// Assembly-CSharp를 새 asmdef로 옮기지 않고 실제 공개 계약을 검사한다.
public sealed class ResourceDownloadTests
{
    private object manager;
    private Type managerType;
    private ResourceLocationMap locator;
    private string label;
    private string key;
    private const string BundleFileName = "remote-download-fixture";
    private string bundleCacheName;
    private Hash128 bundleHash;
    private long bundleSize;
    private readonly List<IResourceProvider> addedProviders = new List<IResourceProvider>();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE")))
            Assert.Ignore("HTTP 통합 테스트는 tools/RemoteContentValidation/RunUnity.ps1의 격리 프로젝트에서 실행하세요.");
        managerType = Type.GetType("ResourceManager, Assembly-CSharp", true);
        manager = Activator.CreateInstance(managerType);
        var init = Addressables.InitializeAsync(false);
        yield return init;
        Assert.That(init.Status, Is.EqualTo(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded));
        Addressables.Release(init);
        // 빈 검증 카탈로그는 번들 공급자를 등록하지 않는다. 실제 표준 공급자로 HTTP 경계를 구성한다.
        EnsureProvider(new AssetBundleProvider());
        EnsureProvider(new BundledAssetProvider());
        label = "download-test-" + Guid.NewGuid().ToString("N");
        bundleCacheName = BundleFileName + "-" + label;
        key = label + "-asset";
        locator = new ResourceLocationMap(label);
        Addressables.AddResourceLocator(locator);
    }

    [TearDown]
    public void TearDown()
    {
        managerType?.GetMethod("Clear")?.Invoke(manager, null);
        if (locator != null) Addressables.RemoveResourceLocator(locator);
        if (bundleHash.isValid) Caching.ClearCachedVersion(bundleCacheName, bundleHash);
        bundleHash = default;
        foreach (IResourceProvider provider in addedProviders) Addressables.ResourceManager.ResourceProviders.Remove(provider);
        addedProviders.Clear();
    }

    private void EnsureProvider(IResourceProvider provider)
    {
        foreach (IResourceProvider existing in Addressables.ResourceManager.ResourceProviders)
            if (existing.ProviderId == provider.ProviderId) return;
        Addressables.ResourceManager.ResourceProviders.Add(provider);
        addedProviders.Add(provider);
    }

    [OneTimeTearDown]
    public void ClearSharedEditorFixtureCache()
    {
        string fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        if (string.IsNullOrEmpty(fixture)) return;
        var hash = Hash128.Parse(File.ReadAllText(Path.Combine(fixture, "hash.txt")).Trim());
        Caching.ClearCachedVersion(BundleFileName, hash);
    }

    private IEnumerator Size(string value, Action<long, string> done)
    {
        MethodInfo method = managerType.GetMethod("GetDownloadSizeAsync", new[] { typeof(string), typeof(Action<long, string>) });
        Assert.That(method, Is.Not.Null, "다운로드 전 용량 조회 API가 필요합니다.");
        return (IEnumerator)method.Invoke(manager, new object[] { value, done });
    }

    private IEnumerator Download(string value, Action<long, long> progress, Action<bool, string> done)
    {
        MethodInfo method = managerType.GetMethod("DownloadDependenciesAsync",
            new[] { typeof(string), typeof(Action<long, long>), typeof(Action<bool, string>) });
        Assert.That(method, Is.Not.Null, "메모리 로딩과 분리된 다운로드 API가 필요합니다.");
        return (IEnumerator)method.Invoke(manager, new object[] { value, progress, done });
    }

    private T Cached<T>() where T : UnityEngine.Object
        => (T)managerType.GetMethod("Load").MakeGenericMethod(typeof(T)).Invoke(manager, new object[] { key });

    private void AddFixture(bool missing = false)
    {
        // 일반 프로젝트에서는 수동 HTTP 통합 테스트를 명시적으로 실행할 때만 사용한다.
        string fixture = Environment.GetEnvironmentVariable("SBSA_REMOTE_FIXTURE");
        if (string.IsNullOrEmpty(fixture)) Assert.Ignore("tools/RemoteContentValidation/RunUnity.ps1로 HTTP 통합 환경을 준비하세요.");
        bundleHash = Hash128.Parse(File.ReadAllText(Path.Combine(fixture, "hash.txt")).Trim());
        bundleSize = new FileInfo(Path.Combine(fixture, BundleFileName)).Length;
        Caching.ClearCachedVersion(bundleCacheName, bundleHash);
        string origin = Environment.GetEnvironmentVariable("SBSA_REMOTE_ORIGIN");
        Assert.That(origin, Is.Not.Null.And.Not.Empty);
        var bundle = new ResourceLocationBase(label + "-bundle", origin + "/" + (missing ? "missing" : BundleFileName),
            typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource));
        bundle.Data = new AssetBundleRequestOptions
        {
            BundleName = bundleCacheName, Hash = bundleHash.ToString(), BundleSize = bundleSize,
            Timeout = 30, RetryCount = 0, UseCrcForCachedBundle = false
        };
        var asset = new ResourceLocationBase(key, "assets/fixture/payload.txt",
            typeof(BundledAssetProvider).FullName, typeof(TextAsset), bundle);
        locator.Add(label, asset);
        locator.Add(key, asset);
    }

    [UnityTest]
    public IEnumerator BlankSizeFailsInsteadOfPretendingTheCacheIsReady()
    {
        long bytes = 0; string error = null;
        yield return Size("  ", (count, reason) => { bytes = count; error = reason; });
        Assert.That(bytes, Is.EqualTo(-1));
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [UnityTest]
    public IEnumerator MissingLabelSizeFailsInsteadOfReportingZeroBytes()
    {
        long bytes = 0; string error = null;
        yield return Size(label, (count, reason) => { bytes = count; error = reason; });
        Assert.That(bytes, Is.EqualTo(-1));
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [UnityTest]
    public IEnumerator BlankDownloadFails()
    {
        bool success = true; string error = null;
        yield return Download(null, null, (ok, reason) => { success = ok; error = reason; });
        Assert.That(success, Is.False);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [UnityTest]
    public IEnumerator MissingDownloadFailsAndCanBeRequestedAgain()
    {
        int failures = 0;
        for (int i = 0; i < 2; i++)
            yield return Download(label, null, (ok, reason) => { if (!ok && !string.IsNullOrEmpty(reason)) failures++; });
        Assert.That(failures, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator ClearInvalidatesPendingSize()
    {
        string error = null; long bytes = 0;
        IEnumerator request = Size(label, (count, reason) => { bytes = count; error = reason; });
        request.MoveNext();
        managerType.GetMethod("Clear").Invoke(manager, null);
        yield return request;
        Assert.That(bytes, Is.EqualTo(-1));
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [UnityTest]
    public IEnumerator HttpDownloadFillsDiskCacheWithoutLoadingAssetIntoManager()
    {
        AddFixture();
        long before = -1;
        yield return Size(" " + label + " ", (count, reason) => { Assert.That(reason, Is.Null); before = count; });
        Assert.That(before, Is.EqualTo(bundleSize).And.GreaterThan(0));
        long finalReceived = -1; long finalTotal = -1; bool success = false; string error = null;
        yield return Download(label, (received, total) => { finalReceived = received; finalTotal = total; },
            (ok, reason) => { success = ok; error = reason; });
        Assert.That(success, Is.True, error);
        Assert.That(finalReceived, Is.EqualTo(finalTotal));
        Assert.That(finalTotal, Is.GreaterThan(0));
        Assert.That(Cached<TextAsset>(), Is.Null, "디스크 다운로드가 에셋 메모리 캐시를 채우면 안 됩니다.");
        long after = -1;
        yield return Size(label, (count, reason) => { after = count; Assert.That(reason, Is.Null); });
        Assert.That(after, Is.Zero);
        bool loaded = false;
        var preload = (IEnumerator)managerType.GetMethod("PreloadAsync").Invoke(manager,
            new object[] { label, new Action<bool, string>((ok, reason) => loaded = ok) });
        yield return preload;
        Assert.That(loaded, Is.True);
        Assert.That(Cached<TextAsset>().text, Does.StartWith("SBSA remote fixture\n"));
    }

    [UnityTest]
    public IEnumerator ConcurrentDownloadsHaveIndependentCompletionAndProgress()
    {
        AddFixture();
        int completed = 0; int firstProgress = 0; int secondProgress = 0;
        bool firstOk = false; bool secondOk = false;
        IEnumerator first = Download(label, (a, b) => firstProgress++, (ok, e) => { firstOk = ok; completed++; });
        IEnumerator second = Download(label, (a, b) => secondProgress++, (ok, e) => { secondOk = ok; completed++; });
        while (true)
        {
            bool a = first.MoveNext(); bool b = second.MoveNext();
            if (!a && !b) break;
            yield return null;
        }
        Assert.That(completed, Is.EqualTo(2));
        Assert.That(firstOk && secondOk, Is.True);
        Assert.That(firstProgress, Is.GreaterThan(0));
        Assert.That(secondProgress, Is.GreaterThan(0));
        Assert.That(Cached<TextAsset>(), Is.Null);
    }

    [UnityTest]
    public IEnumerator ClearCancelsDownloadAndLateCompletionDoesNotRefillMemory()
    {
        AddFixture();
        bool ok = true; string error = null;
        IEnumerator request = Download(label, null, (success, reason) => { ok = success; error = reason; });
        request.MoveNext();
        yield return null;
        managerType.GetMethod("Clear").Invoke(manager, null);
        yield return request;
        Assert.That(ok, Is.False);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
        Assert.That(Cached<TextAsset>(), Is.Null);
    }

    [UnityTest]
    public IEnumerator ClearFromProgressCancelsThisCaller()
    {
        AddFixture();
        bool ok = true; string error = null;
        yield return Download(label, (a, b) => managerType.GetMethod("Clear").Invoke(manager, null),
            (success, reason) => { ok = success; error = reason; });
        Assert.That(ok, Is.False);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }

    [UnityTest]
    public IEnumerator ClearDoesNotDeleteDiskCacheAndExplicitCacheRemovalRestoresDownloadSize()
    {
        AddFixture();
        bool success = false;
        yield return Download(label, null, (ok, reason) => success = ok);
        Assert.That(success, Is.True);
        managerType.GetMethod("Clear").Invoke(manager, null);
        long cached = -1;
        yield return Size(label, (bytes, error) => cached = bytes);
        Assert.That(cached, Is.Zero);
        Assert.That(Caching.ClearCachedVersion(bundleCacheName, bundleHash), Is.True);
        long removed = -1;
        yield return Size(label, (bytes, error) => removed = bytes);
        Assert.That(removed, Is.EqualTo(bundleSize));
    }

    [UnityTest]
    public IEnumerator AbandonedWaiterDoesNotPreventDownloadCompletionAndCleanup()
    {
        AddFixture();
        bool called = false;
        IEnumerator request = Download(label, null, (ok, reason) => called = true);
        Assert.That(request.MoveNext(), Is.True);
        var pending = (IDictionary)managerType.GetField("downloads", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
        var entries = pending.Values.GetEnumerator();
        Assert.That(entries.MoveNext(), Is.True);
        object ownedRequest = entries.Current;
        (request as IDisposable)?.Dispose();
        float deadline = Time.realtimeSinceStartup + 30f;
        while ((!Caching.IsVersionCached(bundleCacheName, bundleHash) || pending.Count > 0)
            && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Caching.IsVersionCached(bundleCacheName, bundleHash), Is.True);
        Assert.That(called, Is.False, "파괴된 대기자의 콜백은 호출하지 않습니다.");
        Assert.That(pending.Count, Is.Zero);
        Type requestType = ownedRequest.GetType();
        Assert.That((bool)requestType.GetField("OwnsLookup").GetValue(ownedRequest), Is.False);
        Assert.That((bool)requestType.GetField("OwnsWork").GetValue(ownedRequest), Is.False);
        var work = (UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle)requestType.GetField("Work").GetValue(ownedRequest);
        Assert.That(work.IsValid(), Is.False, "대기자 이탈 후에도 완료 작업 핸들을 반환해야 합니다.");
        bool success = false;
        yield return Download(label, null, (ok, error) => success = ok);
        Assert.That(success, Is.True);
    }

    [UnityTest]
    public IEnumerator ProgressExceptionDoesNotPreventAnotherWaiterFromCompleting()
    {
        AddFixture();
        bool thrown = false; bool firstOk = false; bool secondOk = false;
        LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("download progress probe"));
        IEnumerator first = Download(label, (a, b) =>
        {
            if (thrown) return;
            thrown = true;
            throw new InvalidOperationException("download progress probe");
        }, (ok, error) => firstOk = ok);
        IEnumerator second = Download(label, null, (ok, error) => secondOk = ok);
        while (true)
        {
            bool a = first.MoveNext(); bool b = second.MoveNext();
            if (!a && !b) break;
            yield return null;
        }
        Assert.That(firstOk && secondOk, Is.True);
    }

    [UnityTest]
    public IEnumerator NewRequestFromCanceledCallbackSurvivesOldCompletion()
    {
        AddFixture();
        IEnumerator next = null; bool success = false;
        long received = 0; long total = 0;
        IEnumerator previous = Download(label, (bytes, size) => { received = bytes; total = size; }, (ok, error) =>
        {
            Assert.That(ok, Is.False);
            next = Download(label, null, (newOk, newError) => success = newOk);
            next.MoveNext();
        });
        float deadline = Time.realtimeSinceStartup + 30f;
        while (previous.MoveNext() && received == 0 && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(received, Is.GreaterThan(0).And.LessThan(total), "HTTP 본문을 일부 받은 시점에 취소해야 합니다.");
        managerType.GetMethod("Clear").Invoke(manager, null);
        yield return previous;
        Assert.That(next, Is.Not.Null);
        yield return next;
        Assert.That(success, Is.True);
    }

    [UnityTest]
    public IEnumerator HttpFailureIsReportedAndDoesNotBecomeCachedSuccess()
    {
        AddFixture(true);
        // 예상 404와 이를 감싼 Addressables 의존 작업 실패만 허용한다. 핸들/해제 오류는 숨기지 않는다.
        var previous = UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler;
        try
        {
            UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = (handle, exception) =>
            {
                string detail = exception.ToString();
                if (detail.Contains("/missing") && detail.Contains("404")) return;
                if (previous != null) previous(handle, exception);
                else Debug.LogException(exception);
            };
            for (int i = 0; i < 2; i++)
            {
                bool ok = true; string error = null;
                yield return Download(label, null, (success, reason) => { ok = success; error = reason; });
                Assert.That(ok, Is.False);
                Assert.That(error, Is.Not.Null.And.Not.Empty);
            }
            Assert.That(Caching.IsVersionCached(bundleCacheName, bundleHash), Is.False);
        }
        finally { UnityEngine.ResourceManagement.ResourceManager.ExceptionHandler = previous; }
        locator.Locations.Remove(label);
        locator.Locations.Remove(key);
        AddFixture();
        bool recovered = false;
        yield return Download(label, null, (ok, error) => recovered = ok);
        Assert.That(recovered, Is.True, "실패 결과를 보관하지 않고 같은 라벨로 재시도해야 합니다.");
    }
}
