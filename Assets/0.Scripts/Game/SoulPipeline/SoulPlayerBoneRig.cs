using Unity.Collections;
using UnityEngine;

/// <summary>
/// 2D 뼈대 리깅 시험판. MainScene 플레이어 정면 대기 한 장(Idle/south_01)을 픽셀 격자 메시로 만들고
/// 뼈대 11개와 가중치를 심어 SkinnedMeshRenderer로 변형한다. 원본 그림·meta는 수정하지 않는다.
/// 스프라이트에 그려진 지팡이는 런타임 텍스처에서 지우고, ArmL 손 소켓에 3D 지팡이를 붙인다.
/// 부위 좌표는 이 그림 전용(64×64 텍스처, 왼쪽 위가 0인 픽셀 좌표)이다.
/// </summary>
public sealed class SoulPlayerBoneRig : MonoBehaviour
{
    public enum Motion { Idle, Walk, Attack }
    public enum Bone { Root, Hips, Spine, Head, Bow, HairL, HairR, ArmL, ArmR, LegL, LegR }

    public const int BoneCount = 11;
    public const float AttackCycleSeconds = 1.1f;
    const float FootRow = 57f;
    const float CenterColumn = 32f;
    const int SmoothPasses = 4;
    /// <summary>공격 휘두름 중 팔(몸통 메시)이 맡는 몫. 나머지는 3D 지팡이 소켓이 돈다.</summary>
    const float ArmShareOfSwing = 0.35f;
    // 화면 왼쪽(캐릭터 오른손) 지팡이 그립·끝. south_01 전용.
    static readonly Vector2 WandGripPixel = new Vector2(16f, 41f);
    static readonly Vector2 WandTipPixel = new Vector2(10f, 48f);

    static readonly Vector2[] Joints =
    {
        new Vector2(32, 57), new Vector2(32, 42), new Vector2(32, 34), new Vector2(32, 26), new Vector2(32, 10),
        new Vector2(19, 22), new Vector2(45, 22), new Vector2(24, 28), new Vector2(40, 28),
        new Vector2(27, 46), new Vector2(37, 46),
    };
    static readonly int[] Parents = { -1, 0, 1, 2, 3, 3, 3, 2, 2, 0, 0 };

    [SerializeField] private Motion motion = Motion.Idle;
    [Tooltip("이동량을 1픽셀 단위로 끊어 픽셀아트처럼 보이게 한다(회전은 그대로)")]
    [SerializeField] private bool pixelSnap = true;
    [Tooltip("스프라이트 지팡이를 지우고 손 소켓에 3D 지팡이를 붙인다")]
    [SerializeField] private bool attachWandProp = true;

    Transform[] bones;
    Vector3[] restPositions;
    SkinnedMeshRenderer skinned;
    Mesh mesh;
    Material material;
    Texture2D ownedTexture;
    Texture2D wandTexture;
    Material wandMaterial;
    Transform handSocket;
    Transform wandRoot;
    Transform wandTip;
    float pixelSize;
    int columns;
    int rows;
    int firstColumn;
    int firstRow;
    float startTime;
    int erasedWandPixels;
    float attackDuration = AttackCycleSeconds;

    public Motion CurrentMotion { get => motion; set { motion = value; startTime = Time.time; } }
    public bool PixelSnap { get => pixelSnap; set => pixelSnap = value; }
    public Transform[] Bones => bones;
    public SkinnedMeshRenderer Skinned => skinned;
    public Mesh SourceMesh => mesh;
    public float PixelSize => pixelSize;
    public Transform HandSocket => handSocket;
    public Transform WandRoot => wandRoot;
    public Transform WandTip => wandTip;
    public int ErasedWandPixels => erasedWandPixels;
    /// <summary>공격 한 번(AttackCycleSeconds 분량의 동작)을 실제로 재생하는 시간(초). 본게임 휘두르기 길이에 맞출 때 쓴다.</summary>
    public float AttackDuration { get => attackDuration; set => attackDuration = Mathf.Max(0.05f, value); }
    /// <summary>발 위치(Root 뼈대)에서 그림 맨 아래까지 내려간 거리(유닛). 원본 스프라이트처럼 바닥에 맞출 때 올려 준다.</summary>
    public float BottomBelowRoot => Mathf.Max(0f, (firstRow + rows - FootRow) * pixelSize);

