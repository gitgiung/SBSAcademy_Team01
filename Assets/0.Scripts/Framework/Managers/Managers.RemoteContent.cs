using System;
using System.Collections;

public enum RemoteContentState
{
    Disabled, CheckingRelease, LoadingCatalog, CheckingSize, AwaitingConsent, Downloading, Ready, Failed
}

/// <summary>읽기 전용 UI snapshot. Ready는 필수 번들 디스크 준비이며 GameDataReady와 다르다.</summary>
public readonly struct RemoteContentStatus
{
    public RemoteContentState State { get; }
    public long RequiredBytes { get; }
    public long ReceivedBytes { get; }
    public string Error { get; }
    public string ContentVersion { get; }
    public RemoteContentStatus(RemoteContentState state, long required, long received, string error, string version)
    { State = state; RequiredBytes = required; ReceivedBytes = received; Error = error; ContentVersion = version; }
}

public partial class Managers
{
    private RemoteOptions remoteOptions;
    private RemoteContentRelease remoteRelease;
    private RemoteContentState remoteState;
    private bool remotePreparing;
    private bool remoteApproved;
    private long remoteRequiredBytes;
    private long remoteReceivedBytes;
    private string remoteError;
    private int remoteGeneration;

    // 상태 조회로 Managers를 생성하지 않는다.
    public static RemoteContentStatus RemoteStatus => s_instance == null
        ? new RemoteContentStatus(RemoteContentState.Disabled, 0, 0, null, null)
        : new RemoteContentStatus(s_instance.remoteState, s_instance.remoteRequiredBytes,
            s_instance.remoteReceivedBytes, s_instance.remoteError, s_instance.remoteRelease?.contentVersion);

    public static IEnumerator PrepareRemoteContent()
    {
        if (s_instance != null && s_instance.remoteOptions != null)
        {
            yield return s_instance.WaitForRemoteContent(s_instance.remoteOptions);
            yield break;
        }
        yield return PrepareRemoteContent(RemoteContentSettings.Load());
    }

    public static IEnumerator PrepareRemoteContent(RemoteContentSettings settings)
    {
        Init();
        Managers owner = s_instance;
        if (owner == null) yield break;
        if (settings == null || !settings.UseRemoteContent)
        {
            if (owner.remoteOptions != null)
                throw new InvalidOperationException("원격 준비 설정을 바꾸려면 앱을 다시 실행하세요.");
            owner.remoteState = RemoteContentState.Disabled;
            yield break;
        }
        var options = new RemoteOptions(settings);
        yield return owner.WaitForRemoteContent(options);
    }

    private IEnumerator WaitForRemoteContent(RemoteOptions options)
    {
        if (remoteOptions != null && !remoteOptions.Matches(options))
            throw new InvalidOperationException("다른 원격 준비 설정을 적용하려면 앱을 다시 실행하세요.");
        if (remoteOptions == null)
        {
            remoteOptions = options;
            StartRemotePreparation();
        }
        else if (remoteState == RemoteContentState.Ready && remoteGeneration != _resource.Generation)
            StartRemotePreparation();
        while (this != null && remotePreparing) yield return null;
    }

    public static bool ApproveRemoteDownload()
    {
        if (s_instance == null || s_instance.remoteState != RemoteContentState.AwaitingConsent) return false;
        s_instance.remoteApproved = true;
        return true;
    }

    public static bool RetryRemoteContent()
    {
        if (s_instance == null || s_instance.remoteOptions == null || s_instance.remotePreparing
            || s_instance.remoteState != RemoteContentState.Failed) return false;
        s_instance.StartRemotePreparation();
        return true;
    }

    private void StartRemotePreparation()
    {
        remotePreparing = true;
        remoteApproved = false;
        remoteError = null;
        remoteReceivedBytes = 0;
        remoteRequiredBytes = 0;
        remoteState = RemoteContentState.CheckingRelease;
        StartCoroutine(PrepareRemoteRoutine());
    }

