using UnityEngine;

/// <summary>SoulArena에서 Main 2D 스프라이트를 3D 바닥 위 빌보드로 세우는 공용 처리.</summary>
public static class SoulSpriteBillboard
{
    public static Transform CreateBody(Transform parent, string name, float scale, RuntimeAnimatorController controller,
        out SpriteRenderer renderer, out Animator animator, out Material material)
    {
        foreach (var r in parent.GetComponentsInChildren<Renderer>())
            r.enabled = false;

        var body = new GameObject(name).transform;
        body.SetParent(parent, false);
        body.localScale = Vector3.one * scale;
        renderer = body.gameObject.AddComponent<SpriteRenderer>();
        material = new Material(Shader.Find("Sprites/Default"));
        renderer.sharedMaterial = material;
        animator = body.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return body;
    }

    /// <summary>
    /// 카메라 정면 빌보드. lift는 첫 기본 프레임의 하단을 바닥(root)에 맞춘 피벗 높이로 고정한다.
    /// 프레임마다 다시 맞추면 휘두르기처럼 사각형 크기가 다른 프레임에서 발이 떠 보인다(Main은 피벗 고정).
    /// </summary>
    public static void Place(Transform body, SpriteRenderer renderer, Vector3 root, float scale, ref float lift)
    {
        if (lift <= 0f && renderer.sprite != null)
            lift = -renderer.sprite.bounds.min.y * scale;
        Quaternion camRot = ViewRotation();
        body.SetPositionAndRotation(root + camRot * Vector3.up * Mathf.Max(lift, 0f), camRot);
    }

    public static Quaternion ViewRotation()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.rotation : SoulRenderSpec.CameraRotation;
    }

    /// <summary>XZ 평면 방향을 카메라 화면 기준 2D 방향(위=화면 위)으로 변환.</summary>
    public static Vector2 ToScreenDirection(Vector3 worldDir, Quaternion camRot)
    {
        Vector3 right = camRot * Vector3.right;
        Vector3 forward = camRot * Vector3.forward;
        right.y = 0f;
        forward.y = 0f;
        if (right.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f) return Vector2.zero;
        right.Normalize();
        forward.Normalize();
        worldDir.y = 0f;
        return new Vector2(Vector3.Dot(worldDir, right), Vector3.Dot(worldDir, forward));
    }

    public static Vector2 Quantize8(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x);
        float step = Mathf.PI / 4f;
        float q = Mathf.Round(angle / step) * step;
        return new Vector2(Mathf.Cos(q), Mathf.Sin(q));
    }

    /// <summary>Main BossController와 같은 8방향 이름(East, NorthEast …).</summary>
    public static string OctantName(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        switch (Mathf.RoundToInt(angle / 45f) % 8)
        {
            case 0: return "East";
            case 1: return "NorthEast";
            case 2: return "North";
            case 3: return "NorthWest";
            case 4: return "West";
            case 5: return "SouthWest";
            case 6: return "South";
            default: return "SouthEast";
        }
    }

    public static Transform CreateBlobShadow(Transform parent, Vector2 size, out Material material)
    {
        const int res = 32;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = (x + 0.5f) / res * 2f - 1f;
            float dy = (y + 0.5f) / res * 2f - 1f;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            tex.SetPixel(x, y, new Color(0f, 0f, 0f, a * a * 0.55f));
        }
        tex.Apply();

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "SpriteShadow";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        material = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return go.transform;
    }
}
