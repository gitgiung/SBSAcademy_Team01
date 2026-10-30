using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager
{
    private int _order = 10;

    private Dictionary<string, UI_Popup> _popups = new Dictionary<string, UI_Popup>();
    private Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();

    private UI_Scene _sceneUI = null;
    public UI_Scene SceneUI
    {
        set { _sceneUI = value; }
        get { return _sceneUI; }
    }

    public GameObject Root
    {
        get
        {
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject { name = "@UI_Root" };
            return root;
        }
    }

    public void CacheAllPopups()
    {
        var list = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(UI_Popup)) && !type.IsAbstract && !type.ContainsGenericParameters);

        foreach (Type type in list)
        {
            // Scene-authored popups need no Addressables entry. Only preload available prefabs.
            if (Managers.Resource.Load<GameObject>(type.Name) == null)
                continue;
            CachePopupUI(type);
        }
    }

    public void SetCanvas(GameObject go, bool sort = true, int sortOrder = 0)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
        }

        CanvasScaler cs = go.GetOrAddComponent<CanvasScaler>();
        if (cs != null)
        {
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
        }

        go.GetOrAddComponent<GraphicRaycaster>();

        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = sortOrder;
        }
    }

    public T GetSceneUI<T>() where T : UI_Base
    {
        return _sceneUI as T;
    }

    public T MakeWorldSpaceUI<T>(Transform parent = null, string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"{name}");
        if (go == null)
            return null;
        if (parent != null)
            go.transform.SetParent(parent);

        Canvas canvas = go.GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        return Util.GetOrAddComponent<T>(go);
    }

    public T MakeSubItem<T>(Transform parent = null, string name = null, bool pooling = true) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate(name, parent, pooling);
        if (go == null)
            return null;
        go.transform.SetParent(parent);

        return Util.GetOrAddComponent<T>(go);
    }

    public T ShowBaseUI<T>(string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate(name);
        if (go == null)
            return null;
        T baseUI = Util.GetOrAddComponent<T>(go);

        go.transform.SetParent(Root.transform);

        return baseUI;
    }

    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate(name);
        if (go == null)
            return null;
        T sceneUI = Util.GetOrAddComponent<T>(go);
        _sceneUI = sceneUI;

        go.transform.SetParent(Root.transform);

        return sceneUI;
    }

    /// <summary>씬에서 준비한 인스턴스를 기존 캐시에 등록합니다. Clear에서 함께 정리합니다.</summary>
    public bool RegisterPopupUI(string name, UI_Popup popup)
    {
        if (string.IsNullOrEmpty(name) || popup == null)
        {
            Debug.LogError("RegisterPopupUI requires a name and a live popup.");
            return false;
        }
        if (_popups.TryGetValue(name, out UI_Popup existing) && existing != null)
        {
            if (existing == popup) return true;
            Debug.LogError($"Popup name already registered: {name}.");
            return false;
        }
        if (_popups.Values.Any(item => item == popup))
        {
            Debug.LogError("Popup instance already registered under another name.");
            return false;
        }
        popup.gameObject.SetActive(false);
        popup.transform.SetParent(Root.transform, false);
        _popups[name] = popup;
        return true;
    }

    public void CachePopupUI(Type type)
    {
        if (type == null || !typeof(UI_Popup).IsAssignableFrom(type)
            || type.IsAbstract || type.ContainsGenericParameters)
        {
            Debug.LogError("CachePopupUI requires a concrete UI_Popup type.");
            return;
        }

        RefreshPopupStack();
        string name = type.Name;

        if (_popups.TryGetValue(name, out UI_Popup popup) && popup != null)
        {
            if (!type.IsInstanceOfType(popup))
                Debug.LogError($"Popup type mismatch: {name}.");
            return;
        }

        _popups.Remove(name);
        GameObject go = Managers.Resource.Instantiate(name);
        if (go == null)
            return;

        popup = go.GetComponent(type) as UI_Popup;
        if (popup == null)
        {
            Debug.LogError($"Popup prefab {name} does not contain {type.Name}.");
            UnityEngine.Object.Destroy(go);
            return;
        }

        go.transform.SetParent(Root.transform, false);
        go.SetActive(false);
        _popups[name] = popup;
    }

    public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        RefreshPopupStack();
        if (!_popups.TryGetValue(name, out UI_Popup popup) || popup == null)
        {
            _popups.Remove(name);
            GameObject go = Managers.Resource.Instantiate(name);
            if (go == null)
                return null;
            popup = Util.GetOrAddComponent<T>(go);
            _popups[name] = popup;
        }

        if (!(popup is T result))
        {
            Debug.LogError($"Popup type mismatch: {name}.");
            return null;
        }

        // Init may be deferred for an inactive prefab. Complete it before assigning order.
        popup.Init();
        RemovePopupFromStack(popup);
        _popupStack.Push(popup);

        popup.transform.SetParent(Root.transform, false);
        UpdatePopupOrder();
        popup.gameObject.SetActive(true);

        return result;
    }

    public void ClosePopupUI(UI_Popup popup)
    {
        RefreshPopupStack();
        if (_popupStack.Count == 0)
            return;

        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!");
            return;
        }

        ClosePopupUI();
    }

    public void ClosePopupUI()
    {
        RefreshPopupStack();
        if (_popupStack.Count == 0)
            return;

        UI_Popup popup = _popupStack.Pop();

        if (popup != null)
            popup.gameObject.SetActive(false);
        UpdatePopupOrder();
    }

    public void CloseAllPopupUI()
    {
        RefreshPopupStack();
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }

    public int GetPopupCount()
    {
        RefreshPopupStack();
        return _popupStack.Count;
    }

    private void RemovePopupFromStack(UI_Popup popup)
    {
        UI_Popup[] remaining = _popupStack.Reverse().Where(item => item != popup).ToArray();
        _popupStack.Clear();
        foreach (UI_Popup item in remaining)
            _popupStack.Push(item);
    }

    private void RefreshPopupStack()
    {
        // Unity destroyed objects compare equal to null. Hidden objects are cached, not open.
        UI_Popup[] open = _popupStack.Reverse()
            .Where(popup => popup != null && popup.gameObject.activeSelf).Distinct().ToArray();
        _popupStack.Clear();
        foreach (UI_Popup popup in open)
            _popupStack.Push(popup);
        UpdatePopupOrder();
    }

    private void UpdatePopupOrder()
    {
        int order = 10;
        foreach (UI_Popup popup in _popupStack.Reverse())
            SetCanvas(popup.gameObject, false, order++);
        _order = order;
    }

    public void Clear()
    {
        CloseAllPopupUI();
        UI_Popup[] cached = _popups.Values.Where(popup => popup != null).Distinct().ToArray();
        _popups.Clear();
        foreach (UI_Popup popup in cached)
            UnityEngine.Object.Destroy(popup.gameObject);
        _sceneUI = null;
        _order = 10;
    }
}
