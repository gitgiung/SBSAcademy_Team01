using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

public class ResourceManager
{
    private Dictionary<string, UnityEngine.Object> _resources = new Dictionary<string, UnityEngine.Object>();
    private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
    private readonly Dictionary<string, IResourceLocation> _assetLocations = new Dictionary<string, IResourceLocation>();
    private readonly Dictionary<string, PendingAsset> _pending = new Dictionary<string, PendingAsset>();
    private readonly HashSet<LoadBatch> _batches = new HashSet<LoadBatch>();
    private int _generation;
    internal int Generation => _generation;

    #region Load Resource
    public T Load<T>(string key) where T : Object
    {
        if (_resources.TryGetValue(key, out Object resource))
            return resource as T;

        return null;
    }

    /// <summary>기존 호출을 유지합니다. parent는 풀 사용 시에도 적용됩니다.</summary>
    public GameObject Instantiate(string key, Transform parent = null, bool pooling = false)
    {
        GameObject prefab = FindPrefab(key);
        if (prefab == null)
            return null;

        if (pooling)
            return Managers.Pool.Pop(prefab, Vector3.zero, Quaternion.identity, parent);

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = prefab.name;
        return go;
    }

    /// <summary>
    /// 풀 전용 생성 경로입니다. 월드 좌표를 설정하고 initialize를 실행한 뒤 활성화합니다.
    /// initialize에서는 SetActive/반납/Clear를 호출하지 않습니다.
    /// UI 로컬 좌표는 parent를 넘기고 initialize에서 anchoredPosition을 설정합니다.
    /// </summary>
    public GameObject Instantiate(string key, Vector3 position, Quaternion rotation,
        Transform parent = null, Action<GameObject> initialize = null)
    {
        GameObject prefab = FindPrefab(key);
        if (prefab == null)
            return null;

        return Managers.Pool.Pop(prefab, position, rotation, parent, initialize);
    }

    /// <summary>첫 생성 전에 호출하면 프리팹별 대기 수량과 보관 한도를 설정합니다.</summary>
    public void Prewarm(string key, int count, int maxSize = 256)
    {
        GameObject prefab = FindPrefab(key);
        if (prefab != null)
            Managers.Pool.Prewarm(prefab, count, maxSize);
    }

    private GameObject FindPrefab(string key)
    {
        GameObject prefab = Load<GameObject>(key);
        if (prefab == null)
            Debug.LogError($"Failed to load prefab : {key}. Addressables 로딩 완료 여부를 확인하세요.");
        return prefab;
    }

    public void Destroy(GameObject go)
    {
        if (go == null)
            return;

        PooledObject pooled = go.GetComponent<PooledObject>();
        if (pooled != null && pooled.IsPoolOwned)
        {
            // 실제 소유자에게 반납합니다. 중복/콜백 중 반납 실패를 Destroy로 바꾸지 않습니다.
            // 독립 풀의 객체도 다른 매니저에 섞지 않고 원래 풀로 돌아갑니다.
            if (!pooled.ReturnToPool() && pooled.IsRented)
                Debug.LogWarning("풀 콜백 안에서는 반납할 수 없습니다. 콜백 종료 후 반납하세요.", go);
            return;
        }

        Object.Destroy(go);
    }
    #endregion

    #region Addressable
    /// <summary>콜백 API와 같은 로딩 작업을 기다립니다. 중단된 대기자가 핸들을 소유하지 않습니다.</summary>
    public IEnumerator PreloadAsync(string label, Action<bool, string> completed)
    {
        LoadBatch batch = BeginBatch(label, typeof(Object), null, null);
        while (!batch.Finished)
            yield return null;
        InvokeSafely(() => completed?.Invoke(batch.Error == null, batch.Error));
    }

    /// <summary>기존 진행 콜백 API입니다. 실패는 Console에 기록합니다.</summary>
    public void LoadAllAsync<T>(string label, Action<string, int, int> callback) where T : Object
    {
        LoadAllAsync<T>(label, callback, (success, error) =>
        {
            if (!success) Debug.LogError($"Resource load failed: {error}");
        });
    }

    /// <summary>진행 수는 처리된 개수입니다. 전체 성공 여부는 completed로 판단합니다.</summary>
    public void LoadAllAsync<T>(string label, Action<string, int, int> callback,
        Action<bool, string> completed) where T : Object
    {
        BeginBatch(label, typeof(T), callback, completed);
    }

