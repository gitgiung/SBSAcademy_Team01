using UnityEngine;

[CreateAssetMenu(menuName = "Game/Remote Content Settings", fileName = "RemoteContentSettings")]
public sealed class RemoteContentSettings : ScriptableObject
{
    [SerializeField, Tooltip("명시적으로 켠 경우에만 원격 준비를 수행합니다.")]
    private bool useRemoteContent;
    [SerializeField, Tooltip("현재 앱 호환 계열용 릴리스 JSON의 HTTPS 주소입니다.")]
    private string releaseUrl = "";
    [SerializeField, Tooltip("이 앱이 읽을 수 있는 콘텐츠 형식의 호환 계열입니다.")]
    private string appCompatibility = "1";
    [SerializeField, Tooltip("Editor/Development Build에서만 테스트용 HTTP를 허용합니다.")]
    private bool allowDevelopmentHttp;

    public bool UseRemoteContent => useRemoteContent;
    public string ReleaseUrl => releaseUrl?.Trim();
    public string AppCompatibility => appCompatibility?.Trim();
    public bool AllowDevelopmentHttp => allowDevelopmentHttp && (Application.isEditor || Debug.isDebugBuild);
    public string ContentPlatform => Application.platform == RuntimePlatform.Android ? "Android"
        : Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer ? "Windows" : "Unsupported";
    public static RemoteContentSettings Load() => Resources.Load<RemoteContentSettings>("RemoteContentSettings");
}
