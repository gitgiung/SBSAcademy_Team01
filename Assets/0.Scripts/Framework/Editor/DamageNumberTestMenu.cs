#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DamageNumberTestMenu
{
    [MenuItem("Tools/테스트/데미지 숫자 테스트 추가")]
    private static void Add()
    {
        foreach (var existing in Object.FindObjectsByType<DamageNumberTest>(FindObjectsInactive.Include))
        {
            if (existing.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) continue;
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing);
            return;
        }
        var root = new GameObject("DamageNumberTest");
        Undo.RegisterCreatedObjectUndo(root, "데미지 숫자 테스트 추가");
        Undo.AddComponent<DamageNumberTest>(root);
        Selection.activeGameObject = root;
    }
}
#endif