    public static SoulPlayerBoneRig Create(Transform parent, Sprite sprite, float scale, bool attachWand = true)
    {
        if (sprite == null) return null;
        var go = new GameObject("BoneRigBody");
        go.transform.SetParent(parent, false);
        var rig = go.AddComponent<SoulPlayerBoneRig>();
        rig.attachWandProp = attachWand;
        rig.Build(sprite, scale);
        return rig;
    }

    /// <summary>그림 픽셀(위가 0) 모서리 좌표의 정점 번호. 영역 밖이면 -1.</summary>
    public int VertexAt(int column, int row)
    {
        int cx = column - firstColumn;
        int cy = row - firstRow;
        if (cx < 0 || cy < 0 || cx > columns || cy > rows) return -1;
        return cy * (columns + 1) + cx;
    }

    void Build(Sprite sprite, float scale)
    {
        pixelSize = scale / sprite.pixelsPerUnit;
        Texture2D source = sprite.texture;
        Rect rect = sprite.rect;
        columns = Mathf.RoundToInt(rect.width);
        rows = Mathf.RoundToInt(rect.height);
        firstColumn = Mathf.RoundToInt(rect.x);
        firstRow = source.height - Mathf.RoundToInt(rect.yMax);

        bones = new Transform[BoneCount];
        restPositions = new Vector3[BoneCount];
        for (int i = 0; i < BoneCount; i++)
        {
            var bone = new GameObject("Bone_" + (Bone)i).transform;
            bone.SetParent(i == 0 ? transform : bones[Parents[i]], false);
            Vector3 local = Local(Joints[i]) - (i == 0 ? Vector3.zero : Local(Joints[Parents[i]]));
            bone.localPosition = local;
            bones[i] = bone;
            restPositions[i] = local;
        }

        // ArmL = 화면 왼쪽(캐릭터 오른손·지팡이). 스키닝 뼈대 밖의 손 소켓만 추가한다.
        handSocket = new GameObject("HandSocket").transform;
        handSocket.SetParent(bones[(int)Bone.ArmL], false);
        handSocket.localPosition = Local(WandGripPixel) - Local(Joints[(int)Bone.ArmL]);
        handSocket.localRotation = Quaternion.identity;

        int vertexCount = (columns + 1) * (rows + 1);
        var vertices = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];
        var colors = new Color32[vertexCount];
        Texture2D drawTexture = source;
        if (attachWandProp)
        {
            ownedTexture = CopyTexture(source);
            erasedWandPixels = EraseWandPixels(ownedTexture, firstColumn, firstRow, columns, rows);
            drawTexture = ownedTexture;
        }

        for (int cy = 0; cy <= rows; cy++)
        for (int cx = 0; cx <= columns; cx++)
        {
            int col = firstColumn + cx;
            int row = firstRow + cy;
            int v = cy * (columns + 1) + cx;
            vertices[v] = Local(new Vector2(col, row));
            uvs[v] = new Vector2((float)col / drawTexture.width, (float)(drawTexture.height - row) / drawTexture.height);
            colors[v] = new Color32(255, 255, 255, 255);
        }

        var triangles = new int[columns * rows * 6];
        int t = 0;
        for (int cy = 0; cy < rows; cy++)
        for (int cx = 0; cx < columns; cx++)
        {
            int topLeft = cy * (columns + 1) + cx;
            int topRight = topLeft + 1;
            int bottomLeft = topLeft + columns + 1;
            int bottomRight = bottomLeft + 1;
            triangles[t++] = bottomLeft; triangles[t++] = topLeft; triangles[t++] = topRight;
            triangles[t++] = bottomLeft; triangles[t++] = topRight; triangles[t++] = bottomRight;
        }

        float[,] weights = BuildWeights(vertexCount);

