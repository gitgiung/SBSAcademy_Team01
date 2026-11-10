using UnityEngine;

/// <summary>
/// SoulArena3D 본선 파이프라인 부트스트랩.
/// Prefab이 있으면 사용하고, 없으면 3D 폴백 몸을 생성해 Play 가능하게 한다.
/// </summary>
[DisallowMultipleComponent]
public sealed class SoulArenaBootstrap : MonoBehaviour
{
    private const string PlayerPrefabPath = "SoulPipeline/SoulPlayerHumanoid";
    private const string BossPrefabPath = "SoulPipeline/SoulBossHumanoid";

    [SerializeField] private SoulPipelineAvatar player;
    [SerializeField] private SoulBossApproachSlash boss;
    [SerializeField] private SoulCameraRig cameraRig;
    [SerializeField] private bool buildWorldIfEmpty = true;

    private GUIStyle hudStyle;
    private bool usingFallback;
    private SoulArenaDummyHealth bossHealth;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (GetComponent<SoulArena3DScene>() == null && FindFirstObjectByType<SoulArena3DScene>() == null)
            gameObject.AddComponent<SoulArena3DScene>();
        if (GetComponent<SoulArenaWinLoseHud>() == null)
            gameObject.AddComponent<SoulArenaWinLoseHud>();
        if (GetComponent<SoulArenaWaveDirector>() == null)
            gameObject.AddComponent<SoulArenaWaveDirector>();
        if (GetComponent<SoulArenaDebugOverlay>() == null)
            gameObject.AddComponent<SoulArenaDebugOverlay>();
        if (GetComponent<SoulArenaPause>() == null)
            gameObject.AddComponent<SoulArenaPause>();
        if (GetComponent<SoulArenaKillFeed>() == null)
            gameObject.AddComponent<SoulArenaKillFeed>();
        if (GetComponent<SoulArenaBossTelegraphHud>() == null)
            gameObject.AddComponent<SoulArenaBossTelegraphHud>();
        if (GetComponent<SoulArenaInputHint>() == null)
            gameObject.AddComponent<SoulArenaInputHint>();
        if (GetComponent<SoulParryRiposteHint>() == null)
            gameObject.AddComponent<SoulParryRiposteHint>();
        if (buildWorldIfEmpty)
            EnsureWorld();
    }

    private void Start()
    {
        WireReferences();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TryReturnToLobby();
        if (Input.GetKeyDown(KeyCode.R))
            ReloadArena();
    }

    private static void ReloadArena()
    {
        Time.timeScale = 1f;
        try
        {
            if (Managers.Scene != null)
                Managers.Scene.LoadScene(Define.EScene.SoulArena3D);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("SoulArena3D");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SoulPipeline] 재시작 실패: {ex.Message}");
        }
    }

    private static void TryReturnToLobby()
    {
        Time.timeScale = 1f;
        try
        {
            if (Managers.Scene != null)
                Managers.Scene.LoadScene(Define.EScene.LobbyScene);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SoulPipeline] 로비 복귀 실패: {ex.Message}");
        }
    }

    private void EnsureWorld()
    {
        if (FindFirstObjectByType<SoulCameraRig>() == null)
            EnsureCamera();

        if (FindFirstObjectByType<Light>() == null)
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        if (GameObject.Find("FlatGround3D") == null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FlatGround3D";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = SoulRenderSpec.FlatGroundScale / 10f;
            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                renderer.sharedMaterial = new Material(shader) { color = new Color(0.23f, 0.25f, 0.28f, 1f) };
            }
        }

        if (FindFirstObjectByType<SoulPipelineAvatar>() == null)
        {
            GameObject playerGo = TryInstantiatePrefab(PlayerPrefabPath, new Vector3(-3f, 0f, 0f));
            if (playerGo == null)
            {
                playerGo = SoulArenaFallbackFactory.CreatePlayer(new Vector3(-3f, 0f, 0f));
                usingFallback = true;
            }
        }

        if (FindFirstObjectByType<SoulBossApproachSlash>() == null)
        {
            GameObject bossGo = TryInstantiatePrefab(BossPrefabPath, new Vector3(4.5f, 0f, 0f));
            if (bossGo == null)
            {
                bossGo = SoulArenaFallbackFactory.CreateBoss(new Vector3(4.5f, 0f, 0f));
                usingFallback = true;
            }
        }

        if (FindFirstObjectByType<SoulArenaTrashMob>() == null)
        {
            Color green = new Color(0.35f, 0.8f, 0.4f);
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI * 2f / 6f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * 7.5f, 0f, Mathf.Sin(ang) * 7.5f);
                SoulArenaTrashMob.Spawn(pos, Color.Lerp(green, Color.yellow, i / 6f));
            }
        }
    }

    private static GameObject TryInstantiatePrefab(string resourcesPath, Vector3 position)
    {
        var prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab == null) return null;
        var go = Instantiate(prefab);
        go.transform.position = position;
        return go;
    }

    private void EnsureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }

        if (cam.GetComponent<SoulCameraRig>() == null)
            cam.gameObject.AddComponent<SoulCameraRig>();
        if (cam.GetComponent<SoulCameraShake>() == null)
            cam.gameObject.AddComponent<SoulCameraShake>();
    }

    private void WireReferences()
    {
        if (player == null) player = FindFirstObjectByType<SoulPipelineAvatar>();
        if (boss == null) boss = FindFirstObjectByType<SoulBossApproachSlash>();
        if (cameraRig == null) cameraRig = FindFirstObjectByType<SoulCameraRig>();

        if (player != null && cameraRig != null)
        {
            var cam = Camera.main;
            if (cam != null) cameraRig.Bind(player.transform, cam);
        }

        if (player != null && boss != null)
        {
            var hitbox = boss.GetComponentInChildren<SoulWeaponHitbox3D>(true);
            boss.Bind(player.transform, hitbox);
            bossHealth = boss.GetComponent<SoulArenaDummyHealth>();
        }

        if (player == null || boss == null)
            Debug.LogWarning("[SoulPipeline] 플레이어/보스 없음. Tools > Soul Pipeline > 1~3 실행 권장.", this);
        else if (usingFallback)
            Debug.Log("[SoulPipeline] Prefab 없음 → 3D 폴백 몸 사용 중. 메뉴 1~3 후 본 애니로 교체.", this);
    }

    private void OnGUI()
    {
        hudStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
        string hp = player != null ? $"{player.Health:0}/{player.MaxHealth:0}" : "-";
        string bossHp = bossHealth != null ? $"{bossHealth.Health:0}/{bossHealth.MaxHealth:0}" : "-";
        string mode = usingFallback ? "폴백3D" : "Humanoid/Prefab";
        GUI.Label(new Rect(14, 10, 980, 24),
            $"SoulArena [{mode}] | P {hp} | B {bossHp} | WASD Shift E 클릭 Esc R | P일시정지 F1디버그", hudStyle);
        GUI.Label(new Rect(14, 32, 980, 24),
            "바닥 경고 없음 · 휠 줌 · 잡몹은 단순 접촉 · 진입: 로비F6 / One-Click", hudStyle);

        DrawBar(14f, Screen.height - 36f, 220f, 16f,
            player != null ? player.Health / Mathf.Max(1f, player.MaxHealth) : 0f,
            new Color(0.2f, 0.75f, 1f), "P");
        DrawBar(Screen.width - 254f, Screen.height - 36f, 240f, 16f,
            bossHealth != null ? bossHealth.Health / Mathf.Max(1f, bossHealth.MaxHealth) : 0f,
            new Color(1f, 0.35f, 0.25f), "B");

        if (player != null && player.IsDashing)
            GUI.Label(new Rect(14, Screen.height - 58f, 200, 20), "DASH", hudStyle);
        else if (player != null && player.IsParrying)
            GUI.Label(new Rect(14, Screen.height - 58f, 200, 20), "PARRY", hudStyle);
    }

    private static void DrawBar(float x, float y, float w, float h, float fill, Color color, string label)
    {
        fill = Mathf.Clamp01(fill);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(x + 1f, y + 1f, (w - 2f) * fill, h - 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 4f, y - 2f, 40f, h + 4f), label);
    }
}
