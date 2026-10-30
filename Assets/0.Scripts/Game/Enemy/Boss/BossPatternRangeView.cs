using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(BossController))]
public sealed class BossPatternRangeView : MonoBehaviour
{
    [Header("게임 화면 범위 표시")]
    [SerializeField] private bool showInGameRanges = true;
    [SerializeField, Min(0.001f)] private float startRangeLineWidth = 0.035f;
    [SerializeField, Range(0f, 1f)] private float attackRangeAlpha = 0.22f;
    [SerializeField, Range(0f, 1f)] private float startRangeLineAlpha = 0.85f;
    [SerializeField, Min(0f)] private float pattern1RangeDisplayDuration = 0.25f;

    private const int CircleSegmentCount = 64;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Color DebugRed = new Color(1f, 0f, 0f, 1f);

    private BossController boss;
    private Transform visualRoot;
    private Transform slashVisual;
    private Transform dashEndVisual;
    private Mesh slashMesh;
    private Mesh dashEndMesh;
    private MeshRenderer slashRenderer;
    private MeshRenderer dashEndRenderer;
    private LineRenderer closeRangeLine;
    private LineRenderer specialRangeLine;
    private Material rangeMaterial;
    private MaterialPropertyBlock propertyBlock;
    private float builtSlashRange = -1f;
    private float builtSlashAngle = -1f;
    private float builtDashEndRadius = -1f;
    private float pattern1RangeVisibleUntil;

