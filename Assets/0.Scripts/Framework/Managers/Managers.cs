using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum GamePreparationState
{
    NotStarted,
    Preparing,
    TestReady,
    Ready,
    Failed
}

public class Managers : MonoBehaviour
{
    /// <summary>공통 서비스 준비 상태입니다. 게임 데이터 준비와는 별개입니다.</summary>
	public static bool Initialized { get; private set; } = false;
    private static bool s_quitting;

	private static Managers s_instance;
    private static Managers Instance { get { Init(); return s_instance; } }

    private GamePreparationState _preparationState;
    private string _preparationError;
    private string _preloadLabel;
    private bool _requireSkillData;
    private int _preparedResourceGeneration;

    // 상태 조회만으로 Managers를 새로 만들지는 않습니다.
    public static GamePreparationState PreparationState
    {
        get
        {
            if (s_instance == null)
                return GamePreparationState.NotStarted;
            if (s_instance._preparationState == GamePreparationState.Ready
                && (!s_instance._data.IsInitialized
                    || s_instance._preparedResourceGeneration != s_instance._resource.Generation))
                return GamePreparationState.NotStarted;
            return s_instance._preparationState;
        }
    }
    public static string PreparationError => s_instance != null ? s_instance._preparationError : null;
    public static bool GameDataReady => PreparationState == GamePreparationState.Ready;

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
    private bool clearing;

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
        if (Initialized)
            return;

        if (_pool == null)
            _pool = new PoolManager(transform);

        _sound.Init(transform);
        Initialized = true;
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

    /// <summary>
    /// 빈 설정은 테스트 씬을 허용합니다. 요청한 리소스 또는 데이터의 실패는 허용하지 않습니다.
    /// 호출자는 완료 후 PreparationState를 확인해야 합니다.
    /// </summary>
    public static IEnumerator PrepareGame(string preloadLabel = "", bool requireSkillData = false)
    {
        Init();
        Managers owner = s_instance;
        if (owner == null)
            yield break;

        string label = preloadLabel?.Trim() ?? "";
        if (owner._preparationState == GamePreparationState.Preparing)
        {
            if (owner._preloadLabel != label || owner._requireSkillData != requireSkillData)
                throw new InvalidOperationException("다른 설정으로 게임 준비가 이미 진행 중입니다.");
        }
        else
        {
            owner._preloadLabel = label;
            owner._requireSkillData = requireSkillData;
            owner._preparationState = GamePreparationState.Preparing;
            owner._preparationError = null;
            // 호출한 로딩 씬이 파괴되어도 공유 준비 작업이 고립되지 않도록
            // 영속 Managers가 코루틴과 리소스 수명을 소유합니다.
            owner.StartCoroutine(owner.PrepareGameRoutine());
        }

        while (owner != null && owner._preparationState == GamePreparationState.Preparing)
            yield return null;
    }

    private IEnumerator PrepareGameRoutine()
    {
        int generation = _resource.Generation;
        try
        {
            if (!string.IsNullOrEmpty(_preloadLabel))
            {
                bool loaded = false;
                string error = null;
                yield return _resource.PreloadAsync(_preloadLabel, (success, reason) =>
                {
                    loaded = success;
                    error = reason;
                });

                if (this == null)
                    yield break;
                if (!loaded)
                {
                    FailPreparation(error ?? "리소스 준비에 실패했습니다.");
                    yield break;
                }
            }

            if (generation != _resource.Generation)
            {
                FailPreparation("게임 준비 중 리소스 캐시가 정리되었습니다.");
                yield break;
            }

            bool hasSkillData = _resource.Load<TextAsset>("SkillData") != null;
            if (hasSkillData || _requireSkillData)
            {
                Exception failure = null;
                try { _data.Init(); }
                catch (Exception exception) { failure = exception; }
                if (failure != null)
                {
                    FailPreparation(failure.Message);
                    yield break;
                }
            }

            _preparedResourceGeneration = generation;
            _preparationState = hasSkillData
                ? GamePreparationState.Ready : GamePreparationState.TestReady;
        }
        finally
        {
            if (this != null && _preparationState == GamePreparationState.Preparing)
                FailPreparation("게임 준비가 완료되기 전에 취소되었습니다.");
        }
    }

    private void FailPreparation(string error)
    {
        _preparationError = error;
        _preparationState = GamePreparationState.Failed;
    }

    /// <summary>씬 이동 직전 호출합니다. 에셋 캐시는 다음 씬에서도 사용하므로 유지합니다.</summary>
    public static void Clear()
    {
        // 정리만 하려다가 새 @Managers를 생성하지 않습니다.
        if (s_instance == null || s_instance.clearing)
            return;

        Managers owner = s_instance;
        owner.clearing = true;
        try { owner._scene.Clear(); }
        finally
        {
            // 씬 훅 이후에도 풀 콜백이 UI를 참조할 수 있으므로 풀을 먼저 정리합니다.
            try { owner._pool?.Clear(); }
            finally
            {
                try { owner._ui.Clear(); }
                finally
                {
                    owner._sound.Clear();
                    owner.clearing = false;
                }
            }
        }
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
        _resource.Clear();
        Initialized = false;
        s_instance = null;
    }
}
