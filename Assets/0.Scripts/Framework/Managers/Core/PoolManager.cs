using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 런타임 메인 스레드에서 사용합니다. 씬의 소유자가 생성하고 OnDestroy에서 Dispose합니다.
/// 각 프리팹의 대여/반납만 관리하며 이동, AI, 보상 규칙은 각 기능에 둡니다.
/// </summary>
public sealed class PoolManager : IDisposable
{
    private const int DefaultMaxSize = 256;
    private readonly Dictionary<GameObject, PrefabPool> _pools =
        new Dictionary<GameObject, PrefabPool>();
    private readonly Transform _root;
    private readonly Transform _staging;
    private bool _clearing;
    private bool _disposed;
    private int _callbackDepth;
    private int _activationDepth;

    public PoolManager(Transform parent = null)
    {
        _root = new GameObject("@Pools").transform;
        _root.SetParent(parent, false);
        GameObject staging = new GameObject("@InactiveCreation");
        staging.SetActive(false);
        _staging = staging.transform;
        _staging.SetParent(_root, false);
    }

    /// <summary>기존 API. 기본 위치/회전으로 꺼냅니다.</summary>
    public GameObject Pop(GameObject prefab)
    {
        return Pop(prefab, Vector3.zero, Quaternion.identity);
    }

    /// <summary>
    /// 초기화 → 부모/좌표 설정 → initialize → 활성화 순서입니다.
    /// initialize 안에서 SetActive, 반납, Clear 등을 호출하지 마세요.
    /// parent가 없으면 해당 프리팹의 풀 부모를 사용합니다. 좌표는 월드 기준입니다.
    /// </summary>
    public GameObject Pop(GameObject prefab, Vector3 position, Quaternion rotation,
        Transform parent = null, Action<GameObject> initialize = null)
    {
        EnsureAvailable();
        PrefabPool pool = GetOrCreatePool(prefab);
        PooledObject item = pool.Rent();
        uint lease = item.LeaseVersion;
        try
        {
            _callbackDepth++;
            try
            {
                item.ResetForRent();
                item.transform.SetParent(parent != null ? parent : pool.Root, false);
                item.transform.localScale = pool.OriginalScale;
                item.transform.SetPositionAndRotation(position, rotation);
                initialize?.Invoke(item.gameObject);
            }
            finally { _callbackDepth--; }
            _activationDepth++;
            try { item.gameObject.SetActive(true); }
            finally { _activationDepth--; }
            return item.gameObject;
        }
        catch
        {
            if (item != null) item.ReturnToPool(lease);
            throw;
        }
    }

    /// <summary>알 수 없는 객체, 다른 매니저의 객체, 중복 반납은 false입니다.</summary>
    public bool Push(GameObject go)
    {
        if (_disposed || _clearing || go == null) return false;
        PooledObject item = go.GetComponent<PooledObject>();
        return item != null && item.BelongsTo(this) && item.ReturnToPool();
    }

