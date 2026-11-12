using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 일반 몬스터 처치 시 필드 아이템(힐팩·자석·폭탄) 드랍과 획득 효과입니다.
/// 확률과 효과 수치는 FieldItemDropSettings의 임시값이며 기획 확정값이 아닙니다.
/// </summary>
public sealed partial class SceneEnemyPool
{
    private const float FallbackBlastRadius = 12f;

    [Header("필드 아이템 드랍 (임시값, 기획 확정 전)")]
    [SerializeField] private FieldItemDropSettings fieldItems = new FieldItemDropSettings();

    private readonly List<FieldItemPickup> activeFieldItems = new List<FieldItemPickup>();
    private readonly List<KeyValuePair<PooledObject, uint>> bombTargets = new List<KeyValuePair<PooledObject, uint>>();
    private Transform fieldItemTarget;
    private bool defeatingByBomb;

    public FieldItemDropSettings FieldItems => fieldItems ?? (fieldItems = new FieldItemDropSettings());

    public int ActiveFieldItemCount
    {
        get
        {
            PruneFieldItems();
            return activeFieldItems.Count;
        }
    }

    private void HandleEnemyDefeated(EnemyHealth enemy)
    {
        // 폭탄으로 죽은 몬스터가 다시 폭탄을 떨어뜨려 연쇄되지 않도록 막습니다. 경험치는 그대로 떨어집니다.
        if (closing || defeatingByBomb || enemy == null) return;
        try { TryDropFieldItem(FieldItems.Roll(Random.value), enemy.transform.position); }
        catch (System.Exception exception) { Debug.LogException(exception, this); }
    }

    /// <summary>지정한 종류를 그 자리에 떨어뜨립니다. 동시 개수 제한에 걸리면 떨어뜨리지 않습니다.</summary>
    public bool TryDropFieldItem(FieldItemKind kind, Vector3 position)
    {
        if (closing || !isActiveAndEnabled || kind == FieldItemKind.None) return false;
        FieldItemDropSettings settings = FieldItems;
        PruneFieldItems();
        if (activeFieldItems.Count >= Mathf.Max(0, settings.maxFieldItems)) return false;

        Sprite sprite = settings.SpriteFor(kind);
        if (sprite == null) sprite = FieldItemSprites.Get(kind);
        FieldItemPickup item = FieldItemPickup.Create(kind, position, sprite, settings.itemScale,
            settings.collectDistance, ResolveFieldItemTarget(), HandleFieldItemCollected);
        if (item == null) return false;

        SceneManager.MoveGameObjectToScene(item.gameObject, gameObject.scene);
        activeFieldItems.Add(item);
        return true;
    }

    private void HandleFieldItemCollected(FieldItemPickup item, PlayerData collector)
    {
        activeFieldItems.Remove(item);
        if (closing || item == null || collector == null) return;
        switch (item.Kind)
        {
            case FieldItemKind.HealPack:
                collector.RestoreHealth(FieldItems.HealAmount(collector.maxHealth));
                break;
            case FieldItemKind.Magnet:
                AttractAllExperience();
                break;
            case FieldItemKind.Bomb:
                DefeatEnemiesInView(collector.transform.position);
                break;
        }
    }

    /// <summary>자석: 이 씬에 떨어져 있는 모든 경험치를 플레이어에게 끌어당깁니다. 끌려가기 시작한 개수를 돌려줍니다.</summary>
    public int AttractAllExperience()
    {
        float speed = FieldItems.magnetAttractionSpeed;
        int attracted = 0;
        foreach (ExperiencePickup pickup in FindObjectsByType<ExperiencePickup>())
            if (pickup != null && pickup.gameObject.scene == gameObject.scene && pickup.BeginMagnetAttraction(speed))
                attracted++;
        return attracted;
    }

