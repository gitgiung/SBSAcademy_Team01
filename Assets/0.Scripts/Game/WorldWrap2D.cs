using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 플레이어가 맵 경계를 넘으면 반대편으로 이동시키고,
/// 경계 바깥 화면에는 반복 타일맵을 보여주는 컴포넌트입니다.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class WorldWrap2D : MonoBehaviour
{
    // 경계 밖에 반복해서 배치할 원본 타일맵입니다. 이 씬에서는 Ground 아래 Tilemap을 연결합니다.
    [SerializeField] private Tilemap sourceTilemap;

    [SerializeField, InspectorName("World Bounds"), Tooltip("월드 좌표 기준 경계입니다. 타일맵 크기와 맞추면 가장자리가 자연스럽게 반복됩니다.")]
    private Rect boundsRect = new Rect(-37f, -21f, 67f, 37f);

    // 주변 8방향에 생성한 타일맵 사본입니다. 씬 종료 시 정리할 때 사용합니다.
    private readonly List<GameObject> visualCopies = new List<GameObject>(8);

    private Rigidbody2D body;

    private Bounds worldBounds;

    private bool isReady;

    private void Awake()
    {
        if (sourceTilemap == null)
        {
            Debug.LogError("WorldWrap2D requires a source Tilemap.", this);
            enabled = false;
            return;
        }

        BoundsInt cells = sourceTilemap.cellBounds;
        if (cells.size.x <= 0 || cells.size.y <= 0)
        {
            Debug.LogError("WorldWrap2D source Tilemap has no cell bounds.", sourceTilemap);
            enabled = false;
            return;
        }

        // 셀 범위의 시작 좌표와 끝 다음 셀 좌표를 월드 좌표로 바꿔 원본 타일맵 크기를 구합니다.
        GridLayout grid = sourceTilemap.layoutGrid;
        Vector3 mapMin = grid.CellToWorld(cells.min);
        Vector3 mapMax = grid.CellToWorld(new Vector3Int(cells.xMax, cells.yMax, cells.zMin));
        Vector2 mapSize = new Vector2(mapMax.x - mapMin.x, mapMax.y - mapMin.y);
        // 경계 크기는 0보다 커야 나머지 연산으로 안전하게 위치를 접을 수 있습니다.
        if (boundsRect.width <= 0f || boundsRect.height <= 0f)
        {
            Debug.LogError("WorldWrap2D bounds must have a positive width and height.", this);
            enabled = false;
            return;
        }

        // 설정한 Rect의 중심으로 원본 맵을 옮길 준비를 합니다. 맵 크기도 비교해 이음새 문제를 알립니다.
        Vector3 targetCenter = new Vector3(boundsRect.center.x, boundsRect.center.y, mapMin.z);
        Vector3 mapCenter = (mapMin + mapMax) * 0.5f;
        // 복제 대상인 Grid를 찾습니다. 현재 구조에서는 Tilemap이 Grid의 자식이어야 합니다.
        Transform mapRoot = sourceTilemap.transform.parent;
        if (mapRoot == null)
        {
            Debug.LogError("WorldWrap2D expects the source Tilemap to be a child of its Grid.", sourceTilemap);
            enabled = false;
            return;
        }

        // Inspector에서 경계 위치를 바꾸면 실행 중 원본 맵도 그 중심에 맞춥니다.
        // Play 중 변경이므로 씬 에셋에 저장된 Ground 위치는 수정하지 않습니다.
        mapRoot.position += targetCenter - mapCenter;

        // 경계 크기와 원본 맵 크기가 다르면 복제 간격과 타일맵 가장자리가 맞지 않을 수 있습니다.
        if (!Mathf.Approximately(mapSize.x, boundsRect.width) || !Mathf.Approximately(mapSize.y, boundsRect.height))
        {
            Debug.LogWarning("WorldWrap2D bounds size differs from the source Tilemap. Set them to the same size for seamless visual repetition.", this);
        }

        // 매 프레임 사용할 월드 경계를 보관하고 플레이어 물리 컴포넌트를 찾은 뒤 주변 맵을 생성합니다.
        worldBounds = new Bounds(targetCenter, new Vector3(boundsRect.width, boundsRect.height, 0f));
        body = GetComponent<Rigidbody2D>();
        CreateVisualCopies();
        isReady = enabled;
    }

    /// <summary>플레이어 위치가 경계를 벗어났는지 확인하고 반대편 좌표로 이동시킵니다.</summary>
    private void LateUpdate()
    {
        if (!isReady) return;

        Vector2 position = body != null ? body.position : (Vector2)transform.position;

        float wrappedX = worldBounds.min.x + Mathf.Repeat(position.x - worldBounds.min.x, worldBounds.size.x);
        float wrappedY = worldBounds.min.y + Mathf.Repeat(position.y - worldBounds.min.y, worldBounds.size.y);

        if (Mathf.Approximately(position.x, wrappedX) && Mathf.Approximately(position.y, wrappedY)) return;

        Vector2 wrappedPosition = new Vector2(wrappedX, wrappedY);
        if (body != null) body.position = wrappedPosition;
        else transform.position = new Vector3(wrappedX, wrappedY, transform.position.z);
    }

    /// <summary>원본 Ground를 둘러싼 8칸에 같은 타일맵을 실행 중 복제합니다.</summary>
    private void CreateVisualCopies()
    {
        // Tilemap 자체가 아니라 Grid 루트를 복제해 타일맵의 Grid 설정과 자식 구조를 함께 유지합니다.
        Transform mapRoot = sourceTilemap.transform.parent;

        float width = worldBounds.size.x;
        float height = worldBounds.size.y;

        // X/Y 각각 -1, 0, 1 조합 중 원본 위치(0,0)를 제외해 가장자리와 모서리를 모두 채웁니다.
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                GameObject copy = Instantiate(mapRoot.gameObject,
                    mapRoot.position + new Vector3(x * width, y * height, 0f), mapRoot.rotation);
                copy.name = $"{mapRoot.name} (Wrap {x},{y})";
                visualCopies.Add(copy);
            }
        }
    }

    /// <summary>Inspector에서 경계 크기를 0 이하로 설정하지 못하도록 보정합니다.</summary>
    private void OnValidate()
    {
        boundsRect.width = Mathf.Max(0.01f, boundsRect.width);
        boundsRect.height = Mathf.Max(0.01f, boundsRect.height);
    }

    /// <summary>Scene 뷰에서 선택된 컴포넌트의 경계 사각형과 크기를 표시합니다.</summary>
    private void OnDrawGizmosSelected()
    {
        if (boundsRect.width <= 0f || boundsRect.height <= 0f) return;

        // Inspector의 월드 좌표와 크기로 경계 테두리를 그립니다.
        Vector3 center = new Vector3(boundsRect.center.x, boundsRect.center.y, transform.position.z);
        Vector3 size = new Vector3(boundsRect.width, boundsRect.height, 0f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
#if UNITY_EDITOR
        Handles.color = Color.cyan;
        Handles.Label(new Vector3(boundsRect.xMin, boundsRect.yMax, center.z),
            $"World Bounds  {boundsRect.width:0.##} x {boundsRect.height:0.##}");
#endif
    }

    /// <summary>이 컴포넌트가 제거될 때 생성했던 이웃 맵 사본도 함께 정리합니다.</summary>
    private void OnDestroy()
    {
        foreach (GameObject copy in visualCopies)
        {
            if (copy != null) Destroy(copy);
        }
        visualCopies.Clear();
    }
}