    /// <summary>비활성 대기 수량을 count까지 채웁니다. 동시 활성 개수 제한이 아닙니다.</summary>
    public void Prewarm(GameObject prefab, int count, int maxSize = DefaultMaxSize)
    {
        EnsureAvailable();
        if (maxSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxSize));
        if (count < 0 || count > maxSize) throw new ArgumentOutOfRangeException(nameof(count));
        PrefabPool pool = GetOrCreatePool(prefab, maxSize);
        if (pool.MaxSize != maxSize)
            throw new InvalidOperationException("풀의 maxSize는 최초 등록 후 변경할 수 없습니다.");
        pool.Prewarm(count);
    }

    public int GetActiveCount(GameObject prefab)
    {
        return prefab != null && _pools.TryGetValue(prefab, out PrefabPool pool)
            ? pool.ActiveCount : 0;
    }

    public int GetInactiveCount(GameObject prefab)
    {
        return prefab != null && _pools.TryGetValue(prefab, out PrefabPool pool)
            ? pool.InactiveCount : 0;
    }

    /// <summary>사용 중/대기 중 객체 모두 정리합니다. 이후 새 풀을 만들 수 있습니다.</summary>
    public void Clear()
    {
        if (_disposed || _clearing) return;
        if (_callbackDepth > 0 || _activationDepth > 0)
            throw new InvalidOperationException("초기화/반납 콜백 안에서 Clear를 호출할 수 없습니다.");
        _clearing = true;
        try
        {
            foreach (PrefabPool pool in _pools.Values) pool.Clear();
            _pools.Clear();
        }
        finally { _clearing = false; }
    }

    /// <summary>소유자 파괴 시 호출합니다. 이후에는 매니저를 다시 사용할 수 없습니다.</summary>
    public void Dispose()
    {
        if (_disposed || _clearing) return;
        if (_callbackDepth > 0 || _activationDepth > 0)
            throw new InvalidOperationException("초기화/반납 콜백 안에서 Dispose를 호출할 수 없습니다.");
        Clear();
        _disposed = true;
        if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
    }

    private PrefabPool GetOrCreatePool(GameObject prefab, int maxSize = DefaultMaxSize)
    {
        if (prefab == null) throw new ArgumentNullException(nameof(prefab));
        if (!_pools.TryGetValue(prefab, out PrefabPool pool))
        {
            pool = new PrefabPool(this, prefab, _root, _staging, maxSize);
            _pools.Add(prefab, pool);
        }
        return pool;
    }

    private void EnsureAvailable()
    {
        if (_disposed || _root == null)
            throw new ObjectDisposedException(nameof(PoolManager));
        if (_clearing)
            throw new InvalidOperationException("전체 정리 중에는 풀을 사용할 수 없습니다.");
        if (_callbackDepth > 0)
            throw new InvalidOperationException("초기화/반납 콜백 안에서 새 대여를 시작할 수 없습니다.");
    }

    private sealed class PrefabPool
    {
        private readonly PoolManager _owner;
        private readonly GameObject _prefab;
        private readonly Transform _staging;
        private readonly ObjectPool<PooledObject> _pool;
        private readonly HashSet<PooledObject> _active = new HashSet<PooledObject>();
        private bool _closed;

        public Transform Root { get; }
        public Vector3 OriginalScale { get; }
        public int MaxSize { get; }
        public int ActiveCount => _active.Count;
        public int InactiveCount => _pool.CountInactive;

        public PrefabPool(PoolManager owner, GameObject prefab, Transform parent,
            Transform staging, int maxSize)
        {
            _owner = owner;
            _prefab = prefab;
            _staging = staging;
            MaxSize = maxSize;
            OriginalScale = prefab.transform.localScale;
            Root = new GameObject("@" + prefab.name + "Pool").transform;
            Root.SetParent(parent, false);
            _pool = new ObjectPool<PooledObject>(Create, null, null, DestroyItem,
                collectionCheck: true, defaultCapacity: Math.Min(16, maxSize), maxSize: maxSize);
        }

        private PooledObject Create()
        {
            // 비활성 부모 아래에서 생성하여 활성 프리팹도 첫 OnEnable이 먼저 실행되지 않습니다.
            GameObject go = UnityEngine.Object.Instantiate(_prefab, _staging, false);
            go.SetActive(false);
            go.name = _prefab.name;
            go.transform.SetParent(Root, false);
            PooledObject item = go.GetComponent<PooledObject>();
            if (item == null) item = go.AddComponent<PooledObject>();
            item.Bind(_owner, Release);
            return item;
        }

        public PooledObject Rent()
        {
            if (_closed) throw new ObjectDisposedException(nameof(PrefabPool));
            PooledObject item = _pool.Get();
            // 외부 Destroy는 권장하지 않지만, 파괴된 대기 객체를 다시 반환하지는 않습니다.
            while (item == null) item = _pool.Get();
            _active.Add(item);
            item.BeginLease();
            return item;
        }

        private bool Release(PooledObject item)
        {
            if (_closed || _owner._callbackDepth > 0 ||
                !item.IsRented || !_active.Remove(item)) return false;
            // OnDisable/정리 콜백에서 다시 반납해도 재진입하지 않습니다.
            item.EndLease();
            _owner._callbackDepth++;
            try
            {
                item.CleanupForReturn();
                item.gameObject.SetActive(false);
                item.transform.SetParent(Root, false);
                _pool.Release(item);
            }
            finally { _owner._callbackDepth--; }
            return true;
        }

        public void Prewarm(int count)
        {
            if (count <= _pool.CountInactive) return;
            // 하나씩 꺼내 바로 반납하면 같은 한 개만 재사용하므로 모두 확보 후 반납합니다.
            PooledObject[] items = new PooledObject[count];
            for (int i = 0; i < count; i++) items[i] = _pool.Get();
            foreach (PooledObject item in items) _pool.Release(item);
        }

        public void Clear()
        {
            _closed = true;
            PooledObject[] active = new PooledObject[_active.Count];
            _active.CopyTo(active);
            _active.Clear();
            foreach (PooledObject item in active)
            {
                if (item == null) continue;
                item.EndLease();
                item.CleanupForReturn();
                DestroyItem(item);
            }
            _pool.Clear();
            if (Root != null) UnityEngine.Object.Destroy(Root.gameObject);
        }

        private void DestroyItem(PooledObject item)
        {
            if (item == null) return;
            item.Unbind();
            item.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(item.gameObject);
        }
    }
}
