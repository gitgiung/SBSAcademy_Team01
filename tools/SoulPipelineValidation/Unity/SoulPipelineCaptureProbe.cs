using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>검증 복사본에만 복사한다. 메인 스프라이트 적용 전/후 화면을 PNG로 남긴다.</summary>
public sealed class SoulPipelineCaptureProbe : MonoBehaviour
{
    public const string OutDir = @"C:\sv\capture";
    public const string ResultPath = @"C:\sv\soul-capture.txt";
    const int Tile = 256;

    IEnumerator Start()
    {
        Directory.CreateDirectory(OutDir);
        var log = new StringBuilder();

        // Awake 전에 '적용 전' 옵션을 넣으려고 비활성 상태에서 붙인 뒤 켠다.
        var root = new GameObject("capture-bootstrap");
        root.SetActive(false);
        var boot = root.AddComponent<SoulArenaBootstrap>();
        SetField(boot, "useMainPlayerSprite", false);
        SetField(boot, "useMainBossSprite", false);
        SetField(boot, "useMainZombieSprite", false);
        root.SetActive(true);
        var director = root.GetComponent<SoulArenaWaveDirector>();
        if (director != null) SetField(director, "prepSeconds", 999f);
        yield return null;
        yield return null;

        var player = Object.FindAnyObjectByType<SoulPipelineAvatar>();
        var boss = Object.FindAnyObjectByType<SoulBossApproachSlash>();
        Camera cam = Camera.main;
        if (player == null || boss == null || cam == null)
        {
            File.WriteAllText(ResultPath, "FAIL 씬 구성 실패 player=" + (player != null) + " boss=" + (boss != null) + " cam=" + (cam != null));
            yield break;
        }

        var rig = cam.GetComponent<SoulCameraRig>();
        if (rig != null) rig.enabled = false;
        player.enabled = false;
        player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(Vector3.back));
        boss.transform.SetPositionAndRotation(new Vector3(3f, 0f, 1.5f), Quaternion.LookRotation(new Vector3(-3f, 0f, -1.5f)));
        foreach (var trash in Object.FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Exclude))
            trash.enabled = false;
        yield return WaitSeconds(0.4f);

        Vector3 wideFocus = new Vector3(1.5f, 0f, 0.75f);
        SavePng(RenderView(cam, wideFocus, SoulRenderSpec.CameraOrthoSize, 1280, 720), "wide-before.png", log);

        var sheet = new Texture2D(Tile * 4, Tile * 3, TextureFormat.RGB24, false);
        Vector3[] dirs = { Vector3.back, Vector3.right, Vector3.forward, Vector3.left };
        for (int i = 0; i < dirs.Length; i++)
        {
            player.transform.rotation = Quaternion.LookRotation(dirs[i]);
            yield return WaitSeconds(0.25f);
            Blit(sheet, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), i, 2);
        }

        var skin = SoulPlayerSpriteSkin.Apply(player);
        var bossSkin = SoulBossSpriteSkin.Apply(boss);
        SoulTrashMobSpriteSkin.UseMainZombieSprite = true;
        int zombies = 0;
        foreach (var trash in Object.FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Exclude))
            if (SoulTrashMobSpriteSkin.Apply(trash) != null) zombies++;
        log.AppendLine("zombie skins=" + zombies);
        if (skin == null || bossSkin == null)
        {
            File.WriteAllText(ResultPath, "FAIL 스킨 적용 실패 player=" + (skin != null) + " boss=" + (bossSkin != null));
            yield break;
        }

        for (int i = 0; i < dirs.Length; i++)
        {
            player.transform.rotation = Quaternion.LookRotation(dirs[i]);
            yield return WaitSeconds(0.3f);
            Blit(sheet, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), i, 1);
            log.AppendLine("after dir" + i + " facing=" + skin.Facing.ToString("0") + " sprite=" + SpriteName(skin));
        }

        player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        yield return WaitSeconds(0.3f);
        SavePng(RenderView(cam, wideFocus, SoulRenderSpec.CameraOrthoSize, 1280, 720), "wide-after.png", log);

        player.transform.rotation = Quaternion.LookRotation(Vector3.right);
        SetProperty(player, "IsMoving", true);
        yield return WaitSeconds(0.35f);
        Blit(sheet, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 0, 0);
        log.AppendLine("move " + StateName(skin) + " sprite=" + SpriteName(skin));
        SetProperty(player, "IsMoving", false);

        SetField(player, "dashRemaining", 0.5f);
        yield return WaitSeconds(0.12f);
        Blit(sheet, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 1, 0);
        log.AppendLine("dash " + StateName(skin) + " sprite=" + SpriteName(skin));
        SetField(player, "dashRemaining", 0f);
        yield return WaitSeconds(0.4f);

        player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        SetField(player, "parryRemaining", 0.13f);
        yield return WaitSeconds(0.12f);
        Blit(sheet, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 2, 0);
        log.AppendLine("parry " + StateName(skin) + " sprite=" + SpriteName(skin));
        SetField(player, "parryRemaining", 0f);
        yield return WaitSeconds(0.6f);

        Invoke(player, "TryLightSwing");
        yield return WaitSeconds(0.12f);
        Blit(sheet, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 3, 0);
        log.AppendLine("attack " + StateName(skin) + " sprite=" + SpriteName(skin));

        sheet.Apply();
        SavePng(sheet, "sheet.png", log);

        // 같은 장면을 게임 카메라 리그로: 기본 직교 → F2 원근 → 원근 회전.
        SetField(player, "attackLock", 0f);
        player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        if (rig != null)
        {
            rig.enabled = true;
            yield return WaitSeconds(0.3f);
            SavePng(RenderCurrent(cam, 1280, 720), "view-2d.png", log);
            rig.SetDepthView(true);
            yield return WaitSeconds(0.2f);
            SavePng(RenderCurrent(cam, 1280, 720), "view-3d.png", log);
            rig.OrbitDepthView(55f);
            yield return WaitSeconds(0.2f);
            SavePng(RenderCurrent(cam, 1280, 720), "view-3d-orbit.png", log);
            log.AppendLine("depth view yaw=" + SoulCameraRig.ViewYaw.ToString("0") + " ortho=" + cam.orthographic);
            rig.SetDepthView(false);
        }

        yield return CaptureBossStrip(cam, rig, player, boss, bossSkin, log);

        // 잡몹 좀비가 실제 AI로 플레이어에게 몰려오는 장면(보스는 멀리 치움).
        boss.enabled = false;
        boss.transform.position = new Vector3(14f, 0f, 14f);
        player.transform.position = Vector3.zero;
        var mobs = Object.FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Exclude);
        for (int i = 0; i < mobs.Length; i++)
        {
            float ang = i * Mathf.PI * 2f / Mathf.Max(1, mobs.Length);
            mobs[i].transform.position = new Vector3(Mathf.Cos(ang) * 6.5f, 0f, Mathf.Sin(ang) * 6.5f);
            mobs[i].enabled = true;
        }
        Time.captureFramerate = 60;
        yield return null;
        SavePng(RenderView(cam, Vector3.up * 0.8f, 5.5f, 1280, 720), "zombies-start.png", log);
        for (int i = 0; i < 75; i++) yield return null;
        SavePng(RenderView(cam, Vector3.up * 0.8f, 5.5f, 1280, 720), "zombies-chase.png", log);
        Time.captureFramerate = 0;

        // 잡몹을 끄면 웨이브가 끝난 것으로 보고 보스가 들어오므로 웨이브를 멈추고 멀리 치운다.
        if (director != null) director.enabled = false;
        boss.gameObject.SetActive(false);
        foreach (var mob in mobs)
        {
            mob.enabled = false;
            mob.transform.position = new Vector3(9f, 0f, 9f);
        }
        yield return CaptureRigInGame(cam, player, log);
        yield return CaptureBoneRig(cam, player, log);
        File.WriteAllText(ResultPath, "PASS\n" + log);
    }

    // 본게임 연결: 정면 대기/걷기/공격(뼈대) 4장 + 오른쪽·위쪽(원본 프레임) 2장을 한 줄로.
    IEnumerator CaptureRigInGame(Camera cam, SoulPipelineAvatar player, StringBuilder log)
    {
        var skin = SoulPlayerSpriteSkin.Apply(player);
        var rigSkin = skin != null ? SoulPlayerBoneRigSkin.Apply(skin) : null;
        if (rigSkin == null)
        {
            log.AppendLine("FAIL in-game rig skin not applied");
            yield break;
        }

        player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(Vector3.back));
        SetField(player, "attackLock", 0f);
        Time.captureFramerate = 60;
        var row = new Texture2D(Tile * 7, Tile, TextureFormat.RGB24, false);
        yield return WaitSeconds(0.3f);
        Blit(row, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 0, 0);
        log.AppendLine("ingame idle active=" + rigSkin.Active + " motion=" + rigSkin.Rig.CurrentMotion);

        SetProperty(player, "IsMoving", true);
        yield return WaitSeconds(0.2f);
        Blit(row, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 1, 0);
        log.AppendLine("ingame walk motion=" + rigSkin.Rig.CurrentMotion);
        SetProperty(player, "IsMoving", false);
        yield return WaitSeconds(0.3f);

        Invoke(player, "TryLightSwing");
        for (int i = 0; i < 3; i++)
        {
            yield return WaitSeconds(i == 0 ? 0.12f : 0.1f);
            Blit(row, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 2 + i, 0);
            log.AppendLine("ingame attack " + i + " motion=" + rigSkin.Rig.CurrentMotion);
        }
        yield return WaitSeconds(0.8f);

        player.transform.rotation = Quaternion.LookRotation(Vector3.right);
        yield return WaitSeconds(0.3f);
        Blit(row, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 5, 0);
        log.AppendLine("ingame east active=" + rigSkin.Active);
        player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        yield return WaitSeconds(0.3f);
        Blit(row, RenderView(cam, CloseFocus(player), 1.35f, Tile, Tile), 6, 0);
        log.AppendLine("ingame north active=" + rigSkin.Active);
        row.Apply();
        SavePng(row, "rig-ingame.png", log);

        // 정면 걷기 한 주기(0.7초)를 8프레임으로: 위 = 뼈대, 아래 = 원본 프레임 애니메이션.
        player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        SetProperty(player, "IsMoving", true);
        var walk = new Texture2D(Tile * 8, Tile * 2, TextureFormat.RGB24, false);
        for (int pass = 0; pass < 2; pass++)
        {
            rigSkin.UserEnabled = pass == 0;
            yield return WaitSeconds(0.3f);
            for (int c = 0; c < 8; c++)
            {
                Blit(walk, RenderView(cam, CloseFocus(player), 1.0f, Tile, Tile), c, 1 - pass);
                yield return WaitSeconds(0.7f / 8f);
            }
        }
        rigSkin.UserEnabled = true;
        SetProperty(player, "IsMoving", false);
        walk.Apply();
        SavePng(walk, "rig-walk-strip.png", log);
        Object.Destroy(rigSkin);
        player.transform.rotation = Quaternion.LookRotation(Vector3.back);
        Time.captureFramerate = 0;
        yield return null;
    }
    // 왼쪽 원본 프레임 / 오른쪽 뼈대 리깅을 대기·걷기·공격 행으로, 그리고 뼈대·가중치 오버레이를 찍는다.
    IEnumerator CaptureBoneRig(Camera cam, SoulPipelineAvatar player, StringBuilder log)
    {
        const int w = 384, h = 256, cols = 6;
        player.transform.position = new Vector3(-6f, 0f, -5f);
        foreach (var r in player.GetComponentsInChildren<Renderer>()) r.enabled = false;
        yield return null;
        Vector3 spot = player.transform.position;
        log.AppendLine("rig spot=" + spot.ToString("0.0"));
        var preview = new GameObject("capture-rig-preview").AddComponent<SoulBoneRigPreview>();
        preview.SideOffset = 0.7f;
        preview.SetOpen(true);
        if (!preview.IsOpen)
        {
            log.AppendLine("FAIL bone rig preview not opened");
            yield break;
        }

        Time.captureFramerate = 60;
        var strip = new Texture2D(w * cols, h * 3, TextureFormat.RGB24, false);
        Vector3 focus = spot + Vector3.up * 0.95f;
        var motions = new[] { SoulPlayerBoneRig.Motion.Idle, SoulPlayerBoneRig.Motion.Walk, SoulPlayerBoneRig.Motion.Attack };
        for (int m = 0; m < motions.Length; m++)
        {
            preview.SetMotion(motions[m]);
            for (int c = 0; c < cols; c++)
            {
                yield return null;
                var tile = RenderView(cam, focus, 1.5f, w, h);
                strip.SetPixels(c * w, (2 - m) * h, w, h, tile.GetPixels());
                Object.Destroy(tile);
                for (int f = 0; f < 10; f++) yield return null;
            }
        }
        strip.Apply();
        SavePng(strip, "rig-compare.png", log);

        // 공격 최대 순간 확대: 왼쪽 원본(스프라이트 지팡이) / 오른쪽 뼈대+3D 지팡이.
        preview.SetMotion(SoulPlayerBoneRig.Motion.Attack);
        for (int f = 0; f < 24; f++) yield return null;
        SavePng(RenderView(cam, focus, 1.15f, 960, 600), "rig-attack-zoom.png", log);

        // 뼈대판만 확대해 부착 지팡이가 손을 따라가는지 찍는다(원본 프레임은 잠시 숨김).
        if (preview.OriginalAnimator != null)
            preview.OriginalAnimator.transform.parent.gameObject.SetActive(false);
        var attachStrip = new Texture2D(w * 4, h, TextureFormat.RGB24, false);
        preview.SetMotion(SoulPlayerBoneRig.Motion.Attack);
        Vector3 wandFocus = preview.Rig.transform.position + Vector3.up * 0.95f;
        for (int c = 0; c < 4; c++)
        {
            for (int f = 0; f < 12; f++) yield return null;
            var tile = RenderView(cam, wandFocus, 0.95f, w, h);
            attachStrip.SetPixels(c * w, 0, w, h, tile.GetPixels());
            Object.Destroy(tile);
        }
        attachStrip.Apply();
        SavePng(attachStrip, "rig-wand-attach.png", log);
        log.AppendLine("wand erased=" + preview.Rig.ErasedWandPixels + " tip=" + (preview.Rig.WandTip != null));
        // 대기 한 장: 스프라이트 지팡이 없음 + 3D 지팡이만.
        preview.SetMotion(SoulPlayerBoneRig.Motion.Idle);
        yield return null;
        yield return null;
        SavePng(RenderView(cam, wandFocus, 0.9f, 640, 640), "rig-wand-idle.png", log);

        // 기본 자세에 부위별 색과 뼈대 선을 덧그린다.
        var rig = preview.Rig;
        rig.enabled = false;
        rig.ResetPose();
        Color[] palette =
        {
            Color.white, new Color(1f, 0.85f, 0.3f), new Color(1f, 0.55f, 0.2f), new Color(0.4f, 0.8f, 1f), new Color(0.6f, 0.6f, 1f),
            new Color(0.4f, 1f, 0.5f), new Color(0.2f, 0.8f, 0.4f), new Color(1f, 0.4f, 0.8f), new Color(0.8f, 0.3f, 1f),
            new Color(1f, 1f, 0.5f), new Color(0.9f, 0.9f, 0.2f),
        };
        Mesh mesh = rig.SourceMesh;
        var perVertex = mesh.GetBonesPerVertex();
        var weights = mesh.GetAllBoneWeights();
        var colors = new Color[mesh.vertexCount];
        int wi = 0;
        for (int v = 0; v < colors.Length; v++)
        {
            Color c = Color.black;
            for (int k = 0; k < perVertex[v]; k++, wi++) c += palette[weights[wi].boneIndex] * weights[wi].weight;
            c.a = 1f;
            colors[v] = c;
        }
        Color[] keepColors = mesh.colors;
        mesh.colors = colors;
        rig.transform.rotation = SoulRenderSpec.CameraRotation;
        var lineMat = new Material(Shader.Find("Sprites/Default"));
        Vector3 toCam = -(SoulRenderSpec.CameraRotation * Vector3.forward) * 0.2f;
        for (int i = 1; i < rig.Bones.Length; i++)
        {
            var lr = new GameObject("bone-line").AddComponent<LineRenderer>();
            lr.sharedMaterial = lineMat;
            lr.startWidth = 0.035f;
            lr.endWidth = 0.012f;
            lr.startColor = lr.endColor = Color.black;
            Transform bone = rig.Bones[i];
            Vector3 tip = bone.childCount > 0 ? bone.GetChild(0).position : bone.position + bone.rotation * Vector3.down * 0.25f;
            if (bone.name.Contains("Hair")) tip = bone.position + (bone.position - rig.Bones[3].position).normalized * 0.05f + bone.rotation * Vector3.down * 0.6f;
            if (bone.name.Contains("Arm")) tip = bone.position + bone.rotation * new Vector3(bone.name.EndsWith("L") ? -0.18f : 0.18f, -0.3f, 0f);
            if (bone.name.Contains("Leg")) tip = bone.position + bone.rotation * Vector3.down * 0.3f;
            lr.positionCount = 2;
            lr.SetPositions(new[] { bone.position + toCam, tip + toCam });
            lr.transform.SetParent(rig.transform, true);
        }
        yield return null;
        SavePng(RenderView(cam, rig.transform.position + Vector3.up * 0.95f, 1.1f, 640, 640), "rig-weights.png", log);
        mesh.colors = keepColors;
        Time.captureFramerate = 0;
        log.AppendLine("bone rig bones=" + rig.Bones.Length + " vertices=" + mesh.vertexCount);
    }

    // 실제 보스 AI를 돌리며 추격 → 윈드업(틴트) → 타격(손 히트박스 무장) → 회복을 한 줄로 찍는다.
    IEnumerator CaptureBossStrip(Camera cam, SoulCameraRig rig, SoulPipelineAvatar player, SoulBossApproachSlash boss,
        SoulBossSpriteSkin bossSkin, StringBuilder log)
    {
        const int bossTile = 384;
        if (rig != null) rig.enabled = false;
        Time.captureFramerate = 60;
        player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(Vector3.right));
        boss.transform.position = new Vector3(6f, 0f, 0f);
        var hitbox = boss.GetComponentInChildren<SoulWeaponHitbox3D>(true);
        boss.enabled = true;

        var strip = new Texture2D(bossTile * 4, bossTile, TextureFormat.RGB24, false);
        bool[] done = new bool[4];
        string[] labels = { "chase", "windup", "impact", "recovery" };
        int frames = 0;
        int recoveryFrames = 0;
        while (frames < 900 && !(done[0] && done[1] && done[2] && done[3]))
        {
            yield return null;
            frames++;
            int slot = -1;
            if (!done[0] && !boss.IsAttacking && !boss.IsRecovering && bossSkin.BodyAnimator.GetCurrentAnimatorStateInfo(0).IsName("BossWalk")
                && Vector3.Distance(boss.transform.position, player.transform.position) < 4.2f) slot = 0;
            else if (!done[1] && boss.IsWindup && boss.AttackNormalizedTime > 0.2f) slot = 1;
            else if (!done[2] && hitbox != null && hitbox.IsArmed) slot = 2;
            else if (!done[3] && done[2] && boss.IsRecovering && ++recoveryFrames >= 15) slot = 3;
            if (slot < 0) continue;

            done[slot] = true;
            Vector3 mid = (boss.transform.position + player.transform.position) * 0.5f + Vector3.up * 2.3f;
            var tile = RenderView(cam, mid, 4.3f, bossTile, bossTile);
            strip.SetPixels(slot * bossTile, 0, bossTile, bossTile, tile.GetPixels());
            Object.Destroy(tile);
            var info = bossSkin.BodyAnimator.GetCurrentAnimatorStateInfo(0);
            AnimationClip main = null;
            float bestWeight = -1f;
            foreach (var c in bossSkin.BodyAnimator.GetCurrentAnimatorClipInfo(0))
                if (c.weight > bestWeight) { bestWeight = c.weight; main = c.clip; }
            float clipSec = main != null ? (info.normalizedTime % 1f) * main.length : 0f;
            log.AppendLine("boss " + labels[slot] + " frame=" + frames + " sprite=" + bossSkin.BodyRenderer.sprite.name
                + " clip=" + (main != null ? main.name : "-") + " t=" + clipSec.ToString("0.00")
                + " facing=" + bossSkin.Facing.ToString("0")
                + " tintB=" + bossSkin.BodyRenderer.material.color.b.ToString("0.00"));
        }

        Time.captureFramerate = 0;
        strip.Apply();
        SavePng(strip, "boss-strip.png", log);
    }

    static Vector3 CloseFocus(SoulPipelineAvatar player) => player.transform.position + Vector3.up * 0.9f;

    static Texture2D RenderView(Camera cam, Vector3 focus, float orthoSize, int w, int h)
    {
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.transform.SetPositionAndRotation(focus + SoulRenderSpec.CameraOffsetFromFocus, SoulRenderSpec.CameraRotation);
        var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 24) { sRGB = true };
        var rt = RenderTexture.GetTemporary(desc);
        var prev = cam.targetTexture;
        float prevAspect = cam.aspect;
        cam.targetTexture = rt;
        cam.aspect = (float)w / h;
        cam.Render();
        cam.targetTexture = prev;
        cam.aspect = prevAspect;
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }

    static Texture2D RenderCurrent(Camera cam, int w, int h)
    {
        var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 24) { sRGB = true };
        var rt = RenderTexture.GetTemporary(desc);
        var prev = cam.targetTexture;
        float prevAspect = cam.aspect;
        cam.targetTexture = rt;
        cam.aspect = (float)w / h;
        cam.Render();
        cam.targetTexture = prev;
        cam.aspect = prevAspect;
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }

    static void Blit(Texture2D sheet, Texture2D tile, int column, int row)
    {
        sheet.SetPixels(column * Tile, row * Tile, Tile, Tile, tile.GetPixels());
        Object.Destroy(tile);
    }

    static void SavePng(Texture2D tex, string file, StringBuilder log)
    {
        string path = Path.Combine(OutDir, file);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        log.AppendLine("saved " + path);
    }

    static string SpriteName(SoulPlayerSpriteSkin skin) =>
        skin.BodyRenderer.sprite != null ? skin.BodyRenderer.sprite.name : "null";

    static string StateName(SoulPlayerSpriteSkin skin)
    {
        var info = skin.BodyAnimator.GetCurrentAnimatorStateInfo(0);
        foreach (string s in new[] { "Idle", "Move", "Charge", "Parry", "LightAttack", "Guard" })
            if (info.IsName(s)) return s;
        return "?";
    }

    static IEnumerator WaitSeconds(float seconds)
    {
        float t = 0f;
        int frames = 0;
        while (t < seconds && frames < 5000)
        {
            yield return null;
            t += Time.deltaTime;
            frames++;
        }
    }

    static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    static void SetProperty(object target, string name, object value) =>
        target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public).SetValue(target, value);

    static void Invoke(object target, string name) =>
        target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