    private void Awake()
    {
        boss = GetComponent<BossController>();
        propertyBlock = new MaterialPropertyBlock();

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("게임 내 보스 범위를 표시할 URP Unlit 셰이더를 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        rangeMaterial = CreateTransparentMaterial(shader);
        CreateVisuals();
        RefreshMeshes();
    }

    private void LateUpdate()
    {
        if (visualRoot == null || boss == null) return;

        visualRoot.gameObject.SetActive(showInGameRanges && boss.isActiveAndEnabled);
        if (!showInGameRanges || !boss.isActiveAndEnabled) return;

        visualRoot.localScale = GetInverseParentScale();
        RefreshMeshes();
        ApplyFillColor(slashRenderer);
        ApplyFillColor(dashEndRenderer);

        if (boss.ActivePattern == BossPattern.Pattern1)
            pattern1RangeVisibleUntil = Time.unscaledTime + pattern1RangeDisplayDuration;

        slashRenderer.enabled = boss.ActivePattern == BossPattern.Pattern1 ||
                                boss.ActivePattern == BossPattern.Pattern2 ||
                                Time.unscaledTime < pattern1RangeVisibleUntil;
        dashEndRenderer.enabled = boss.ActivePattern == BossPattern.Pattern3;

        Vector3 bossPosition = boss.transform.position;
        Vector2 forward = boss.Target != null
            ? (Vector2)boss.Target.position - (Vector2)bossPosition
            : Vector2.right;
        if (forward.sqrMagnitude <= Mathf.Epsilon) forward = Vector2.right;

        slashVisual.position = bossPosition;
        slashVisual.rotation = Quaternion.FromToRotation(Vector3.right, forward.normalized);
        dashEndVisual.position = boss.Target != null ? boss.Target.position : bossPosition;

        UpdateCircleLine(closeRangeLine, bossPosition, boss.CloseRange);
        UpdateCircleLine(specialRangeLine, bossPosition, boss.SpecialPatternDistance);
    }

    private void CreateVisuals()
    {
        visualRoot = new GameObject("Boss Pattern Range Debug").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localScale = GetInverseParentScale();

        slashVisual = CreateMeshVisual("Slash Attack Range", out slashMesh, out slashRenderer);
        dashEndVisual = CreateMeshVisual("Dash End Attack Range", out dashEndMesh, out dashEndRenderer);
        closeRangeLine = CreateRangeLine("Close Pattern Start Range");
        specialRangeLine = CreateRangeLine("Special Pattern Start Range");
    }

    private Transform CreateMeshVisual(string objectName, out Mesh mesh, out MeshRenderer meshRenderer)
    {
        var visual = new GameObject(objectName).transform;
        visual.SetParent(visualRoot, false);
        visual.localScale = Vector3.one;

        MeshFilter filter = visual.gameObject.AddComponent<MeshFilter>();
        meshRenderer = visual.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = rangeMaterial;
        meshRenderer.sortingOrder = 100;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        mesh = new Mesh { name = objectName };
        filter.sharedMesh = mesh;
        return visual;
    }

    private LineRenderer CreateRangeLine(string objectName)
    {
        var lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(visualRoot, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = CircleSegmentCount;
        line.startWidth = startRangeLineWidth;
        line.endWidth = startRangeLineWidth;
        line.startColor = new Color(DebugRed.r, DebugRed.g, DebugRed.b, startRangeLineAlpha);
        line.endColor = line.startColor;
        line.sharedMaterial = rangeMaterial;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.sortingOrder = 101;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    private static Material CreateTransparentMaterial(Shader shader)
    {
        var material = new Material(shader) { name = "Boss Pattern Range Debug Material" };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor(BaseColorId, Color.white);
        return material;
    }

    private void RefreshMeshes()
    {
        if (slashMesh == null || !Mathf.Approximately(builtSlashRange, boss.SlashRange) ||
            !Mathf.Approximately(builtSlashAngle, boss.SlashAngle))
        {
            UpdateSectorMesh(slashMesh, boss.SlashRange, boss.SlashAngle);
            builtSlashRange = boss.SlashRange;
            builtSlashAngle = boss.SlashAngle;
        }

        if (dashEndMesh == null || !Mathf.Approximately(builtDashEndRadius, boss.DashEndDamageRadius))
        {
            UpdateSectorMesh(dashEndMesh, boss.DashEndDamageRadius, 360f);
            builtDashEndRadius = boss.DashEndDamageRadius;
        }
    }

    private void ApplyFillColor(Renderer targetRenderer)
    {
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, new Color(DebugRed.r, DebugRed.g, DebugRed.b, attackRangeAlpha));
        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void UpdateCircleLine(LineRenderer line, Vector3 center, float radius)
    {
        line.startWidth = startRangeLineWidth;
        line.endWidth = startRangeLineWidth;
        line.startColor = new Color(DebugRed.r, DebugRed.g, DebugRed.b, startRangeLineAlpha);
        line.endColor = line.startColor;

        for (int i = 0; i < CircleSegmentCount; i++)
        {
            float angle = 2f * Mathf.PI * i / CircleSegmentCount;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private Vector3 GetInverseParentScale()
    {
        Vector3 scale = transform.lossyScale;
        return new Vector3(
            Mathf.Abs(scale.x) > Mathf.Epsilon ? 1f / scale.x : 1f,
            Mathf.Abs(scale.y) > Mathf.Epsilon ? 1f / scale.y : 1f,
            Mathf.Abs(scale.z) > Mathf.Epsilon ? 1f / scale.z : 1f);
    }

    private static void UpdateSectorMesh(Mesh mesh, float radius, float angle)
    {
        const int segmentCount = 64;
        var vertices = new List<Vector3>(segmentCount + 2) { Vector3.zero };
        var triangles = new List<int>(segmentCount * 6);
        int actualSegments = Mathf.Max(1, Mathf.CeilToInt(segmentCount * angle / 360f));

        for (int i = 0; i <= actualSegments; i++)
        {
            float currentAngle = (-angle * 0.5f + angle * i / actualSegments) * Mathf.Deg2Rad;
            vertices.Add(new Vector3(Mathf.Cos(currentAngle) * radius, Mathf.Sin(currentAngle) * radius, 0f));
        }

        for (int i = 0; i < actualSegments; i++)
        {
            int first = i + 1;
            int second = i + 2;
            triangles.Add(0);
            triangles.Add(first);
            triangles.Add(second);
            triangles.Add(0);
            triangles.Add(second);
            triangles.Add(first);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void OnValidate()
    {
        startRangeLineWidth = Mathf.Max(0.001f, startRangeLineWidth);
        attackRangeAlpha = Mathf.Clamp01(attackRangeAlpha);
        startRangeLineAlpha = Mathf.Clamp01(startRangeLineAlpha);
        pattern1RangeDisplayDuration = Mathf.Max(0f, pattern1RangeDisplayDuration);
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            if (slashMesh != null) Destroy(slashMesh);
            if (dashEndMesh != null) Destroy(dashEndMesh);
            if (rangeMaterial != null) Destroy(rangeMaterial);
        }
    }
}