        mesh = new Mesh { name = "SoulPlayerBoneRig" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors32 = colors;
        mesh.triangles = triangles;
        ApplyBoneWeights(mesh, weights, vertexCount);
        var bindposes = new Matrix4x4[BoneCount];
        for (int i = 0; i < BoneCount; i++)
            bindposes[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        mesh.bindposes = bindposes;
        mesh.RecalculateBounds();

        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = drawTexture };
        skinned = gameObject.AddComponent<SkinnedMeshRenderer>();
        skinned.bones = bones;
        skinned.rootBone = bones[0];
        skinned.sharedMesh = mesh;
        skinned.sharedMaterial = material;
        skinned.quality = SkinQuality.Bone4;
        skinned.updateWhenOffscreen = true;
        skinned.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        skinned.receiveShadows = false;

        if (attachWandProp)
            CreateWandProp();
        startTime = Time.time;
    }

    void CreateWandProp()
    {
        Vector3 tipLocal = Local(WandTipPixel) - Local(WandGripPixel);
        float length = tipLocal.magnitude;
        const int shaftWidth = 3;
        const int handlePixels = 2;
        int lengthPixels = Mathf.Max(6, Mathf.RoundToInt(length / pixelSize));
        int textureHeight = lengthPixels + handlePixels + 1;

        wandRoot = new GameObject("AttachedWand").transform;
        wandRoot.SetParent(handSocket, false);
        wandRoot.localPosition = Vector3.zero;
        wandRoot.localRotation = Quaternion.FromToRotation(Vector3.up, tipLocal.normalized);

        // 몸 그림과 같은 픽셀 격자로 보이도록 점 필터 스프라이트처럼 그린 지팡이.
        wandTexture = BuildWandTexture(shaftWidth, textureHeight);
        wandMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = wandTexture };

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "WandPixels";
        Object.Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(wandRoot, false);
        float height = textureHeight * pixelSize;
        quad.transform.localScale = new Vector3(shaftWidth * pixelSize, height, 1f);
        quad.transform.localPosition = new Vector3(0f, height * 0.5f - handlePixels * pixelSize, -0.15f);
        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = wandMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingOrder = 20;