    private IEnumerator PrepareRemoteRoutine()
    {
        int generation = _resource.Generation;
        try
        {
            string error = null;
            if (remoteRelease == null)
            {
                RemoteContentRelease release = null;
                yield return _resource.FetchRemoteReleaseAsync(remoteOptions.Url, remoteOptions.Platform,
                    remoteOptions.Compatibility, remoteOptions.AllowHttp, (value, reason) => { release = value; error = reason; });
                if (!RemoteStepValid(generation, error)) yield break;
                if (release == null) { FailRemote("유효한 릴리스 정보가 없습니다."); yield break; }
                remoteRelease = release;
            }
            remoteState = RemoteContentState.LoadingCatalog;
            bool loaded = false;
            yield return _resource.LoadRemoteCatalogAsync(remoteRelease, (ok, reason) => { loaded = ok; error = reason; });
            if (!RemoteStepValid(generation, error)) yield break;
            if (!loaded) { FailRemote("카탈로그를 준비하지 못했습니다."); yield break; }

            remoteState = RemoteContentState.CheckingSize;
            long required = -1;
            yield return _resource.GetDownloadSizeAsync(remoteRelease.requiredLabel,
                (bytes, reason) => { required = bytes; error = reason; });
            if (!RemoteStepValid(generation, error)) yield break;
            if (required < 0) { FailRemote("추가 다운로드 용량을 확인하지 못했습니다."); yield break; }
            remoteRequiredBytes = required;
            if (required > 0)
            {
                remoteState = RemoteContentState.AwaitingConsent;
                while (!remoteApproved)
                {
                    if (!RemoteStepValid(generation, null)) yield break;
                    yield return null;
                }
                // 동의 대기 중 Clear가 발생한 프레임에도 이전 세대로 전송하지 않는다.
                if (!RemoteStepValid(generation, null)) yield break;
                remoteState = RemoteContentState.Downloading;
                bool downloaded = false;
                yield return _resource.DownloadDependenciesAsync(remoteRelease.requiredLabel, (received, total) =>
                {
                    if (generation != _resource.Generation) return;
                    remoteReceivedBytes = received;
                    remoteRequiredBytes = total;
                }, (ok, reason) => { downloaded = ok; error = reason; });
                if (!RemoteStepValid(generation, error)) yield break;
                if (!downloaded) { FailRemote("필수 리소스 다운로드에 실패했습니다."); yield break; }
            }
            // 완료 표시만 믿지 않고, 실제 필수 번들의 추가 다운로드가 없는지 다시 확인한다.
            yield return _resource.GetDownloadSizeAsync(remoteRelease.requiredLabel,
                (bytes, reason) => { required = bytes; error = reason; });
            if (!RemoteStepValid(generation, error)) yield break;
            if (required != 0) { FailRemote("필수 리소스가 아직 준비되지 않았습니다. 다시 시도하세요."); yield break; }
            remoteGeneration = generation;
            remoteState = RemoteContentState.Ready;
        }
        finally
        {
            remotePreparing = false;
            if (this != null && remoteState != RemoteContentState.Ready && remoteState != RemoteContentState.Failed)
                FailRemote("원격 준비가 완료되기 전에 중단되었습니다.");
        }
    }

    private bool RemoteStepValid(int generation, string error)
    {
        if (this == null) return false;
        if (generation != _resource.Generation) error = "원격 준비 중 리소스가 정리되었습니다. 다시 시도하세요.";
        if (error == null) return true;
        FailRemote(error);
        return false;
    }

    private void FailRemote(string error)
    {
        remoteError = error;
        remoteState = RemoteContentState.Failed;
    }

    private sealed class RemoteOptions
    {
        public readonly string Url;
        public readonly string Platform;
        public readonly string Compatibility;
        public readonly bool AllowHttp;
        public RemoteOptions(RemoteContentSettings settings)
        { Url = settings.ReleaseUrl; Platform = settings.ContentPlatform; Compatibility = settings.AppCompatibility; AllowHttp = settings.AllowDevelopmentHttp; }
        public bool Matches(RemoteOptions other) => Url == other.Url && Platform == other.Platform
            && Compatibility == other.Compatibility && AllowHttp == other.AllowHttp;
    }
}