    /// <summary>
    /// 폭탄: 카메라 화면 안(여유 거리 포함)의 일반 몬스터를 모두 처치합니다. 보스는 대상이 아닙니다.
    /// 처치된 몬스터는 일반 사망과 같이 경험치를 떨어뜨리고 처치 수에 집계됩니다.
    /// </summary>
    public int DefeatEnemiesInView(Vector3 blastCenter)
    {
        if (closing || defeatingByBomb) return 0;

        Camera view = recycleCamera != null ? recycleCamera : Camera.main;
        bool bounded = view != null && view.isActiveAndEnabled && view.orthographic;
        float margin = FieldItems.bombViewMargin;
        if (float.IsNaN(margin) || float.IsInfinity(margin) || margin < 0f) margin = 0f;
        Vector2 center = bounded ? (Vector2)view.transform.position : (Vector2)blastCenter;
        float halfHeight = bounded ? view.orthographicSize + margin : 0f;
        float halfWidth = bounded ? view.orthographicSize * view.aspect + margin : 0f;

        bombTargets.Clear();
        foreach (var entry in activeEnemies)
        {
            if (entry.Key == null) continue;
            // 화면을 판정할 카메라가 없으면 활성 일반 몬스터 전체를 대상으로 합니다.
            if (!bounded || FieldItemDropSettings.IsInsideView(entry.Key.transform.position, center, halfWidth, halfHeight))
                bombTargets.Add(entry);
        }

        int defeated = 0;
        defeatingByBomb = true;
        try
        {
            // 사망 처리 중 activeEnemies가 바뀌므로 모아 둔 대상의 대여가 그대로인지 다시 확인합니다.
            foreach (var entry in bombTargets)
            {
                if (entry.Key == null || !activeEnemies.TryGetValue(entry.Key, out uint lease) || lease != entry.Value
                    || !entry.Key.TryGetComponent<EnemyHealth>(out var health)) continue;
                try { if (health.Kill()) defeated++; }
                catch (System.Exception exception) { Debug.LogException(exception, this); }
            }
        }
        finally
        {
            defeatingByBomb = false;
            bombTargets.Clear();
        }

        float radius = bounded ? Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) : FallbackBlastRadius;
        FieldItemBlastFx blast = FieldItemBlastFx.Play(blastCenter, radius);
        if (blast != null) SceneManager.MoveGameObjectToScene(blast.gameObject, gameObject.scene);
        return defeated;
    }

    private Transform ResolveFieldItemTarget()
    {
        if (fieldItemTarget != null) return fieldItemTarget;
        PlayerData player = FindAnyObjectByType<PlayerData>();
        return player != null ? player.transform : null;
    }

    private void PruneFieldItems()
    {
        // 획득 연출이 끝나 파괴됐거나 이미 획득된 항목을 정리합니다.
        for (int i = activeFieldItems.Count - 1; i >= 0; i--)
            if (activeFieldItems[i] == null || activeFieldItems[i].Collected) activeFieldItems.RemoveAt(i);
    }

    private void DiscardFieldItems()
    {
        foreach (FieldItemPickup item in activeFieldItems)
            if (item != null) item.Discard();
        activeFieldItems.Clear();
    }

#if UNITY_EDITOR
    [ContextMenu("테스트/힐팩 떨어뜨리기 (Play 중)")]
    private void DropTestHealPack() { DropTestItem(FieldItemKind.HealPack); }

    [ContextMenu("테스트/자석 떨어뜨리기 (Play 중)")]
    private void DropTestMagnet() { DropTestItem(FieldItemKind.Magnet); }

    [ContextMenu("테스트/폭탄 떨어뜨리기 (Play 중)")]
    private void DropTestBomb() { DropTestItem(FieldItemKind.Bomb); }

    private void DropTestItem(FieldItemKind kind)
    {
        if (!Application.isPlaying) return;
        Transform player = ResolveFieldItemTarget();
        if (player == null)
        {
            Debug.LogWarning("SceneEnemyPool: 필드 아이템 테스트에 쓸 플레이어(PlayerData)를 찾지 못했습니다.", this);
            return;
        }
        if (!TryDropFieldItem(kind, player.position + new Vector3(2f, 0f, 0f)))
            Debug.LogWarning("SceneEnemyPool: 필드 아이템을 떨어뜨리지 못했습니다. Field Items의 Max Field Items와 이 컴포넌트의 활성 상태를 확인하세요.", this);
    }
#endif
}
