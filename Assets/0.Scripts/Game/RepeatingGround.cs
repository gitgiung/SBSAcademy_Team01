using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
[DefaultExecutionOrder(1000)]
public sealed class RepeatingGround : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;

    private const int PaddingCells = 2;
    private Tilemap tilemap;
    private BoundsInt sourceBounds;
    private TileBase[] pattern;
    private TileBase[] emptyPattern;
    private RectInt activeChunks;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemap.CompressBounds();
        sourceBounds = tilemap.cellBounds;

        if (worldCamera == null || sourceBounds.size.x == 0 || sourceBounds.size.y == 0)
        {
            Debug.LogError("RepeatingGround requires a camera and a painted Tilemap.", this);
            enabled = false;
            return;
        }

        pattern = tilemap.GetTilesBlock(sourceBounds);
        emptyPattern = new TileBase[pattern.Length];
        activeChunks = new RectInt(0, 0, 1, 1);
    }

    private void LateUpdate()
    {
        var plane = new Plane(transform.forward, transform.position);
        var min = new Vector2Int(int.MaxValue, int.MaxValue);
        var max = new Vector2Int(int.MinValue, int.MinValue);

        for (int corner = 0; corner < 4; corner++)
        {
            Ray ray = worldCamera.ViewportPointToRay(new Vector3(corner % 2, corner / 2));
            if (!plane.Raycast(ray, out float distance)) return;

            Vector3Int cell = tilemap.WorldToCell(ray.GetPoint(distance));
            min = Vector2Int.Min(min, new Vector2Int(cell.x, cell.y));
            max = Vector2Int.Max(max, new Vector2Int(cell.x, cell.y));
        }

        var padding = new Vector2Int(PaddingCells, PaddingCells);
        Vector2Int first = CellToChunk(min - padding);
        Vector2Int last = CellToChunk(max + padding);
        var neededChunks = new RectInt(first, last - first + Vector2Int.one);
        if (neededChunks == activeChunks) return;

        WriteChangedChunks(activeChunks, neededChunks, emptyPattern);
        WriteChangedChunks(neededChunks, activeChunks, pattern);
        activeChunks = neededChunks;
        tilemap.CompressBounds();
    }

    private Vector2Int CellToChunk(Vector2Int cell)
    {
        return new Vector2Int(
            Mathf.FloorToInt((cell.x - sourceBounds.xMin) / (float)sourceBounds.size.x),
            Mathf.FloorToInt((cell.y - sourceBounds.yMin) / (float)sourceBounds.size.y));
    }

    private void WriteChangedChunks(RectInt area, RectInt unchanged, TileBase[] tiles)
    {
        foreach (Vector2Int chunk in area.allPositionsWithin)
        {
            if (unchanged.Contains(chunk)) continue;

            Vector3Int origin = sourceBounds.position + new Vector3Int(
                chunk.x * sourceBounds.size.x, chunk.y * sourceBounds.size.y, 0);
            tilemap.SetTilesBlock(new BoundsInt(origin, sourceBounds.size), tiles);
        }
    }
}