        wandTip = new GameObject("WandTipPoint").transform;
        wandTip.SetParent(wandRoot, false);
        wandTip.localPosition = new Vector3(0f, length, -0.15f);
    }

    /// <summary>폭 3픽셀 자루(윤곽 어두움·가운데 갈색)와 맨 위 3×3 금색 별 끝. 아래가 손잡이 쪽.</summary>
    static Texture2D BuildWandTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "SoulWandPixels"
        };
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 outline = new Color32(0x3b, 0x20, 0x25, 255);
        Color32 wood = new Color32(0xb8, 0x7a, 0x3c, 255);
        Color32 gold = new Color32(0xfb, 0xbd, 0x63, 255);
        Color32 cream = new Color32(0xfd, 0xec, 0xab, 255);
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Color32 c;
            if (y >= height - 3)
                c = (x == 1 && y == height - 2) ? cream : gold;
            else
                c = x == 1 ? wood : outline;
            pixels[y * width + x] = c;
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    static Texture2D CopyTexture(Texture2D source)
    {
        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = source.name + "_NoWand"
        };
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply(false, false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    /// <summary>south_01 왼쪽 아래의 갈색 손잡이·노란 별 끝을 지운다. 분홍 옷·머리는 건드리지 않는다.</summary>
    public static int EraseWandPixels(Texture2D texture, int firstColumn, int firstRow, int columns, int rows)
    {
        int erased = 0;
        var pixels = texture.GetPixels32();
        int w = texture.width;
        int h = texture.height;
        for (int cy = 0; cy < rows; cy++)
        for (int cx = 0; cx < columns; cx++)
        {
            int col = firstColumn + cx;
            int row = firstRow + cy;
            if (!IsWandRegion(col, row)) continue;
            // ReadPixels 결과는 아래가 y=0.
            int texY = h - 1 - row;
            int i = texY * w + col;
            Color32 c = pixels[i];
            if (c.a < 8) continue;
            if (!LooksLikeWand(c)) continue;
            pixels[i] = new Color32(0, 0, 0, 0);
            erased++;
        }
        // 한 번 더: 지팡이 영역에서 이미 지운 이웃에 붙은 잔여 픽셀 제거.
        for (int pass = 0; pass < 2; pass++)
        {
            for (int cy = 0; cy < rows; cy++)
            for (int cx = 0; cx < columns; cx++)
            {
                int col = firstColumn + cx;
                int row = firstRow + cy;
                if (!IsWandRegion(col, row)) continue;
                int texY = h - 1 - row;
                int i = texY * w + col;
                if (pixels[i].a < 8 || !LooksLikeWand(pixels[i])) continue;
                bool nearCleared = false;
                for (int oy = -1; oy <= 1 && !nearCleared; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int nx = col + ox, ny = row + oy;
                    if (nx < firstColumn || ny < firstRow || nx >= firstColumn + columns || ny >= firstRow + rows) continue;
                    int ni = (h - 1 - ny) * w + nx;
                    if (pixels[ni].a < 8) { nearCleared = true; break; }
                }
                if (!nearCleared) continue;
                pixels[i] = new Color32(0, 0, 0, 0);
                erased++;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return erased;
    }

    // south_01: 지팡이 끝(별·윤곽·주황 테두리)은 (8~13, 45~50) 6×6 안에 있다. 몸 쪽 분홍 픽셀만 남긴다.
    static bool IsWandRegion(int x, int y) => x >= 8 && x <= 13 && y >= 45 && y <= 50;

    static bool LooksLikeWand(Color32 c)
    {
        bool pink = c.r > 190 && c.g < 140 && c.b > 100;
        return !pink;
    }

    Vector3 Local(Vector2 pixel) => new Vector3((pixel.x - CenterColumn) * pixelSize, (FootRow - pixel.y) * pixelSize, 0f);

    /// <summary>픽셀마다 부위를 정해 정점(모서리)에 나누고, 이웃 평균으로 경계를 부드럽게 해 찢어짐을 막는다.</summary>
    float[,] BuildWeights(int vertexCount)
    {
        var weights = new float[vertexCount, BoneCount];
        for (int cy = 0; cy < rows; cy++)
        for (int cx = 0; cx < columns; cx++)
        {
            int bone = (int)Region(firstColumn + cx, firstRow + cy);
            int topLeft = cy * (columns + 1) + cx;
            weights[topLeft, bone] += 1f;
            weights[topLeft + 1, bone] += 1f;
            weights[topLeft + columns + 1, bone] += 1f;
            weights[topLeft + columns + 2, bone] += 1f;
        }
        Normalize(weights, vertexCount);

        var next = new float[vertexCount, BoneCount];
        for (int pass = 0; pass < SmoothPasses; pass++)
        {
            for (int cy = 0; cy <= rows; cy++)
            for (int cx = 0; cx <= columns; cx++)
            {
                int v = cy * (columns + 1) + cx;
                int count = 0;
                var sum = new float[BoneCount];
                AddNeighbor(weights, sum, cx - 1, cy, ref count);
                AddNeighbor(weights, sum, cx + 1, cy, ref count);
                AddNeighbor(weights, sum, cx, cy - 1, ref count);
                AddNeighbor(weights, sum, cx, cy + 1, ref count);
                for (int b = 0; b < BoneCount; b++)
                    next[v, b] = 0.5f * weights[v, b] + 0.5f * sum[b] / count;
            }
            (weights, next) = (next, weights);
        }
        return weights;
    }

    void AddNeighbor(float[,] weights, float[] sum, int cx, int cy, ref int count)
    {
        if (cx < 0 || cy < 0 || cx > columns || cy > rows) return;
        int v = cy * (columns + 1) + cx;
        for (int b = 0; b < BoneCount; b++) sum[b] += weights[v, b];
        count++;
    }

    static void Normalize(float[,] weights, int vertexCount)
    {
        for (int v = 0; v < vertexCount; v++)
        {
            float total = 0f;
            for (int b = 0; b < BoneCount; b++) total += weights[v, b];
            if (total <= 0f) { weights[v, (int)Bone.Hips] = 1f; continue; }
            for (int b = 0; b < BoneCount; b++) weights[v, b] /= total;
        }
    }

    /// <summary>정점마다 큰 순서로 최대 4개 뼈대만 남겨 다시 합 1로 맞춘다.</summary>
    static void ApplyBoneWeights(Mesh target, float[,] weights, int vertexCount)
    {
        var perVertex = new NativeArray<byte>(vertexCount, Allocator.Temp);
        var list = new System.Collections.Generic.List<BoneWeight1>(vertexCount * 4);
        var order = new int[BoneCount];
        for (int v = 0; v < vertexCount; v++)
        {
            for (int b = 0; b < BoneCount; b++) order[b] = b;
            System.Array.Sort(order, (a, c) => weights[v, c].CompareTo(weights[v, a]));
            int keep = 0;
            float total = 0f;
            for (; keep < 4 && weights[v, order[keep]] > 0.001f; keep++) total += weights[v, order[keep]];
            for (int k = 0; k < keep; k++)
                list.Add(new BoneWeight1 { boneIndex = order[k], weight = weights[v, order[k]] / total });
            perVertex[v] = (byte)keep;
        }
        var all = new NativeArray<BoneWeight1>(list.ToArray(), Allocator.Temp);
        target.SetBoneWeights(perVertex, all);
        perVertex.Dispose();
        all.Dispose();
    }

    /// <summary>원본 픽셀 좌표(왼쪽 위 0) → 담당 뼈대. 쌍갈래 머리·소매·다리를 손으로 나눈 값이다.</summary>
    public static Bone Region(int x, int y)
    {
        if (y >= 46 && x >= 20 && x <= 44) return x < 32 ? Bone.LegL : Bone.LegR;
        if ((x <= 16 && y >= 43) || (x >= 15 && x <= 17 && y >= 40) || (x >= 17 && x <= 23 && y >= 34 && y <= 42) || (x >= 20 && x <= 26 && y >= 26 && y < 34))
            return Bone.ArmL;
        if ((x >= 37 && x <= 42 && y >= 26 && y < 34) || (x >= 42 && x <= 48 && y >= 34 && y <= 42))
            return Bone.ArmR;
        if (y <= 11 && x >= 19 && x <= 45) return Bone.Bow;
        if (y <= 25 && x >= 17 && x <= 47) return Bone.Head;
        if (x <= 17 || (x <= 20 && y >= 12)) return Bone.HairL;
        if (x >= 47 || (x >= 44 && y >= 12)) return Bone.HairR;
        if (y >= 36) return Bone.Hips;
        return Bone.Spine;
    }

    void LateUpdate()
    {
        transform.rotation = SoulSpriteBillboard.ViewRotation();
        float elapsed = Time.time - startTime;
        if (motion == Motion.Attack) elapsed *= AttackCycleSeconds / attackDuration;
        Pose(motion, elapsed);
    }

    /// <summary>시간 t의 자세. 단위는 원본 픽셀·도(degree). 다리는 Root 자식이라 몸이 출렁여도 발이 바닥에 남는다.</summary>
    public void Pose(Motion pose, float t)
    {
        ResetPose();
        switch (pose)
        {
            case Motion.Idle:
            {
                float w = t * Mathf.PI * 2f / 1.2f;
                Move(Bone.Hips, 0f, -(0.5f - 0.5f * Mathf.Cos(w)));
                Turn(Bone.Head, 1.5f * Mathf.Sin(w - 0.6f));
                Turn(Bone.Bow, 4f * Mathf.Sin(w - 1.1f));
                Turn(Bone.HairL, 3f * Mathf.Sin(w - 1.3f));
                Turn(Bone.HairR, -3f * Mathf.Sin(w - 1.5f));
                Turn(Bone.ArmL, 2.5f * Mathf.Sin(w - 0.5f));
                Turn(Bone.ArmR, -2.5f * Mathf.Sin(w - 0.5f));
                break;
            }
            case Motion.Walk:
            {
                // 정면 걷기: 큰 회전 대신 체중 이동·발걸음 출렁임·머리카락 뒤늦은 흔들림만 작게 쓴다(그림이 찌그러지지 않게).
                float p = t * Mathf.PI * 2f / 0.7f;
                float s = Mathf.Sin(p);
                float step = 0.5f - 0.5f * Mathf.Cos(2f * p);
                Move(Bone.Hips, 1f * s, -1f * step);
                Turn(Bone.Hips, 1f * s);
                Move(Bone.LegL, 0f, 2f * Mathf.Max(0f, s));
                Move(Bone.LegR, 0f, 2f * Mathf.Max(0f, -s));
                Move(Bone.ArmL, 0f, 1f * s);
                Move(Bone.ArmR, 0f, -1f * s);
                // 발을 디딜 때(step 최대) 두 팔이 바깥으로 살짝 벌어지고, 체중 이동 쪽으로 약간 쏠린다.
                Turn(Bone.ArmL, -6f * step - 2f * s);
                Turn(Bone.ArmR, 6f * step - 2f * s);
                Turn(Bone.Head, -1f * s);
                Turn(Bone.Bow, 3f * Mathf.Sin(p - 0.9f));
                Turn(Bone.HairL, 3f * Mathf.Sin(p - 1.1f));
                Turn(Bone.HairR, 3f * Mathf.Sin(p - 1.3f));
                break;
            }            case Motion.Attack:
            {
                float a = Mathf.Repeat(t, AttackCycleSeconds);
                float arm = AttackArmAngle(a);
                // 지팡이는 별도 소품이라 큰 휘두름은 손 소켓이 맡고, 팔 메시는 조금만 돌려 몸통 늘어남을 줄인다.
                Turn(Bone.ArmL, arm * ArmShareOfSwing);
                TurnWand(arm * (1f - ArmShareOfSwing));
                Turn(Bone.Spine, arm * 0.06f);
                Move(Bone.Hips, arm * 0.02f, 0f);
                Turn(Bone.Head, arm * 0.03f);
                float lag = AttackArmAngle(Mathf.Max(0f, a - 0.08f));
                Turn(Bone.HairL, -lag * 0.12f);
                Turn(Bone.HairR, -lag * 0.12f);
                Turn(Bone.Bow, -lag * 0.08f);
                break;
            }
        }
    }

    /// <summary>바인드 자세(원본 그림 그대로).</summary>
    public void ResetPose()
    {
        for (int i = 0; i < BoneCount; i++)
        {
            bones[i].localPosition = restPositions[i];
            bones[i].localRotation = Quaternion.identity;
        }
        if (handSocket != null) handSocket.localRotation = Quaternion.identity;
    }

    /// <summary>지팡이 팔: 바깥으로 들어올림(-55°) → 몸 안쪽으로 휘두름(+45°) → 유지 → 복귀.</summary>
    public static float AttackArmAngle(float a)
    {
        if (a < 0.25f) return -55f * Mathf.SmoothStep(0f, 1f, a / 0.25f);
        if (a < 0.4f) return Mathf.Lerp(-55f, 45f, Mathf.SmoothStep(0f, 1f, (a - 0.25f) / 0.15f));
        if (a < 0.55f) return 45f;
        if (a < 0.9f) return Mathf.Lerp(45f, 0f, Mathf.SmoothStep(0f, 1f, (a - 0.55f) / 0.35f));
        return 0f;
    }

    void Move(Bone bone, float dxPixels, float dyPixels)
    {
        if (pixelSnap)
        {
            dxPixels = Mathf.Round(dxPixels);
            dyPixels = Mathf.Round(dyPixels);
        }
        bones[(int)bone].localPosition += new Vector3(dxPixels * pixelSize, dyPixels * pixelSize, 0f);
    }

    void Turn(Bone bone, float degrees) => bones[(int)bone].localRotation *= Quaternion.Euler(0f, 0f, degrees);

    void TurnWand(float degrees)
    {
        if (handSocket != null) handSocket.localRotation *= Quaternion.Euler(0f, 0f, degrees);
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        if (ownedTexture != null) Destroy(ownedTexture);
        if (wandTexture != null) Destroy(wandTexture);
        if (wandMaterial != null) Destroy(wandMaterial);
    }
}
