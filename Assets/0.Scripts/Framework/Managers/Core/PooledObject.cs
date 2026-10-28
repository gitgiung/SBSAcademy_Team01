using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PooledObject : MonoBehaviour
{
    private PoolManager _manager;
    private Func<PooledObject, bool> _release;
    private IPoolable[] _callbacks;

    /// <summary>풀에 소속된 객체인지 확인합니다. 대여 중 여부와 별개입니다.</summary>
    public bool IsPoolOwned => _manager != null;
    public bool IsRented { get; private set; }
    public uint LeaseVersion { get; private set; }

    /// <summary>먼저 SetActive(false)하거나 Destroy하지 말고 이 메서드로 반납합니다.</summary>
    public bool ReturnToPool()
    {
        return IsRented && _release != null && _release(this);
    }

    /// <summary>지연 콜백에는 대여 당시 저장한 버전을 전달해 재사용 중인 객체를 보호합니다.</summary>
    public bool ReturnToPool(uint expectedLease)
    {
        return expectedLease == LeaseVersion && ReturnToPool();
    }

    internal bool BelongsTo(PoolManager manager) { return _manager == manager; }

    internal void Bind(PoolManager manager, Func<PooledObject, bool> release)
    {
        _manager = manager;
        _release = release;
        // 최초 생성 시에만 탐색합니다. 사용 중 컴포넌트를 추가/제거하지 않는 계약입니다.
        MonoBehaviour[] components = GetComponentsInChildren<MonoBehaviour>(true);
        List<IPoolable> callbacks = new List<IPoolable>();
        foreach (MonoBehaviour component in components)
            if (component is IPoolable callback) callbacks.Add(callback);
        _callbacks = callbacks.ToArray();
    }

    internal void BeginLease()
    {
        IsRented = true;
        unchecked { LeaseVersion++; }
    }

    internal void EndLease() { IsRented = false; }

    internal void ResetForRent()
    {
        foreach (IPoolable callback in _callbacks)
            callback.OnPoolRent();
    }

    internal void CleanupForReturn()
    {
        // 한 컴포넌트의 정리 실패가 다른 컴포넌트의 정리까지 막지 않도록 합니다.
        foreach (IPoolable callback in _callbacks)
        {
            try { callback.OnPoolReturn(); }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }
    }

    internal void Unbind()
    {
        IsRented = false;
        _manager = null;
        _release = null;
    }
}
