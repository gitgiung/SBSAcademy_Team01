using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public partial class ResourceManager
{
    // 다운로드는 디스크 캐시만 준비한다. _resources의 메모리 에셋 캐시와 섞지 않는다.
    private readonly Dictionary<string, DownloadRequest> downloads = new Dictionary<string, DownloadRequest>();

    /// <summary>필요한 추가 다운로드 바이트를 조회한다. 실패는 -1과 오류를 전달한다.</summary>
    public IEnumerator GetDownloadSizeAsync(string label, Action<long, string> completed)
    {
        DownloadRequest request = BeginDownload(label, true);
        while (!request.Finished) yield return null;
        string error = DownloadError(request);
        InvokeSafely(() => completed?.Invoke(error == null ? request.TotalBytes : -1, error));
    }

    /// <summary>
    /// 사전에 용량을 조회하고 사용자의 동의를 받은 뒤 호출한다. 진행은 수신/전체 바이트다.
    /// 완료 후에도 에셋 사용 전 PreloadAsync 등의 메모리 로딩이 필요하다.
    /// </summary>
    public IEnumerator DownloadDependenciesAsync(string label, Action<long, long> progress,
        Action<bool, string> completed)
    {
        DownloadRequest request = BeginDownload(label, false);
        long reportedBytes = -1;
        long reportedTotal = -1;
        while (!request.Finished)
        {
            UpdateDownloadProgress(request);
            ReportDownloadProgress(request, progress, ref reportedBytes, ref reportedTotal);
            if (!request.Finished) yield return null;
        }
        ReportDownloadProgress(request, progress, ref reportedBytes, ref reportedTotal);
        string error = DownloadError(request);
        InvokeSafely(() => completed?.Invoke(error == null, error));
    }

    private string DownloadError(DownloadRequest request)
        => request.Generation != _generation ? "리소스 다운로드가 정리되었습니다." : request.Error;

    private DownloadRequest BeginDownload(string label, bool sizeOnly)
    {
        string trimmed = label?.Trim();
        string operationKey = (sizeOnly ? "size:" : "download:") + trimmed;
        if (downloads.TryGetValue(operationKey, out DownloadRequest pending)) return pending;
        var request = new DownloadRequest { Key = operationKey, Generation = _generation, SizeOnly = sizeOnly };
        if (string.IsNullOrEmpty(trimmed))
        {
            FinishDownload(request, "리소스 라벨이 비어 있습니다.");
            return request;
        }
        downloads.Add(operationKey, request);
        try
        {
            var lookup = Addressables.LoadResourceLocationsAsync(trimmed, typeof(UnityEngine.Object));
            request.Lookup = lookup;
            request.OwnsLookup = true;
            lookup.Completed += operation => OnDownloadLocations(request, operation);
        }
        catch (Exception error) { FinishDownload(request, error.Message); }
        return request;
    }

    private void OnDownloadLocations(DownloadRequest request, AsyncOperationHandle<IList<IResourceLocation>> operation)
    {
        if (request.Finished) return;
        try
        {
            if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null || operation.Result.Count == 0)
            {
                FinishDownload(request, "리소스 라벨의 다운로드 대상을 찾지 못했습니다.");
                return;
            }
            request.Locations = new List<IResourceLocation>(operation.Result);
            var size = Addressables.GetDownloadSizeAsync((IEnumerable)request.Locations);
            request.Work = size;
            request.OwnsWork = true;
            size.Completed += result => OnDownloadSize(request, result);
        }
        catch (Exception error) { FinishDownload(request, error.Message); }
        finally { ReleaseDownloadLookup(request); }
    }

    private void OnDownloadSize(DownloadRequest request, AsyncOperationHandle<long> operation)
    {
        if (request.Finished) return;
        if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result < 0)
        {
            FinishDownload(request, "다운로드 용량 조회 실패: " + operation.OperationException?.Message);
            return;
        }
        request.TotalBytes = operation.Result;
        ReleaseDownloadWork(request);
        if (request.SizeOnly)
        {
            FinishDownload(request, null);
            return;
        }
        try
        {
            var download = Addressables.DownloadDependenciesAsync(request.Locations, false);
            request.Work = download;
            request.OwnsWork = true;
            request.IsDownloading = true;
            download.Completed += result =>
            {
                if (request.Finished) return;
                UpdateDownloadProgress(request);
                if (result.Status == AsyncOperationStatus.Succeeded)
                {
                    request.ReceivedBytes = request.TotalBytes;
                    FinishDownload(request, null);
                }
                else FinishDownload(request, "리소스 다운로드 실패: " + result.OperationException?.Message);
            };
        }
        catch (Exception error) { FinishDownload(request, error.Message); }
    }

    private static void UpdateDownloadProgress(DownloadRequest request)
    {
        if (!request.IsDownloading || !request.OwnsWork || !request.Work.IsValid()) return;
        var status = request.Work.GetDownloadStatus();
        if (status.TotalBytes > 0) request.TotalBytes = status.TotalBytes;
        request.ReceivedBytes = status.DownloadedBytes;
    }

    private static void ReportDownloadProgress(DownloadRequest request, Action<long, long> callback,
        ref long reportedBytes, ref long reportedTotal)
    {
        if (reportedBytes == request.ReceivedBytes && reportedTotal == request.TotalBytes) return;
        reportedBytes = request.ReceivedBytes;
        reportedTotal = request.TotalBytes;
        // 콜백이 Clear를 호출해도 이 요청은 다음 대기 단계에서 실패로 종료된다.
        long received = reportedBytes;
        long total = reportedTotal;
        InvokeSafely(() => callback?.Invoke(received, total));
    }

    private void FinishDownload(DownloadRequest request, string error)
    {
        if (request.Finished) return;
        request.Finished = true;
        request.Error = error;
        if (downloads.TryGetValue(request.Key, out DownloadRequest current) && ReferenceEquals(current, request))
            downloads.Remove(request.Key);
        ReleaseDownloadLookup(request);
        ReleaseDownloadWork(request);
        request.Locations = null;
    }

    private static void ReleaseDownloadLookup(DownloadRequest request)
    {
        if (!request.OwnsLookup) return;
        request.OwnsLookup = false;
        if (request.Lookup.IsValid()) Addressables.Release(request.Lookup);
    }

    private static void ReleaseDownloadWork(DownloadRequest request)
    {
        if (!request.OwnsWork) return;
        request.OwnsWork = false;
        if (request.Work.IsValid()) Addressables.Release(request.Work);
    }

    private void ClearDownloads()
    {
        var previous = new List<DownloadRequest>(downloads.Values);
        downloads.Clear();
        // 먼저 전부 종료 표시해야 Release 과정에서 도착하는 완료 콜백도 이전 작업을 재개하지 않는다.
        foreach (DownloadRequest request in previous)
        {
            request.Finished = true;
            request.Error = "리소스 다운로드가 정리되었습니다.";
        }
        foreach (DownloadRequest request in previous)
        {
            ReleaseDownloadLookup(request);
            ReleaseDownloadWork(request);
            request.Locations = null;
        }
    }

    private sealed class DownloadRequest
    {
        public string Key;
        public int Generation;
        public bool SizeOnly;
        public bool Finished;
        public string Error;
        public long TotalBytes;
        public long ReceivedBytes;
        public bool IsDownloading;
        public bool OwnsLookup;
        public bool OwnsWork;
        public AsyncOperationHandle Lookup;
        public AsyncOperationHandle Work;
        public IList<IResourceLocation> Locations;
    }
}
