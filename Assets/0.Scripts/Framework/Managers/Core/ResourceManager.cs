using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

public class ResourceManager
{
    private Dictionary<string, UnityEngine.Object> _resources = new Dictionary<string, UnityEngine.Object>();
    private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

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
    private void LoadAsync<T>(string key, Action<T> callback = null) where T : UnityEngine.Object
    {
        // Cache
        if (_resources.TryGetValue(key, out Object resource))
        {
            callback?.Invoke(resource as T);
            return;
        }

        var asyncOperation = Addressables.LoadAssetAsync<T>(key);
        asyncOperation.Completed += (op) =>
        {
            _resources.Add(key, op.Result);
            _handles.Add(key, asyncOperation);
            callback?.Invoke(op.Result);
        };
    }

    public void LoadAllAsync<T>(string label, Action<string, int, int> callback) where T : UnityEngine.Object
    {
        var opHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
        opHandle.Completed += (op) =>
        {
            var locations = op.Result;
            var spriteKeys = new HashSet<string>();
            foreach (var loc in locations)
            {
                if (loc.ResourceType == typeof(Sprite))
                    spriteKeys.Add(loc.PrimaryKey);
            }

            int loadCount = 0;
            int totalCount = 0;
            foreach (var loc in locations)
            {
                if (loc.ResourceType == typeof(Texture2D) && spriteKeys.Contains(loc.PrimaryKey))
                    continue;
                totalCount++;
            }

            foreach (var result in locations)
            {
                if (result.ResourceType == typeof(Texture2D) && spriteKeys.Contains(result.PrimaryKey))
                    continue;
                LoadAsync<T>(result.PrimaryKey, (obj) =>
                {
                    loadCount++;
                    callback?.Invoke(result.PrimaryKey, loadCount, totalCount);
                });
            }
        };
    }

    public void Clear()
    {
        _resources.Clear();

        foreach (var handle in _handles)
            Addressables.Release(handle.Value);

        _handles.Clear();
    }
    #endregion
}
