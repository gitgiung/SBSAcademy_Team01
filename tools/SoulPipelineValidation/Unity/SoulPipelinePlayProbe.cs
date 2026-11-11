using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>검증 복사본에만 복사한다. 폴백/프리팹 공방 루프를 파일로 남긴다.</summary>
public sealed class SoulPipelinePlayProbe : MonoBehaviour
{
    const string ResultPath = @"C:\sv\soul-probe.txt";

    IEnumerator Start()
    {
        var report = new StringBuilder();
        int failed = 0;
        int eventErrors = 0;
        Application.LogCallback onLog = (condition, stack, type) =>
        {
            if (type != LogType.Log && type != LogType.Warning && condition.Contains("AnimationEvent")) eventErrors++;
        };
        Application.logMessageReceived += onLog;
        yield return CheckFallbackArena(report, () => failed++);
        yield return CheckParry(report, () => failed++);
        yield return CheckHitOnce(report, () => failed++);
        yield return CheckRecoveryAndDeath(report, () => failed++);
        yield return CheckMainSpriteSkin(report, () => failed++);
        yield return CheckDepthView(report, () => failed++);
        yield return CheckBossSpriteSkin(report, () => failed++);
        yield return CheckTrashMobSpriteSkin(report, () => failed++);
        yield return CheckBoneRigPrototype(report, () => failed++);
        yield return CheckBoneRigPlayerSkin(report, () => failed++);
        Application.logMessageReceived -= onLog;
        if (eventErrors > 0)
        {
            failed++;
            report.AppendLine("FAIL 애니메이션 이벤트 수신 오류 " + eventErrors + "건");
        }
        else
            report.AppendLine("PASS 보스 클립 애니메이션 이벤트 수신 오류 0건");
        report.Insert(0, failed == 0 ? "PASS\n" : "FAIL " + failed + "\n");
        File.WriteAllText(ResultPath, report.ToString());
        Debug.Log(report.ToString());
    }

    IEnumerator CheckFallbackArena(StringBuilder report, System.Action fail)
    {
        var root = new GameObject("verify-bootstrap");
        root.AddComponent<SoulArenaBootstrap>();
        var director = root.GetComponent<SoulArenaWaveDirector>();
        if (director == null)
        {
            fail();
            report.AppendLine("FAIL 웨이브 디렉터가 없다.");
            yield break;
        }

        SetField(director, "prepSeconds", 0.2f);
        yield return null;

        var player = Object.FindAnyObjectByType<SoulPipelineAvatar>();
        var boss = Object.FindAnyObjectByType<SoulBossApproachSlash>();
        var hitbox = boss != null ? boss.GetComponentInChildren<SoulWeaponHitbox3D>(true) : null;
        if (player == null || boss == null || hitbox == null || boss.enabled)
        {
            fail();
            report.AppendLine("FAIL 폴백/프리팹 생성 또는 이완 정지. player=" + (player != null) + " boss=" + (boss != null) + " enabled=" + (boss != null && boss.enabled));
            yield break;
        }

        string mode = player.name.Contains("Fallback") ? "폴백3D" : "Humanoid/Prefab";
        var bootSkin = player.GetComponent<SoulPlayerSpriteSkin>();
        if (bootSkin == null || !bootSkin.Applied)
        {
            fail();
            report.AppendLine("FAIL 부트스트랩이 메인 스프라이트를 입히지 않았다.");
        }
        else
            mode += "+메인스프라이트";
        var bossSkinAtBoot = boss.GetComponent<SoulBossSpriteSkin>();
        if (bossSkinAtBoot == null || !bossSkinAtBoot.Applied)
        {
            fail();
            report.AppendLine("FAIL 부트스트랩이 보스 스프라이트를 입히지 않았다.");
        }
        else
            mode += "+보스스프라이트";
        var trashAtBoot = Object.FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Exclude);
        int skinnedTrash = 0;
        foreach (var mob in trashAtBoot)
            if (mob.GetComponent<SoulTrashMobSpriteSkin>() != null) skinnedTrash++;
        if (trashAtBoot.Length == 0 || skinnedTrash != trashAtBoot.Length)
        {
            fail();
            report.AppendLine("FAIL 부트스트랩 잡몹 좀비 스프라이트 " + skinnedTrash + "/" + trashAtBoot.Length);
        }
        else
            mode += "+좀비스프라이트" + skinnedTrash;
        // 거리 단축으로 추격→윈드업을 빠르게 확인.
        player.transform.position = new Vector3(0f, 0f, 0f);
        // 개시거리(2.85)보다 살짝 멀리 두어 추격 후 윈드업 접근이 보이게 한다.
        boss.transform.position = new Vector3(3.2f, 0f, 0f);

        float elapsed = 0f;
        int frames = 0;
        while (!boss.enabled && elapsed < 2f && frames < 600)
        {
            yield return null;
            elapsed += Time.deltaTime;
            frames++;
        }

        if (!boss.enabled)
        {
            fail();
            report.AppendLine("FAIL 보스가 켜지지 않았다. t=" + elapsed);
            yield break;
        }

        Vector3 releasePos = boss.transform.position;
        bool sawWindup = false;
        bool armedDuringWindup = false;
        bool sawArmedAttack = false;
        float windupTravel = 0f;
        while (elapsed < 12f && frames < 20000 && !sawArmedAttack)
        {
            yield return null;
            elapsed += Time.deltaTime;
            frames++;
            if (boss.IsWindup)
            {
                windupTravel = Mathf.Max(windupTravel, Vector3.Distance(Flat(boss.transform.position), Flat(releasePos)));
                sawWindup = true;
                if (hitbox.IsArmed) armedDuringWindup = true;
            }

            if (boss.IsAttacking && !boss.IsWindup && hitbox.IsArmed)
                sawArmedAttack = true;
        }