    private LoadBatch BeginBatch(string label, Type type, Action<string, int, int> progress,
        Action<bool, string> completed)
    {
        var batch = new LoadBatch { Generation = _generation, Progress = progress, Completed = completed };
        _batches.Add(batch);
        if (string.IsNullOrWhiteSpace(label))
        {
            FinishEmptyBatch(batch, "리소스 라벨이 비어 있습니다.");
            return batch;
        }

        try
        {
            var locations = Addressables.LoadResourceLocationsAsync(label.Trim(), type);
            batch.Locations = locations;
            batch.OwnsLocations = true;
            locations.Completed += operation => OnLocationsLoaded(batch, label, operation);
        }
        catch (Exception exception) { FinishEmptyBatch(batch, exception.Message); }
        return batch;
    }

    private void OnLocationsLoaded(LoadBatch batch, string label,
        AsyncOperationHandle<IList<IResourceLocation>> operation)
    {
        if (batch.Finished) return;
        try
        {
            if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null || operation.Result.Count == 0)
            {
                FinishEmptyBatch(batch, $"리소스 라벨 '{label}'의 로딩 대상이 없거나 조회에 실패했습니다.");
                return;
            }

            var selected = new Dictionary<string, IResourceLocation>();
            foreach (IResourceLocation location in operation.Result)
            {
                if (string.IsNullOrEmpty(location.PrimaryKey))
                {
                    FinishEmptyBatch(batch, "주소가 없는 리소스를 로드할 수 없습니다.");
                    return;
                }
                if (selected.TryGetValue(location.PrimaryKey, out IResourceLocation previous))
                {
                    // 같은 주소의 Sprite와 원본 Texture는 기존처럼 Sprite를 우선합니다.
                    bool spriteAndTexture = (previous.ResourceType == typeof(Texture2D) && location.ResourceType == typeof(Sprite))
                        || (previous.ResourceType == typeof(Sprite) && location.ResourceType == typeof(Texture2D));
                    if (spriteAndTexture && SameSource(previous, location))
                    {
                        if (location.ResourceType == typeof(Sprite)) selected[location.PrimaryKey] = location;
                    }
                    else if (!SameLocation(previous, location))
                    {
                        FinishEmptyBatch(batch, $"서로 다른 리소스가 같은 주소를 사용합니다: {location.PrimaryKey}");
                        return;
                    }
                }
                else selected.Add(location.PrimaryKey, location);
            }

            batch.Total = selected.Count;
            foreach (IResourceLocation location in selected.Values)
            {
                if (batch.Finished) break;
                RequestAsset(location, (asset, error) => OnAssetSettled(batch, location.PrimaryKey, error));
            }
        }
        catch (Exception exception) { FinishBatch(batch, exception.Message); }
        finally { ReleaseLocations(batch); }
    }

    private void RequestAsset(IResourceLocation location, Action<Object, string> completed)
    {
        string key = location.PrimaryKey;
        if (_resources.TryGetValue(key, out Object cached) && cached != null)
        {
            bool matches = _assetLocations.TryGetValue(key, out IResourceLocation previous)
                && SameLocation(previous, location) && location.ResourceType.IsInstanceOfType(cached);
            completed(matches ? cached : null, matches ? null : $"캐시 주소 또는 타입 충돌: {key}");
            return;
        }
        if (_pending.TryGetValue(key, out PendingAsset existing))
        {
            if (SameLocation(existing.Location, location)) existing.Waiters.Add(completed);
            else completed(null, $"로딩 중인 리소스의 주소 충돌: {key}");
            return;
        }

        // 외부에서 파괴된 캐시 객체의 이전 핸들도 새 요청 전에 해제합니다.
        _resources.Remove(key);
        _assetLocations.Remove(key);
        if (_handles.TryGetValue(key, out AsyncOperationHandle oldHandle))
        {
            _handles.Remove(key);
            if (oldHandle.IsValid()) Addressables.Release(oldHandle);
        }

        var pending = new PendingAsset { Location = location, Generation = _generation };
        pending.Waiters.Add(completed);
        _pending.Add(key, pending);
        try
        {
            pending.Handle = Addressables.LoadAssetAsync<Object>(location);
            pending.OwnsHandle = true;
            pending.Handle.Completed += operation => OnAssetLoaded(pending, operation);
        }
        catch (Exception exception)
        {
            _pending.Remove(key);
            ReleaseAsset(pending);
            InvokeSafely(() => completed(null, exception.Message));
        }
    }

    private void OnAssetLoaded(PendingAsset pending, AsyncOperationHandle<Object> operation)
    {
        if (!pending.OwnsHandle) return; // Clear가 이미 소유권을 회수한 이전 작업입니다.
        string key = pending.Location.PrimaryKey;
        _pending.Remove(key);
        var waiters = pending.Waiters.ToArray();
        pending.Waiters.Clear();
        Object asset = null;
        string error = null;
        if (pending.Generation == _generation && operation.Status == AsyncOperationStatus.Succeeded
            && operation.Result != null && pending.Location.ResourceType.IsInstanceOfType(operation.Result))
        {
            asset = operation.Result;
            _resources[key] = asset;
            _assetLocations[key] = pending.Location;
            _handles[key] = pending.Handle;
            pending.OwnsHandle = false; // 한 번 취득한 참조를 캐시로 이전합니다.
        }
        else
        {
            error = $"리소스 '{key}' 로딩 실패: {operation.OperationException?.Message ?? "결과 또는 타입이 유효하지 않습니다."}";
            ReleaseAsset(pending);
        }
        foreach (Action<Object, string> waiter in waiters)
            InvokeSafely(() => waiter(asset, error));
    }

    private void OnAssetSettled(LoadBatch batch, string key, string error)
    {
        if (batch.Finished || batch.Generation != _generation) return;
        batch.Count++;
        if (error != null && batch.Error == null) batch.Error = error;
        InvokeSafely(() => batch.Progress?.Invoke(key, batch.Count, batch.Total));
        if (batch.Count == batch.Total) FinishBatch(batch, batch.Error);
    }

    private void FinishEmptyBatch(LoadBatch batch, string error)
    {
        InvokeSafely(() => batch.Progress?.Invoke(null, 0, 0));
        FinishBatch(batch, error);
    }

    private void FinishBatch(LoadBatch batch, string error)
    {
        if (batch.Finished) return;
        batch.Finished = true;
        batch.Error = error;
        _batches.Remove(batch);
        ReleaseLocations(batch);
        InvokeSafely(() => batch.Completed?.Invoke(error == null, error));
    }

    private static bool SameLocation(IResourceLocation first, IResourceLocation second)
        => first.ResourceType == second.ResourceType && SameSource(first, second);

    private static bool SameSource(IResourceLocation first, IResourceLocation second)
    {
        if (first.InternalId != second.InternalId || first.ProviderId != second.ProviderId)
            return false;
        // 주소와 파일이 같아도 의존 번들 버전이 다르면 다른 로딩 대상입니다.
        IList<IResourceLocation> firstDependencies = first.Dependencies;
        IList<IResourceLocation> secondDependencies = second.Dependencies;
        int count = firstDependencies?.Count ?? 0;
        if (count != (secondDependencies?.Count ?? 0)) return false;
        for (int index = 0; index < count; index++)
            if (!SameLocation(firstDependencies[index], secondDependencies[index])) return false;
        return true;
    }

    private static void ReleaseLocations(LoadBatch batch)
    {
        if (!batch.OwnsLocations) return;
        batch.OwnsLocations = false;
        if (batch.Locations.IsValid()) Addressables.Release(batch.Locations);
    }

    private static void ReleaseAsset(PendingAsset pending)
    {
        if (!pending.OwnsHandle) return;
        pending.OwnsHandle = false;
        if (pending.Handle.IsValid()) Addressables.Release(pending.Handle);
    }

    private static void InvokeSafely(Action callback)
    {
        try { callback?.Invoke(); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    public void Clear()
    {
        _generation++;
        var batches = new List<LoadBatch>(_batches);
        var pending = new List<PendingAsset>(_pending.Values);
        var handles = new List<AsyncOperationHandle>(_handles.Values);
        _batches.Clear();
        _pending.Clear();
        _resources.Clear();
        _handles.Clear();
        _assetLocations.Clear();

        // 사용자 콜백 전에 이전 상태를 모두 분리합니다. 콜백이 시작한 새 요청은 유지합니다.
        foreach (LoadBatch batch in batches)
        {
            batch.Finished = true;
            batch.Error = "Resource loading canceled: 리소스 캐시가 정리되었습니다.";
            ReleaseLocations(batch);
        }
        foreach (PendingAsset request in pending)
        {
            request.Waiters.Clear();
            ReleaseAsset(request);
        }
        foreach (AsyncOperationHandle handle in handles)
            if (handle.IsValid()) Addressables.Release(handle);
        foreach (LoadBatch batch in batches)
            InvokeSafely(() => batch.Completed?.Invoke(false, batch.Error));
    }

    private sealed class LoadBatch
    {
        public int Generation;
        public int Count;
        public int Total;
        public bool Finished;
        public string Error;
        public bool OwnsLocations;
        public AsyncOperationHandle Locations;
        public Action<string, int, int> Progress;
        public Action<bool, string> Completed;
    }

    private sealed class PendingAsset
    {
        public int Generation;
        public IResourceLocation Location;
        public AsyncOperationHandle<Object> Handle;
        public bool OwnsHandle;
        public readonly List<Action<Object, string>> Waiters = new List<Action<Object, string>>();
    }
    #endregion
}
