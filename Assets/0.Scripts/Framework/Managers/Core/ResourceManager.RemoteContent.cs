using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class ResourceManager
{
    private readonly HashSet<RemoteHttpRequest> remoteHttpRequests = new HashSet<RemoteHttpRequest>();
    private string remoteCatalogIdentity;
    private IResourceLocator remoteLocator;
    private CatalogRequest catalogRequest;

    public IEnumerator FetchRemoteReleaseAsync(string url, string platform, string compatibility, bool allowHttp,
        Action<RemoteContentRelease, string> completed)
    {
        bool developmentHttp = allowHttp && (Application.isEditor || Debug.isDebugBuild);
        if (!RemoteContentRelease.IsAllowedUrl(url, developmentHttp))
        {
            InvokeSafely(() => completed?.Invoke(null, "릴리스 주소는 허용된 HTTPS 주소여야 합니다."));
            yield break;
        }
        byte[] bytes = null; string error = null;
        yield return FetchRemoteBytes(url, 64 * 1024, (value, reason) => { bytes = value; error = reason; });
        RemoteContentRelease release = null;
        if (error == null)
            RemoteContentRelease.TryParse(Encoding.UTF8.GetString(bytes), platform, compatibility, developmentHttp, out release, out error);
        InvokeSafely(() => completed?.Invoke(release, error));
    }

    public IEnumerator LoadRemoteCatalogAsync(RemoteContentRelease release, Action<bool, string> completed)
    {
        RemoteContentRelease validated = null; string validationError = "릴리스 정보가 없습니다.";
        if (release != null)
            RemoteContentRelease.TryParse(JsonUtility.ToJson(release), release.platform, release.appCompatibility,
                release.AllowHttp && (Application.isEditor || Debug.isDebugBuild), out validated, out validationError);
        if (validated == null)
        {
            InvokeSafely(() => completed?.Invoke(false, validationError));
            yield break;
        }
        release = validated;
        if (remoteCatalogIdentity != null && remoteCatalogIdentity != release.Identity)
        {
            InvokeSafely(() => completed?.Invoke(false, "다른 콘텐츠 릴리스를 적용하려면 앱을 다시 실행하세요."));
            yield break;
        }
        remoteCatalogIdentity = release.Identity;
        int generation = _generation;
        // 취소된 등록이 늦게 locator를 추가한 뒤 새 등록을 지우는 경쟁을 피한다.
        while (catalogRequest != null && !catalogRequest.Settled) yield return null;
        if (generation != _generation)
        {
            InvokeSafely(() => completed?.Invoke(false, "카탈로그 준비가 정리되었습니다."));
            yield break;
        }
        if (remoteLocator != null)
        {
            InvokeSafely(() => completed?.Invoke(true, null));
            yield break;
        }
        string cachePath = Path.Combine(Application.persistentDataPath, "RemoteContentCatalogs", release.catalogSha256 + ".bin");
        byte[] bytes = null; string error = null;
        if (!TryReadCatalog(cachePath, release.catalogSha256))
        {
            yield return FetchRemoteBytes(release.catalogUrl, 16 * 1024 * 1024,
                (value, reason) => { bytes = value; error = reason; });
            if (error == null && Hash(bytes) != release.catalogSha256) error = "카탈로그 SHA-256 검증에 실패했습니다.";
            if (error == null) TryWriteCatalog(cachePath, bytes, out error);
        }
        if (error == null && generation != _generation) error = "카탈로그 준비가 정리되었습니다.";
        if (error != null)
        {
            InvokeSafely(() => completed?.Invoke(false, error));
            yield break;
        }
        // HTTP 대기 중 다른 호출자가 같은 릴리스를 등록했을 수 있다.
        while (catalogRequest != null && !catalogRequest.Settled) yield return null;
        if (generation != _generation)
        {
            InvokeSafely(() => completed?.Invoke(false, "카탈로그 준비가 정리되었습니다."));
            yield break;
        }
        if (remoteLocator != null)
        {
            InvokeSafely(() => completed?.Invoke(true, null));
            yield break;
        }
        CatalogRequest request = BeginCatalogRegistration(cachePath);
        while (!request.Finished) yield return null;
        string resultError = generation == _generation ? request.Error : "카탈로그 준비가 정리되었습니다.";
        InvokeSafely(() => completed?.Invoke(resultError == null, resultError));
    }

    private CatalogRequest BeginCatalogRegistration(string path)
    {
        var request = new CatalogRequest { Generation = _generation };
        catalogRequest = request;
        try
        {
            request.Handle = Addressables.LoadContentCatalogAsync(path, false);
            request.OwnsHandle = true;
            request.Handle.Completed += operation =>
            {
                bool success = operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null;
                if (request.Generation != _generation || request.Finished)
                {
                    if (success) Addressables.RemoveResourceLocator(operation.Result);
                    request.Error = "카탈로그 준비가 정리되었습니다.";
                }
                else if (success) remoteLocator = operation.Result;
                else request.Error = "카탈로그 등록 실패: " + operation.OperationException?.Message;
                request.Finished = true;
                request.Settled = true;
                if (request.OwnsHandle)
                {
                    request.OwnsHandle = false;
                    if (request.Handle.IsValid()) Addressables.Release(request.Handle);
                }
            };
        }
        catch (Exception exception)
        {
            request.Error = "카탈로그 등록 실패: " + exception.Message;
            request.Finished = true;
            request.Settled = true;
            if (request.OwnsHandle)
            {
                request.OwnsHandle = false;
                if (request.Handle.IsValid()) Addressables.Release(request.Handle);
            }
        }
        return request;
    }

    private IEnumerator FetchRemoteBytes(string url, int maximumBytes, Action<byte[], string> completed)
    {
        int generation = _generation;
        var handler = new LimitedDownloadHandler(maximumBytes);
        var request = new RemoteHttpRequest
        {
            Web = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, handler, null), Owned = true
        };
        request.Web.timeout = 15;
        request.Web.redirectLimit = 0;
        remoteHttpRequests.Add(request);
        byte[] bytes = null; string error = null;
        try
        {
            var operation = request.Web.SendWebRequest();
            while (request.Owned && generation == _generation && !operation.isDone)
            {
                if (handler.TooLarge)
                {
                    error = "원격 메타데이터가 허용 크기를 초과했습니다.";
                    request.Web.Abort();
                    break;
                }
                yield return null;
            }
            if (!request.Owned || generation != _generation) error = "원격 요청이 정리되었습니다.";
            else if (handler.TooLarge) error = "원격 메타데이터가 허용 크기를 초과했습니다.";
            else if (error == null && request.Web.result != UnityWebRequest.Result.Success)
                error = "원격 정보 요청 실패 (HTTP " + request.Web.responseCode + "): " + request.Web.error;
            else if (error == null)
            {
                bytes = handler.GetBytes();
                if (bytes == null || bytes.Length == 0 || bytes.Length > maximumBytes)
                {
                    error = "원격 메타데이터가 비어 있거나 허용 크기를 초과했습니다.";
                    bytes = null;
                }
            }
        }
        finally
        {
            remoteHttpRequests.Remove(request);
            DisposeRemoteHttp(request);
        }
        InvokeSafely(() => completed?.Invoke(bytes, error));
    }

    private static void DisposeRemoteHttp(RemoteHttpRequest request)
    {
        if (!request.Owned) return;
        request.Owned = false;
        request.Web.Dispose();
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static bool TryReadCatalog(string path, string expectedHash)
    {
        try { return File.Exists(path) && new FileInfo(path).Length <= 16 * 1024 * 1024 && Hash(File.ReadAllBytes(path)) == expectedHash; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static void TryWriteCatalog(string path, byte[] bytes, out string error)
    {
        error = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception) { error = "카탈로그 캐시를 저장하지 못했습니다. 저장 공간과 접근 권한을 확인하세요."; }
    }

    private void ClearRemoteContent()
    {
        var requests = new List<RemoteHttpRequest>(remoteHttpRequests);
        remoteHttpRequests.Clear();
        foreach (RemoteHttpRequest request in requests)
        {
            if (request.Owned) request.Web.Abort();
            DisposeRemoteHttp(request);
        }
        if (catalogRequest != null && !catalogRequest.Settled)
        {
            // LoadContentCatalogAsync는 중간 취소 API가 없다. 늦은 locator 정리 후 핸들을 반환한다.
            catalogRequest.Error = "카탈로그 준비가 정리되었습니다.";
            catalogRequest.Finished = true;
        }
        if (remoteLocator != null) Addressables.RemoveResourceLocator(remoteLocator);
        remoteLocator = null;
    }

    private sealed class RemoteHttpRequest
    {
        public UnityWebRequest Web;
        public bool Owned;
    }

    private sealed class LimitedDownloadHandler : DownloadHandlerScript
    {
        private readonly int maximumBytes;
        private readonly MemoryStream buffer = new MemoryStream();
        public bool TooLarge { get; private set; }
        public LimitedDownloadHandler(int maximumBytes) : base(new byte[8192]) { this.maximumBytes = maximumBytes; }
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (TooLarge) return false;
            if (data == null || dataLength == 0) return true;
            if (buffer.Length + dataLength > maximumBytes) { TooLarge = true; return false; }
            buffer.Write(data, 0, dataLength);
            return true;
        }
        public byte[] GetBytes() => buffer.ToArray();
    }

    private sealed class CatalogRequest
    {
        public int Generation;
        public bool Finished;
        public bool Settled;
        public bool OwnsHandle;
        public string Error;
        public AsyncOperationHandle<IResourceLocator> Handle;
    }
}