        if (!sawWindup || windupTravel <= 0.05f || armedDuringWindup || !sawArmedAttack)
        {
            fail();
            report.AppendLine("FAIL 접근 슬래시[" + mode + "]. windup=" + sawWindup + " windupTravel=" + windupTravel.ToString("0.00") + " armedInWindup=" + armedDuringWindup + " armedAttack=" + sawArmedAttack + " hand=" + hitbox.transform.name);
        }
        else
            report.AppendLine("PASS 접근 슬래시[" + mode + "] hand=" + hitbox.transform.name + " travel=" + windupTravel.ToString("0.00"));

        Destroy(root);
        if (player != null) Destroy(player.gameObject);
        if (boss != null) Destroy(boss.gameObject);
        foreach (var trash in Object.FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Exclude))
            Destroy(trash.gameObject);
        yield return null;
    }

    IEnumerator CheckParry(StringBuilder report, System.Action fail)
    {
        var playerGo = new GameObject("player");
        var player = playerGo.AddComponent<SoulPipelineAvatar>();
        var bossGo = new GameObject("boss");
        var hand = new GameObject("hand");
        hand.transform.SetParent(bossGo.transform, false);
        var hitbox = hand.AddComponent<SoulWeaponHitbox3D>();
        var boss = bossGo.AddComponent<SoulBossApproachSlash>();
        boss.Bind(playerGo.transform, hitbox);
        yield return null;

        SetField(player, "parryRemaining", 0.2f);
        float before = player.Health;
        hitbox.SetArmed(true);
        player.TakeContactDamage(18f, bossGo);
        if (!Mathf.Approximately(before, player.Health) || !boss.IsRecovering || hitbox.IsArmed || player.IsParrying)
        {
            fail();
            report.AppendLine("FAIL 패링. hp=" + player.Health + " recovery=" + boss.IsRecovering + " armed=" + hitbox.IsArmed);
        }
        else
            report.AppendLine("PASS 패링이 체력을 지키고 보스를 리커버리로 보냈다.");

        Destroy(playerGo);
        Destroy(bossGo);
        yield return null;
    }

    IEnumerator CheckHitOnce(StringBuilder report, System.Action fail)
    {
        var playerGo = new GameObject("player");
        var player = playerGo.AddComponent<SoulPipelineAvatar>();
        var bossGo = new GameObject("boss");
        var hand = new GameObject("hand");
        hand.transform.SetParent(bossGo.transform, false);
        var hitbox = hand.AddComponent<SoulWeaponHitbox3D>();
        hitbox.Configure(18f, bossGo);
        yield return null;

        var collider = playerGo.GetComponent<Collider>();
        float before = player.Health;
        hitbox.TryHitCollider(collider);
        if (!Mathf.Approximately(before, player.Health))
        {
            fail();
            report.AppendLine("FAIL 비무장 피해. before=" + before + " after=" + player.Health);
            Destroy(playerGo);
            Destroy(bossGo);
            yield break;
        }

        hitbox.SetArmed(true);
        hitbox.TryHitCollider(collider);
        float once = player.Health;
        hitbox.TryHitCollider(collider);
        if (!Mathf.Approximately(before - 18f, once) || !Mathf.Approximately(once, player.Health))
        {
            fail();
            report.AppendLine("FAIL 히트 1회. before=" + before + " once=" + once + " after=" + player.Health);
        }
        else
            report.AppendLine("PASS 비무장은 피해가 없고 같은 대상은 한 번만 맞는다.");

        Destroy(playerGo);
        Destroy(bossGo);
        yield return null;
    }

    IEnumerator CheckRecoveryAndDeath(StringBuilder report, System.Action fail)
    {
        var playerGo = new GameObject("player");
        playerGo.transform.position = new Vector3(0f, 0f, 0.6f);
        var player = playerGo.AddComponent<SoulPipelineAvatar>();
        var bossGo = new GameObject("boss");
        var health = bossGo.AddComponent<SoulArenaDummyHealth>();
        var hand = new GameObject("hand");
        hand.transform.SetParent(bossGo.transform, false);
        var hitbox = hand.AddComponent<SoulWeaponHitbox3D>();
        var boss = bossGo.AddComponent<SoulBossApproachSlash>();
        boss.Bind(playerGo.transform, hitbox);
        yield return null;

        boss.StunFromParry();
        Invoke(player, "TryLightSwing");
        bool bonus = Mathf.Approximately(health.Health, 182f);
        health.TakeDamage(1000f);
        if (!bonus || !health.IsDead || boss.enabled || hitbox.IsArmed)
        {
            fail();
            report.AppendLine("FAIL 리커버리/사망. hp=" + health.Health + " dead=" + health.IsDead + " ai=" + boss.enabled + " armed=" + hitbox.IsArmed);
        }
        else
            report.AppendLine("PASS 리커버리 추가타 18, 사망 시 AI와 히트박스가 멈춘다.");

        Destroy(playerGo);
        Destroy(bossGo);
        yield return null;
    }

    IEnumerator CheckMainSpriteSkin(StringBuilder report, System.Action fail)
    {
        var camGo = new GameObject("probe-cam");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        camGo.transform.rotation = SoulRenderSpec.CameraRotation;

        var playerGo = new GameObject("player");
        var mannequin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(mannequin.GetComponent<Collider>());
        mannequin.transform.SetParent(playerGo.transform, false);
        var player = playerGo.AddComponent<SoulPipelineAvatar>();
        player.enabled = false;
        var skin = SoulPlayerSpriteSkin.Apply(player);
        yield return null;
        yield return null;

        if (skin == null || skin.BodyRenderer == null || skin.BodyAnimator == null)
        {
            fail();
            report.AppendLine("FAIL 메인 스프라이트 스킨 생성 실패 (Resources/" + SoulPlayerSpriteSkin.ControllerPath + ").");
            Destroy(playerGo);
            Destroy(camGo);
            yield break;
        }

        var body = skin.BodyRenderer;
        var anim = skin.BodyAnimator;
        var lines = new StringBuilder();
        int bad = 0;
        void Expect(bool ok, string what)
        {
            if (!ok) bad++;
            lines.AppendLine("    " + (ok ? "ok  " : "NG  ") + what);
        }

        Expect(!mannequin.GetComponent<Renderer>().enabled, "3D 몸 렌더러 숨김");
        Expect(anim.runtimeAnimatorController.name == "Player", "컨트롤러=" + anim.runtimeAnimatorController.name + " (MainScene Player와 동일)");
        Expect(Mathf.Approximately(body.transform.lossyScale.y, SoulPlayerSpriteSkin.MainPlayerScale), "스케일=" + body.transform.lossyScale.y.ToString("0.##") + " (MainScene Player 3)");
        Expect(Quaternion.Angle(body.transform.rotation, cam.transform.rotation) < 0.5f, "빌보드=카메라 회전 일치");
        Vector3 bottom = body.transform.position + body.transform.up * (body.sprite.bounds.min.y * body.transform.lossyScale.y);
        float feet = bottom.y - playerGo.transform.position.y;
        Expect(Mathf.Abs(feet) < 0.05f, "스프라이트 하단(루트 기준)=" + feet.ToString("0.00") + " 본체 높이=" + body.transform.position.y.ToString("0.00"));
        Expect(body.sprite != null, "첫 스프라이트=" + (body.sprite != null ? body.sprite.name : "null"));

        Vector3[] worldDirs = { Vector3.back, Vector3.right, Vector3.forward, Vector3.left };
        Vector2[] expected = { Vector2.down, Vector2.right, Vector2.up, Vector2.left };
        string[] labels = { "화면 아래(-Z)", "화면 오른쪽(+X)", "화면 위(+Z)", "화면 왼쪽(-X)" };
        var spriteNames = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < worldDirs.Length; i++)
        {
            playerGo.transform.rotation = Quaternion.LookRotation(worldDirs[i]);
            yield return WaitSeconds(0.25f);
            string spriteName = body.sprite != null ? body.sprite.name : "null";
            spriteNames.Add(spriteName);
            Expect(Vector2.Distance(skin.Facing, expected[i]) < 0.01f,
                labels[i] + " → Facing" + skin.Facing.ToString("0") + " sprite=" + spriteName);
        }
        Expect(spriteNames.Count == 4, "방향별 서로 다른 스프라이트 " + spriteNames.Count + "/4");

        SetProperty(player, "IsMoving", true);
        yield return WaitSeconds(0.3f);
        Expect(anim.GetCurrentAnimatorStateInfo(0).IsName("Move"), "IsMoving=true → Move 상태, sprite=" + body.sprite.name);
        SetProperty(player, "IsMoving", false);
        yield return WaitSeconds(0.3f);
        Expect(anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "IsMoving=false → Idle 상태");

        SetField(player, "dashRemaining", 0.5f);
        yield return WaitSeconds(0.1f);
        Expect(anim.GetCurrentAnimatorStateInfo(0).IsName("Charge"), "대시 → Charge 상태");
        var ghost = GameObject.Find("DashGhost");
        Expect(ghost != null && ghost.GetComponent<SpriteRenderer>() != null, "대시 잔상=스프라이트 (캡슐 아님)");
        SetField(player, "dashRemaining", 0f);
        yield return WaitSeconds(0.3f);

        SetField(player, "parryRemaining", 0.13f);
        yield return WaitSeconds(0.1f);
        Expect(anim.GetCurrentAnimatorStateInfo(0).IsName("Parry"), "패링 입력 → Parry 상태");
        SetField(player, "parryRemaining", 0f);
        yield return WaitSeconds(0.6f);

        Invoke(player, "TryLightSwing");
        yield return null;
        Expect(anim.GetCurrentAnimatorStateInfo(0).IsName("LightAttack"), "공격(J/클릭) → LightAttack 상태");

        player.GetComponent<SoulPlayerParryFlash>().HurtFlash();
        yield return null;
        Expect(body.material.color.g < 0.9f, "피격 틴트가 스프라이트에 적용 g=" + body.material.color.g.ToString("0.00"));

        if (bad > 0)
        {
            fail();
            report.AppendLine("FAIL 메인 스프라이트 스킨 " + bad + "건");
        }
        else
            report.AppendLine("PASS 메인 플레이어 스프라이트·애니메이터 적용");
        report.Append(lines);

        Destroy(playerGo);
        Destroy(camGo);
        yield return null;
    }

    IEnumerator CheckDepthView(StringBuilder report, System.Action fail)
    {
        foreach (var leftover in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            Destroy(leftover.gameObject);
        yield return null;

        var camGo = new GameObject("probe-rig-cam");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        var rig = camGo.AddComponent<SoulCameraRig>();
        var focus = new GameObject("focus").transform;
        rig.Bind(focus, cam);
        yield return null;

        var lines = new StringBuilder();
        int bad = 0;
        void Expect(bool ok, string what)
        {
            if (!ok) bad++;
            lines.AppendLine("    " + (ok ? "ok  " : "NG  ") + what);
        }

        float orthoRatio = NearFarRatio(cam);
        Expect(cam.orthographic && !SoulCameraRig.DepthView, "기본=직교(2D 느낌) 가까이/멀리 1m 화면 높이비=" + orthoRatio.ToString("0.00"));
        Expect(Mathf.Abs(orthoRatio - 1f) < 0.01f, "직교에서는 거리와 무관하게 같은 크기");

        rig.SetDepthView(true);
        yield return null;
        float perspRatio = NearFarRatio(cam);
        Expect(!cam.orthographic && Mathf.Approximately(cam.fieldOfView, SoulRenderSpec.DepthViewFieldOfView),
            "F2 → 원근 FOV " + cam.fieldOfView.ToString("0"));
        Expect(perspRatio > 1.15f, "원근에서는 가까운 1m가 더 크게 보임 비=" + perspRatio.ToString("0.00") + " (3D 깊이)");

        rig.OrbitDepthView(90f);
        yield return null;
        Vector3 toFocus = focus.position - cam.transform.position;
        toFocus.y = 0f;
        Expect(Mathf.Abs(SoulCameraRig.ViewYaw - 90f) < 0.5f && Vector3.Angle(toFocus, Vector3.right) < 1f,
            "회전 90° → 카메라가 초점 주위를 돎, 이동 기준 yaw=" + SoulCameraRig.ViewYaw.ToString("0"));

        rig.SetDepthView(false);
        yield return null;
        Expect(cam.orthographic && Mathf.Abs(SoulCameraRig.ViewYaw) < 0.01f, "F2 다시 → 직교 복귀, yaw 0");

        if (bad > 0)
        {
            fail();
            report.AppendLine("FAIL 3D 원근 확인 뷰 " + bad + "건");
        }
        else
            report.AppendLine("PASS 3D 원근 확인 뷰(F2) 전환·회전·복귀");
        report.Append(lines);

        Destroy(camGo);
        Destroy(focus.gameObject);
        yield return null;
    }

    IEnumerator CheckBossSpriteSkin(StringBuilder report, System.Action fail)
    {
        foreach (var leftover in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            Destroy(leftover.gameObject);
        var camGo = new GameObject("probe-cam");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        camGo.transform.rotation = SoulRenderSpec.CameraRotation;

        var playerGo = new GameObject("player");
        var player = playerGo.AddComponent<SoulPipelineAvatar>();
        player.enabled = false;

        var bossGo = new GameObject("boss");
        bossGo.transform.position = new Vector3(5f, 0f, 0f);
        var mannequin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(mannequin.GetComponent<Collider>());
        mannequin.transform.SetParent(bossGo.transform, false);
        var health = bossGo.AddComponent<SoulArenaDummyHealth>();
        var hand = new GameObject("hand");
        hand.transform.SetParent(bossGo.transform, false);
        var hitbox = hand.AddComponent<SoulWeaponHitbox3D>();
        var boss = bossGo.AddComponent<SoulBossApproachSlash>();
        boss.Bind(playerGo.transform, hitbox);
        boss.enabled = false;
        var skin = SoulBossSpriteSkin.Apply(boss);
        yield return null;
        yield return null;

        if (skin == null || skin.BodyRenderer == null)
        {
            fail();
            report.AppendLine("FAIL 보스 스프라이트 스킨 생성 실패 (Resources/" + SoulArenaSpriteSet.ResourcePath + ").");
            Destroy(playerGo);
            Destroy(bossGo);
            Destroy(camGo);
            yield break;
        }

        var body = skin.BodyRenderer;
        var anim = skin.BodyAnimator;
        var lines = new StringBuilder();
        int bad = 0;
        void Expect(bool ok, string what)
        {
            if (!ok) bad++;
            lines.AppendLine("    " + (ok ? "ok  " : "NG  ") + what);
        }

        Expect(!mannequin.GetComponent<Renderer>().enabled, "3D 마네킹 렌더러 숨김");
        Expect(anim.runtimeAnimatorController.name == "Boss", "컨트롤러=" + anim.runtimeAnimatorController.name + " (MainScene BeastZombie와 동일)");
        Expect(Mathf.Approximately(skin.Scale, 4f), "스케일=" + skin.Scale.ToString("0.##") + " (MainScene BeastZombie 4)");
        Expect(body.GetComponent<SoulSpriteAnimationEventSink>() != null, "클립 이벤트 수신기 부착");
        Vector3 bottom = body.transform.position + body.transform.up * (body.sprite.bounds.min.y * body.transform.lossyScale.y);
        Expect(Mathf.Abs(bottom.y - bossGo.transform.position.y) < 0.05f, "스프라이트 하단(루트 기준)=" + (bottom.y - bossGo.transform.position.y).ToString("0.00"));
        Expect(Vector2.Distance(skin.Facing, Vector2.left) < 0.01f, "플레이어(왼쪽)를 바라봄 Facing" + skin.Facing.ToString("0") + " sprite=" + body.sprite.name);
        Expect(anim.GetCurrentAnimatorStateInfo(0).IsName("BossIdle"), "AI 정지 → BossIdle");

        // 배치 모드는 프레임이 매우 빨라 게임 시간이 거의 흐르지 않는다. 1/60초 고정 스텝으로 진행.
        Time.captureFramerate = 60;
        boss.enabled = true;
        bool sawWalk = false;
        bool sawSwipe = false;
        float tintB = 1f;
        float impactGap = -1f;
        string swipeState = "";
        float swipeSpeed = 0f;
        int windupFrames = 0;
        float minDistance = float.MaxValue;
        float t = 0f;
        int frames = 0;
        while (t < 6f && frames < 20000 && impactGap < 0f)
        {
            yield return null;
            t += Time.deltaTime;
            frames++;
            var info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("BossWalk")) sawWalk = true;
            if (boss.IsWindup) windupFrames++;
            minDistance = Mathf.Min(minDistance, Vector3.Distance(Flat(bossGo.transform.position), Flat(playerGo.transform.position)));
            if (boss.IsWindup && !string.IsNullOrEmpty(skin.LastAttackState))
            {
                sawSwipe = info.IsName(skin.LastAttackState);
                swipeState = skin.LastAttackState;
                swipeSpeed = anim.speed;
                tintB = Mathf.Min(tintB, body.material.color.b);
            }
            if (hitbox.IsArmed && sawSwipe)
            {
                var clips = anim.GetCurrentAnimatorClipInfo(0);
                float clipLength = clips.Length > 0 ? clips[0].clip.length : 0f;
                float clipSeconds = info.normalizedTime * clipLength;
                impactGap = clipSeconds - skin.LastImpactSeconds;
            }
        }

        Expect(sawWalk, "추격 → BossWalk (최소거리 " + minDistance.ToString("0.00") + ", 윈드업 프레임 " + windupFrames + ", 보스y " + bossGo.transform.position.y.ToString("0.00") + ")");
        Expect(sawSwipe, "윈드업 → " + swipeState + " (방향별 휘두르기), 재생 속도 x" + swipeSpeed.ToString("0.00"));
        Expect(tintB < 0.85f, "윈드업 틴트가 스프라이트에 적용 b=" + tintB.ToString("0.00"));
        Expect(impactGap >= -0.12f && impactGap <= 0.12f,
            "손 히트박스 무장 순간 ↔ 클립 타격 프레임 차이 " + impactGap.ToString("0.00") + "s (타격 " + skin.LastImpactSeconds.ToString("0.00") + "s)");

        health.TakeDamage(10000f);
        yield return null;
        yield return null;
        Time.captureFramerate = 0;
        Expect(Mathf.Approximately(anim.speed, 0f), "사망 → 애니메이션 정지");

        if (bad > 0)
        {
            fail();
            report.AppendLine("FAIL 보스 스프라이트 스킨 " + bad + "건");
        }
        else
            report.AppendLine("PASS MainScene 보스(BeastZombie) 스프라이트·애니메이터 적용");
        report.Append(lines);

        Destroy(playerGo);
        Destroy(bossGo);
        Destroy(camGo);
        yield return null;
    }

    IEnumerator CheckTrashMobSpriteSkin(StringBuilder report, System.Action fail)
    {
        foreach (var leftover in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            Destroy(leftover.gameObject);
        foreach (var leftover in Object.FindObjectsByType<SoulArenaTrashMob>(FindObjectsInactive.Include))
            Destroy(leftover.gameObject);
        var camGo = new GameObject("probe-cam");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        camGo.transform.rotation = SoulRenderSpec.CameraRotation;
        var playerGo = new GameObject("player");
        var player = playerGo.AddComponent<SoulPipelineAvatar>();
        player.enabled = false;
        yield return null;

        SoulTrashMobSpriteSkin.UseMainZombieSprite = true;
        var mobGo = SoulArenaTrashMob.Spawn(new Vector3(0f, 0f, 6f), Color.green);
        var skin = mobGo.GetComponent<SoulTrashMobSpriteSkin>();
        var sphere = mobGo.transform.Find("Body");
        yield return null;
        yield return null;

        var lines = new StringBuilder();
        int bad = 0;
        void Expect(bool ok, string what)
        {
            if (!ok) bad++;
            lines.AppendLine("    " + (ok ? "ok  " : "NG  ") + what);
        }

        if (skin == null || !skin.Applied)
        {
            fail();
            report.AppendLine("FAIL 잡몹 좀비 스프라이트 스킨 생성 실패");
            Destroy(mobGo);
            Destroy(playerGo);
            Destroy(camGo);
            yield break;
        }

        var body = skin.BodyRenderer;
        var anim = skin.BodyAnimator;
        Expect(sphere != null && !sphere.GetComponent<Renderer>().enabled, "구체 몸 렌더러 숨김");
        Expect(anim.runtimeAnimatorController.name == "Zombie", "컨트롤러=" + anim.runtimeAnimatorController.name + " (MainScene Enemy와 동일)");
        Expect(Mathf.Approximately(skin.Scale, 3f), "스케일=" + skin.Scale.ToString("0.##") + " (MainScene Enemy 3)");
        Vector3 bottom = body.transform.position + body.transform.up * (body.sprite.bounds.min.y * body.transform.lossyScale.y);
        Expect(Mathf.Abs(bottom.y - mobGo.transform.position.y) < 0.1f, "스프라이트 하단(루트 기준)=" + (bottom.y - mobGo.transform.position.y).ToString("0.00"));
        Expect(Vector2.Distance(skin.Facing, Vector2.down) < 0.01f, "플레이어(화면 아래)를 바라봄 Facing" + skin.Facing.ToString("0") + " sprite=" + body.sprite.name);

        Time.captureFramerate = 60;
        bool sawMove = false;
        for (int i = 0; i < 40; i++)
        {
            yield return null;
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("ZombieMove")) sawMove = true;
        }
        Expect(sawMove, "추적 → ZombieMove, sprite=" + body.sprite.name);

        var mob = mobGo.GetComponent<SoulArenaTrashMob>();
        mob.enabled = false;
        for (int i = 0; i < 20; i++) yield return null;
        Expect(anim.GetCurrentAnimatorStateInfo(0).IsName("ZombieIdle"), "멈춤 → ZombieIdle");

        mobGo.GetComponent<SoulArenaDummyHealth>().TakeDamage(1f);
        yield return null;
        Expect(body.material.color.g < 0.9f, "피격 플래시가 스프라이트에 보임 g=" + body.material.color.g.ToString("0.00"));
        Time.captureFramerate = 0;

        if (bad > 0)
        {
            fail();
            report.AppendLine("FAIL 잡몹 좀비 스프라이트 스킨 " + bad + "건");
        }
        else
            report.AppendLine("PASS MainScene 일반 좀비 스프라이트·애니메이터 적용(잡몹)");
        report.Append(lines);

        Destroy(mobGo);
        Destroy(playerGo);
        Destroy(camGo);
        yield return null;
    }

    IEnumerator CheckBoneRigPrototype(StringBuilder report, System.Action fail)
    {
        var lines = new StringBuilder();
        int bad = 0;
        void Expect(bool ok, string what)
        {
            if (!ok) bad++;
            lines.AppendLine("    " + (ok ? "ok  " : "NG  ") + what);
        }

        var set = Resources.Load<SoulArenaSpriteSet>(SoulArenaSpriteSet.ResourcePath);
        var holder = new GameObject("rig-probe");
        var rig = set != null ? SoulPlayerBoneRig.Create(holder.transform, set.RigPrototypeSprite, 3f) : null;
        if (rig == null)
        {
            fail();
            report.AppendLine("FAIL 뼈대 시험판 생성 실패(SpriteSet Rig Prototype Sprite 확인)");
            Destroy(holder);
            yield break;
        }
        rig.enabled = false;
        yield return null;

        Mesh src = rig.SourceMesh;
        Expect(rig.Bones.Length == 11 && src.bindposes.Length == 11, "뼈대 11개, 바인드포즈 " + src.bindposes.Length);
        Expect(src.vertexCount == 48 * 59, "픽셀 격자 정점 " + src.vertexCount + " (47×58 픽셀 → 48×59)");
        var perVertex = src.GetBonesPerVertex();
        var weights = src.GetAllBoneWeights();
        int maxInfluence = 0;
        float worstSum = 0f;
        int w = 0;
        for (int v = 0; v < perVertex.Length; v++)
        {
            float sum = 0f;
            for (int k = 0; k < perVertex[v]; k++) sum += weights[w++].weight;
            maxInfluence = Mathf.Max(maxInfluence, perVertex[v]);
            worstSum = Mathf.Max(worstSum, Mathf.Abs(sum - 1f));
        }
        Expect(maxInfluence <= 4 && worstSum < 0.001f, "정점당 뼈대 최대 " + maxInfluence + "개, 가중치 합 오차 " + worstSum.ToString("0.0000"));

        float px = rig.PixelSize;
        var baked = new Mesh();
        Vector3[] Bake()
        {
            rig.Skinned.BakeMesh(baked, true);
            return baked.vertices;
        }
        rig.ResetPose();
        Vector3[] rest = Bake();
        Vector3[] original = src.vertices;
        float bindError = 0f;
        for (int v = 0; v < rest.Length; v++) bindError = Mathf.Max(bindError, Vector3.Distance(rest[v], original[v]));
        Expect(bindError < px * 0.01f, "기본 자세 = 원본 그림 (최대 오차 " + (bindError / px).ToString("0.000") + "px)");

        float MaxMove(SoulPlayerBoneRig.Motion motion, float seconds, int col, int row, bool upOnly)
        {
            int v = rig.VertexAt(col, row);
            float best = 0f;
            for (float t = 0f; t <= seconds; t += 0.02f)
            {
                rig.Pose(motion, t);
                Vector3 d = Bake()[v] - rest[v];
                best = Mathf.Max(best, upOnly ? d.y : d.magnitude);
            }
            return best / px;
        }

        float footIdle = Mathf.Max(MaxMove(SoulPlayerBoneRig.Motion.Idle, 1.2f, 27, 57, false), MaxMove(SoulPlayerBoneRig.Motion.Idle, 1.2f, 37, 57, false));
        float headIdle = MaxMove(SoulPlayerBoneRig.Motion.Idle, 1.2f, 32, 6, false);
        float hairIdle = MaxMove(SoulPlayerBoneRig.Motion.Idle, 1.2f, 14, 40, false);
        Expect(footIdle < 0.01f, "대기: 발 고정 (이동 " + footIdle.ToString("0.00") + "px)");
        Expect(headIdle >= 0.5f && hairIdle >= 0.5f, "대기: 숨쉬기 머리 " + headIdle.ToString("0.0") + "px, 쌍갈래 끝 " + hairIdle.ToString("0.0") + "px");
        float legLift = MaxMove(SoulPlayerBoneRig.Motion.Walk, 0.7f, 26, 57, true);
        float hairWalk = MaxMove(SoulPlayerBoneRig.Motion.Walk, 0.7f, 14, 40, false);
        float hipWalk = MaxMove(SoulPlayerBoneRig.Motion.Walk, 0.7f, 32, 42, false);
        Expect(legLift >= 2.2f && hairWalk >= 1.8f && hipWalk >= 1.2f,
            "걷기: 발 들기 " + legLift.ToString("0.0") + "px, 머리카락 " + hairWalk.ToString("0.0") + "px, 엉덩이 " + hipWalk.ToString("0.0") + "px (소진폭 대비 상향)");
        // 몸통 중심만 본다(팔·소매 경계는 작은 회전에도 국소 늘어남이 큼). 머리·치마 끌림 회귀 방지.
        float walkStretch = 0f;
        for (float t = 0f; t <= 0.7f; t += 0.05f)
        {
            rig.Pose(SoulPlayerBoneRig.Motion.Walk, t);
            Vector3[] pose = Bake();
            for (int row = 18; row < 44; row++)
            for (int col = 22; col < 42; col++)
            {
                int a = rig.VertexAt(col, row), b = rig.VertexAt(col + 1, row);
                float restLen = Vector3.Distance(rest[a], rest[b]);
                if (restLen < px * 0.01f) continue;
                walkStretch = Mathf.Max(walkStretch, Vector3.Distance(pose[a], pose[b]) / restLen);
            }
        }
        Expect(walkStretch < 4f, "걷기 몸통 중심 픽셀 최대 늘어남 x" + walkStretch.ToString("0.00") + " (<4, 팔12°급 찌그러짐 회귀 방지)");
        Expect(rig.ErasedWandPixels >= 8, "스프라이트 지팡이 픽셀 제거 " + rig.ErasedWandPixels + "개");
        Expect(rig.HandSocket != null && rig.WandRoot != null && rig.WandTip != null, "HandSocket + 3D 지팡이 부착");

        rig.ResetPose();
        Vector3 wandRest = rig.WandTip.position;
        float wandTravel = 0f;
        for (float t = 0f; t <= SoulPlayerBoneRig.AttackCycleSeconds; t += 0.02f)
        {
            rig.Pose(SoulPlayerBoneRig.Motion.Attack, t);
            wandTravel = Mathf.Max(wandTravel, Vector3.Distance(rig.WandTip.position, wandRest));
        }
        Expect(wandTravel >= 10f * px, "공격: 부착 지팡이 끝이 손을 따라감 " + (wandTravel / px).ToString("0.0") + "px");

        // 공격 최대 순간의 픽셀 격자 가로선 최대 늘어남(몸통 메시). 지팡이는 별도 소품이라 끊기지 않음.
        rig.Pose(SoulPlayerBoneRig.Motion.Attack, 0.4f);
        Vector3[] peak = Bake();
        float stretch = 0f;
        for (int row = 1; row < 58; row++)
        for (int col = 8; col < 54; col++)
        {
            int a = rig.VertexAt(col, row), b = rig.VertexAt(col + 1, row);
            stretch = Mathf.Max(stretch, Vector3.Distance(peak[a], peak[b]) / Vector3.Distance(rest[a], rest[b]));
        }
        Expect(stretch < 10f, "공격 최대 순간 몸통 픽셀 최대 늘어남 x" + stretch.ToString("0.00") + " (이전 x20.31, 휘두름은 지팡이 소켓이 분담)");
        Destroy(baked);
        Destroy(holder);

        var camGo = new GameObject("probe-cam");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>().orthographic = true;
        var previewGo = new GameObject("rig-preview");
        var preview = previewGo.AddComponent<SoulBoneRigPreview>();
        preview.SetOpen(true);
        yield return null;
        Expect(preview.IsOpen && preview.OriginalAnimator != null && preview.OriginalAnimator.runtimeAnimatorController.name == "Player",
            "F3 미리보기: 원본(Player 컨트롤러)과 뼈대판 생성");
        preview.SetMotion(SoulPlayerBoneRig.Motion.Walk);
        yield return null;
        yield return null;
        Expect(preview.OriginalAnimator.GetBool("IsMoving") && preview.Rig.CurrentMotion == SoulPlayerBoneRig.Motion.Walk, "F4 걷기: 양쪽 모두 걷기");
        preview.SetOpen(false);
        yield return null;
        Expect(Object.FindAnyObjectByType<SoulPlayerBoneRig>() == null, "F3 닫기: 시험판 제거");
        Destroy(previewGo);
        Destroy(camGo);

        if (bad > 0)
        {
            fail();
            report.AppendLine("FAIL 2D 뼈대 리깅 시험판 " + bad + "건");
        }
        else
            report.AppendLine("PASS 2D 뼈대 리깅 시험판(플레이어 정면 대기 1장)");
        report.Append(lines);
        yield return null;
    }

    IEnumerator CheckBoneRigPlayerSkin(StringBuilder report, System.Action fail)
    {
        var lines = new StringBuilder();
        int bad = 0;
        void Expect(bool ok, string what)
        {
            if (!ok) bad++;
            lines.AppendLine("    " + (ok ? "ok  " : "NG  ") + what);
        }

        var camGo = new GameObject("probe-cam");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        camGo.transform.rotation = SoulRenderSpec.CameraRotation;

        var playerGo = new GameObject("rig-player");
        playerGo.transform.rotation = Quaternion.LookRotation(Vector3.back);
        var mannequin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(mannequin.GetComponent<Collider>());
        mannequin.transform.SetParent(playerGo.transform, false);
        var avatar = playerGo.AddComponent<SoulPipelineAvatar>();
        avatar.enabled = false;
        var skin = SoulPlayerSpriteSkin.Apply(avatar);
        var rigSkin = skin != null ? SoulPlayerBoneRigSkin.Apply(skin) : null;
        yield return null;
        yield return null;

        if (rigSkin == null || rigSkin.Rig == null)
        {
            fail();
            report.AppendLine("FAIL 본게임 뼈대 플레이어 생성 실패(스킨 또는 SpriteSet Rig Prototype Sprite)");
            Destroy(playerGo);
            Destroy(camGo);
            yield break;
        }

        var rig = rigSkin.Rig;
        Expect(rigSkin.Active && !skin.BodyRenderer.enabled && rig.Skinned.enabled, "정면(화면 아래): 뼈대 표시, 원본 스프라이트 숨김");
        Expect(Mathf.Approximately(rig.AttackDuration, SoulPlayerBoneRigSkin.LightAttackSeconds), "공격 재생 시간 " + rig.AttackDuration.ToString("0.00") + "초 (원본 LightAttack 0.75초)");

        Vector3 meshBottom = rig.transform.TransformPoint(new Vector3(0f, rig.SourceMesh.bounds.min.y, 0f));
        float feet = meshBottom.y - playerGo.transform.position.y;
        Expect(Mathf.Abs(feet) < 0.05f, "뼈대 그림 하단(루트 기준) " + feet.ToString("0.000") + " (원본 스프라이트와 같은 바닥)");
        Expect(Quaternion.Angle(rig.transform.rotation, cam.transform.rotation) < 0.5f, "뼈대 빌보드=카메라 회전 일치");
        Expect(rig.CurrentMotion == SoulPlayerBoneRig.Motion.Idle, "정지 중 = 대기 동작");

        typeof(SoulPipelineAvatar).GetProperty("IsMoving").SetValue(avatar, true, null);
        yield return null;
        yield return null;
        Expect(rig.CurrentMotion == SoulPlayerBoneRig.Motion.Walk, "이동 중 = 걷기 동작");
        typeof(SoulPipelineAvatar).GetProperty("IsMoving").SetValue(avatar, false, null);

        var swung = typeof(SoulPipelineAvatar).GetField("LightSwung", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(avatar) as System.Action;
        swung?.Invoke();
        yield return null;
        Expect(swung != null && rig.CurrentMotion == SoulPlayerBoneRig.Motion.Attack, "가벼운 공격 신호 = 공격 동작");
        yield return WaitSeconds(SoulPlayerBoneRigSkin.LightAttackSeconds + 0.2f);
        Expect(rig.CurrentMotion == SoulPlayerBoneRig.Motion.Idle, "공격 길이 후 대기로 복귀");

        playerGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
        yield return null;
        yield return null;
        Expect(!rigSkin.Active && skin.BodyRenderer.enabled && !rig.Skinned.gameObject.activeInHierarchy, "오른쪽 방향: 원본 프레임 애니메이션으로 복귀");
        playerGo.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        yield return null;
        yield return null;
        Expect(!rigSkin.Active && skin.BodyRenderer.enabled, "위쪽(뒷모습): 원본 프레임 유지");
        playerGo.transform.rotation = Quaternion.LookRotation(Vector3.back);
        yield return null;
        yield return null;
        Expect(rigSkin.Active && !skin.BodyRenderer.enabled, "다시 정면: 뼈대 표시");

        rigSkin.UserEnabled = false;
        yield return null;
        Expect(!rigSkin.Active && skin.BodyRenderer.enabled, "F5 끔: 항상 원본");
        rigSkin.UserEnabled = true;
        yield return null;
        Expect(rigSkin.Active && !skin.BodyRenderer.enabled, "F5 켬: 정면에서 뼈대");

        Destroy(rigSkin);
        yield return null;
        Expect(skin.BodyRenderer.enabled && playerGo.transform.Find(SoulPlayerBoneRigSkin.RootName) == null, "컴포넌트 제거: 원본 복구, 뼈대 오브젝트 정리");

        Destroy(playerGo);
        Destroy(camGo);
        if (bad > 0)
        {
            fail();
            report.AppendLine("FAIL 본게임 뼈대 플레이어 " + bad + "건");
        }
        else
            report.AppendLine("PASS 본게임 뼈대 플레이어(정면만 뼈대, 나머지 원본)");
        report.Append(lines);
        yield return null;
    }
    // 초점 앞(-Z 8m)과 뒤(+Z 8m)에 세운 1m 막대의 화면 높이 비. 직교=1, 원근>1.
    static float NearFarRatio(Camera cam)
    {
        float Height(Vector3 basePos) =>
            Mathf.Abs(cam.WorldToScreenPoint(basePos + Vector3.up).y - cam.WorldToScreenPoint(basePos).y);
        return Height(new Vector3(0f, 0f, -8f)) / Mathf.Max(0.0001f, Height(new Vector3(0f, 0f, 8f)));
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

    static void SetProperty(object target, string name, object value)
    {
        target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public).SetValue(target, value);
    }

    static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    static void Invoke(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(target, null);
    }
}
