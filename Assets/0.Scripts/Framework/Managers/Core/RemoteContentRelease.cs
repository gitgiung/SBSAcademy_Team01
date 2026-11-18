using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>코드가 지원하는 릴리스 메타데이터 v1. 게임 밸런스 데이터와는 별개다.</summary>
[Serializable]
public sealed class RemoteContentRelease
{
    public int formatVersion;
    public string platform;
    public string appCompatibility;
    public string contentVersion;
    public string catalogUrl;
    public string catalogSha256;
    public string requiredLabel;
    public string contentProfile;
    public bool AllowHttp { get; private set; }

    public string Identity => platform + "\n" + appCompatibility + "\n" + contentVersion + "\n"
        + catalogUrl + "\n" + catalogSha256 + "\n" + requiredLabel + "\n" + contentProfile;

    public static bool TryParse(string json, string expectedPlatform, string compatibility, bool allowHttp,
        out RemoteContentRelease release, out string error)
    {
        release = null;
        error = null;
        RemoteContentRelease candidate;
        try { candidate = JsonUtility.FromJson<RemoteContentRelease>(json); }
        catch (Exception) { error = "릴리스 정보 형식이 올바르지 않습니다."; return false; }
        if (candidate == null || candidate.formatVersion != 1)
            error = "지원하지 않는 릴리스 정보 버전입니다.";
        else if (string.IsNullOrWhiteSpace(expectedPlatform) || string.IsNullOrWhiteSpace(compatibility)
            || candidate.platform != expectedPlatform || candidate.appCompatibility != compatibility)
            error = "현재 플랫폼 또는 앱과 호환되지 않는 리소스입니다.";
        else if (string.IsNullOrWhiteSpace(candidate.contentVersion) || string.IsNullOrWhiteSpace(candidate.requiredLabel))
            error = "릴리스 버전 또는 필수 리소스 라벨이 없습니다.";
        else if (!string.IsNullOrEmpty(candidate.contentProfile) && candidate.contentProfile != SkillPilotContent.Profile)
            error = "지원하지 않는 콘텐츠 프로필입니다.";
        else if ((candidate.appCompatibility == SkillPilotContent.Profile) != (candidate.contentProfile == SkillPilotContent.Profile))
            error = "스킬 시범 앱 호환 계열과 필수 콘텐츠 프로필이 일치하지 않습니다.";
        else if (!IsAllowedUrl(candidate.catalogUrl, allowHttp))
            error = "카탈로그 주소는 허용된 HTTPS 주소여야 합니다.";
        else if (candidate.catalogSha256 == null || !Regex.IsMatch(candidate.catalogSha256, "\\A[0-9a-fA-F]{64}\\z"))
            error = "카탈로그 SHA-256 값이 올바르지 않습니다.";
        if (error != null) return false;
        candidate.catalogSha256 = candidate.catalogSha256.ToLowerInvariant();
        candidate.requiredLabel = candidate.requiredLabel.Trim();
        candidate.AllowHttp = allowHttp;
        release = candidate;
        return true;
    }

    public static bool IsAllowedUrl(string value, bool allowHttp)
        => Uri.TryCreate(value, UriKind.Absolute, out Uri uri) && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Fragment) && !string.IsNullOrEmpty(uri.Host)
            && (uri.Scheme == Uri.UriSchemeHttps || (allowHttp && uri.Scheme == Uri.UriSchemeHttp));
}
