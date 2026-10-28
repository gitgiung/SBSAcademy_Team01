using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Managers : MonoBehaviour
{
	public static bool Initialized { get; set; } = false;
    private static bool s_quitting;

	private static Managers s_instance;
    private static Managers Instance { get { Init(); return s_instance; } }

    #region Contents

    #endregion

    #region Core
    private DataManager _data = new DataManager();
    // 풀 생성자는 Unity API를 사용하므로 필드 초기화에서 호출하지 않습니다.
    private PoolManager _pool;
    private ResourceManager _resource = new ResourceManager();
    private SceneManagerEx _scene = new SceneManagerEx();
    private SoundManager _sound = new SoundManager();
    private UIManager _ui = new UIManager();

    public static DataManager Data { get { return Instance?._data; } }
    public static PoolManager Pool { get { return Instance?._pool; } }
    public static ResourceManager Resource { get { return Instance?._resource; } }
    public static SceneManagerEx Scene { get { return Instance?._scene; } }
    public static SoundManager Sound { get { return Instance?._sound; } }
    public static UIManager UI { get { return Instance?._ui; } }
    #endregion

    #region Language
    private static Define.ELanguage _language = Define.ELanguage.Korean;
    public static Define.ELanguage Language
    {
        get { return _language; }
        set
        {
            _language = value;
        }
    }

    public static string GetText(string textId)
    {
        switch (_language)
        {
            case Define.ELanguage.Korean:
                //return Managers.Data.TextDic[textId].KOR;
            case Define.ELanguage.English:
                break;
            case Define.ELanguage.French:
                break;
            case Define.ELanguage.SimplifiedChinese:
                break;
            case Define.ELanguage.TraditionalChinese:
                break;
            case Define.ELanguage.Japanese:
                break;
        }

        return "";
    }
    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_instance = null;
        Initialized = false;
        s_quitting = false;
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeCore();
    }

    private void InitializeCore()
    {
        if (_pool == null)
            _pool = new PoolManager(transform);

        Initialized = true;
        // Data.Init은 Addressables 로딩 완료 후 기존 로딩 씬에서 호출합니다.
        // Sound.Init 등 다른 매니저의 초기화 순서는 기존 프로젝트 정책을 유지합니다.
    }

    public static void Init()
    {
        if (s_quitting)
            return;

        if (s_instance != null)
        {
            s_instance.InitializeCore();
            return;
        }

        GameObject go = GameObject.Find("@Managers");
        if (go == null)
            go = new GameObject("@Managers");

        Managers instance = go.GetComponent<Managers>();
        if (instance == null)
            instance = go.AddComponent<Managers>();

        // 비활성 컴포넌트 등으로 Awake가 아직 호출되지 않은 경우도 지원합니다.
        s_instance = instance;
        DontDestroyOnLoad(go);
        instance.InitializeCore();
    }

    /// <summary>씬 이동 직전 호출합니다. 에셋 캐시는 다음 씬에서도 사용하므로 유지합니다.</summary>
    public static void Clear()
    {
        // 정리만 하려다가 새 @Managers를 생성하지 않습니다.
        if (s_instance == null)
            return;

        // 풀 콜백이 아직 기존 씬의 UI를 참조할 수 있으므로 풀부터 정리합니다.
        s_instance._pool?.Clear();
        s_instance._ui.Clear();
    }

    private void OnApplicationQuit()
    {
        if (s_instance == this)
            s_quitting = true;
    }

    private void OnDestroy()
    {
        if (s_instance != this)
            return;

        _pool?.Dispose();
        _pool = null;
        Initialized = false;
        s_instance = null;
    }
}
