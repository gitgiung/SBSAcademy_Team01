using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// 실제 FieldItem*.cs와 SceneEnemyPool.FieldItems.cs를 실행하고, 구워 둔 Assets/Resources/FieldItems 파일을 읽어 확인합니다.
// Unity 오브젝트 수명·씬·카메라·풀·Resources와 SceneEnemyPool의 나머지 절반은 이 파일의 대역입니다.
// Unity의 실제 임포트 결과(스프라이트 생성, 필터, 압축)는 여기서 확인하지 못합니다.
static class Program
{
    static int failures, count;

    static void Test(string name, Action action)
    {
        count++;
        UnityEngine.Object.ResetWorld();
        ResetSpriteCache();
        try { action(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e).Message); }
    }

    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Equal<T>(T expected, T actual, string what)
    { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{what}: expected {expected}, actual {actual}"); }
    static void Near(float expected, float actual, string what)
    { if (Math.Abs(expected - actual) > .0001f) throw new Exception($"{what}: expected {expected}, actual {actual}"); }

    static void Tick(MonoBehaviour behaviour) => behaviour.GetType()
        .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(behaviour, null);

    static void Frame(float deltaTime, float timeScale = 1f)
    {
        Time.timeScale = timeScale;
        Time.unscaledDeltaTime = deltaTime;
        Time.deltaTime = deltaTime * timeScale;
    }

    static PlayerData NewPlayer(float x = 0f, float y = 0f)
    {
        var player = new GameObject("Player").AddComponent<PlayerData>();
        player.transform.position = new Vector3(x, y, 0f);
        return player;
    }

    static readonly Sprite TestSprite = new Sprite();

    static FieldItemPickup NewItem(PlayerData player, List<FieldItemKind> effects, float x = 5f,
        FieldItemKind kind = FieldItemKind.HealPack, float distance = 1f)
    {
        return FieldItemPickup.Create(kind, new Vector3(x, 0f, 0f), TestSprite, 1f, distance, player.transform,
            (item, collector) => effects.Add(item.Kind));
    }

    static SceneEnemyPool NewPool(float heal = 0f, float magnet = 0f, float bomb = 0f)
    {
        var pool = new GameObject("Pool").AddComponent<SceneEnemyPool>();
        pool.gameObject.scene = new Scene(1);
        pool.FieldItems.healPackChance = heal;
        pool.FieldItems.magnetChance = magnet;
        pool.FieldItems.bombChance = bomb;
        return pool;
    }

    // 일반 몬스터: 죽으면 실제 풀처럼 처치 알림 뒤 대여 목록에서 빠집니다.
    static EnemyHealth NewEnemy(SceneEnemyPool pool, float x, float y = 0f, uint lease = 1)
    {
        var item = new GameObject("Enemy").AddComponent<PooledObject>();
        item.transform.position = new Vector3(x, y, 0f);
        var health = item.gameObject.AddComponent<EnemyHealth>();
        health.Killed = enemy => { pool.Defeat(enemy); pool.activeEnemies.Remove(item); };
        pool.activeEnemies.Add(item, lease);
        return health;
    }

    static Camera NewCamera(float x = 0f, float y = 0f, float size = 5f, float aspect = 2f)
    {
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(x, y, -10f);
        camera.orthographic = true;
        camera.orthographicSize = size;
        camera.aspect = aspect;
        return camera;
    }

    static List<FieldItemPickup> Items() => new List<FieldItemPickup>(UnityEngine.Object.FindObjectsByType<FieldItemPickup>());

    // FieldItemSprites는 한 번 불러온 그림을 계속 쓰므로 테스트마다 비웁니다.
    static void ResetSpriteCache()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        Array.Clear((Sprite[])typeof(FieldItemSprites).GetField("items", flags).GetValue(null));
        typeof(FieldItemSprites).GetField("blastRing", flags).SetValue(null, null);
        typeof(FieldItemSprites).GetField("blastFrames", flags).SetValue(null, null);
    }

    // Resources에 구운 그림이 임포트돼 있는 상황의 대역입니다. width는 유닛 단위 너비입니다.
    static Sprite Asset(string name, float width = 1f)
    {
        var sprite = new Sprite { name = name, width = width };
        Resources.Assets[FieldItemSprites.ResourceFolder + "/" + name] = sprite;
        return sprite;
    }

    static Sprite[] BlastAssets(int frames)
    {
        var sprites = new Sprite[frames];
        // 일부러 뒤에서부터 등록해 이름순 정렬을 확인합니다.
        for (int i = frames - 1; i >= 0; i--) sprites[i] = Asset(FieldItemSprites.BlastFramePrefix + i.ToString("00"), 5f);
        return sprites;
    }

    static SpriteRenderer RingOf(FieldItemBlastFx blast)
    {
        foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>())
            if (renderer.transform.parent == blast.transform) return renderer;
        throw new Exception("파동 고리 없음");
    }

    static float RingWorldDiameter(FieldItemBlastFx blast)
    {
        SpriteRenderer ring = RingOf(blast);
        return ring.transform.localScale.x * blast.transform.localScale.x * ring.sprite.bounds.size.x;
    }

    // ── 구워 둔 파일 ────────────────────────────────────────────
    static string AssetFolder([System.Runtime.CompilerServices.CallerFilePath] string source = "") =>
        Environment.GetEnvironmentVariable("FIELD_ITEM_ASSET_DIR") ?? Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(source), "../../Assets/Resources", FieldItemSprites.ResourceFolder));

    sealed class Png { public int Width, Height; public byte[] Rgba; public byte Alpha(int x, int y) => Rgba[(y * Width + x) * 4 + 3]; }

    // 굽기 스크립트가 쓰는 형식(8비트 RGBA, 필터 없음)만 읽습니다.
    static Png ReadPng(string name)
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(AssetFolder(), name + ".png"));
        var png = new Png();
        var packed = new MemoryStream();
        for (int position = 8; position < bytes.Length;)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(position));
            string type = Encoding.ASCII.GetString(bytes, position + 4, 4);
            if (type == "IHDR")
            {
                png.Width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(position + 8));
                png.Height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(position + 12));
                Check(bytes[position + 16] == 8 && bytes[position + 17] == 6, name + " RGBA8 아님");
            }
            else if (type == "IDAT") packed.Write(bytes, position + 8, length);
            position += 12 + length;
        }
        packed.Position = 0;
        var raw = new MemoryStream();
        using (var inflate = new ZLibStream(packed, CompressionMode.Decompress)) inflate.CopyTo(raw);
        byte[] rows = raw.ToArray();
        int stride = png.Width * 4 + 1;
        Equal(stride * png.Height, rows.Length, name + " 데이터 길이");
        png.Rgba = new byte[png.Width * png.Height * 4];
        for (int y = 0; y < png.Height; y++)
        {
            Equal((byte)0, rows[y * stride], name + " 행 필터");
            Buffer.BlockCopy(rows, y * stride + 1, png.Rgba, y * png.Width * 4, png.Width * 4);
        }
        return png;
    }

    static int Opaque(Png png) { int total = 0; for (int i = 3; i < png.Rgba.Length; i += 4) if (png.Rgba[i] != 0) total++; return total; }

    static bool BorderClear(Png png)
    {
        for (int i = 0; i < png.Width; i++) if (png.Alpha(i, 0) != 0 || png.Alpha(i, png.Height - 1) != 0) return false;
        for (int i = 0; i < png.Height; i++) if (png.Alpha(0, i) != 0 || png.Alpha(png.Width - 1, i) != 0) return false;
        return true;
    }

    static string[] BakedNames()
    {
        var names = new List<string>();
        foreach (string file in Directory.GetFiles(AssetFolder(), "*.png")) names.Add(Path.GetFileNameWithoutExtension(file));
        names.Sort(string.CompareOrdinal);
        return names.ToArray();
    }

    static void AssetFileTests()
    {
        Test("구운 아이템 그림 3종은 32×32 픽셀 그림이고 가장자리가 비어 있음", () =>
        {
            foreach (var kind in new[] { FieldItemKind.HealPack, FieldItemKind.Magnet, FieldItemKind.Bomb })
            {
                Png png = ReadPng(FieldItemSprites.ResourceName(kind));
                Equal(32, png.Width, kind + " 너비"); Equal(32, png.Height, kind + " 높이");
                Check(BorderClear(png), kind + " 가장자리에 닿음");
                Check(Opaque(png) >= 200, kind + " 내용 부족 " + Opaque(png));
                for (int i = 3; i < png.Rgba.Length; i += 4)
                    Check(png.Rgba[i] == 0 || png.Rgba[i] == 255, kind + " 반투명 픽셀");
            }
        });
        Test("구운 폭발 프레임은 번호가 이어진 160×160이고 마지막 프레임만 비어 있음", () =>
        {
            var frames = new List<string>();
            foreach (string name in BakedNames()) if (name.StartsWith(FieldItemSprites.BlastFramePrefix, StringComparison.Ordinal)) frames.Add(name);
            Equal(14, frames.Count, "프레임 수");
            for (int i = 0; i < frames.Count; i++)
            {
                Equal(FieldItemSprites.BlastFramePrefix + i.ToString("00"), frames[i], "프레임 이름");
                Png png = ReadPng(frames[i]);
                Equal(160, png.Width, frames[i] + " 너비"); Equal(160, png.Height, frames[i] + " 높이");
                Check(BorderClear(png), frames[i] + " 가장자리에서 잘림");
                if (i == frames.Count - 1) Equal(0, Opaque(png), "마지막 프레임");
                else Check(Opaque(png) > 0, frames[i] + " 비어 있음");
            }
            // 불덩이가 커졌다가 흩어집니다.
            Check(Opaque(ReadPng(frames[5])) > Opaque(ReadPng(frames[0])) * 10, "커지지 않음");
            Check(Opaque(ReadPng(frames[12])) < Opaque(ReadPng(frames[5])) / 10, "흩어지지 않음");
        });
        Test("구운 고리 그림은 128×128이고 가장자리 띠가 밝고 모서리는 투명", () =>
        {
            Png png = ReadPng(FieldItemSprites.BlastRingName);
            Equal(128, png.Width, "너비"); Equal(128, png.Height, "높이");
            Equal((byte)0, png.Alpha(0, 0), "모서리");
            Check(png.Alpha(64, 64) > 0 && png.Alpha(64, 64) < 80, "가운데 " + png.Alpha(64, 64));
            Check(png.Alpha(119, 64) > 200, "띠 " + png.Alpha(119, 64)); // 중심에서 약 0.86 반지름
        });
        Test("구운 파일은 코드가 찾는 이름과 일치하고 남는 파일이 없음", () =>
        {
            var expected = new List<string> { FieldItemSprites.BlastRingName };
            foreach (var kind in new[] { FieldItemKind.HealPack, FieldItemKind.Magnet, FieldItemKind.Bomb }) expected.Add(FieldItemSprites.ResourceName(kind));
            for (int i = 0; i < 14; i++) expected.Add(FieldItemSprites.BlastFramePrefix + i.ToString("00"));
            expected.Sort(string.CompareOrdinal);
            Equal(string.Join(",", expected), string.Join(",", BakedNames()), "파일 목록");
        });
        Test("meta는 단일 스프라이트·32 PPU·밉맵과 압축 없음이고 GUID가 겹치지 않음", () =>
        {
            var guids = new HashSet<string>();
            foreach (string name in BakedNames())
            {
                string meta = File.ReadAllText(Path.Combine(AssetFolder(), name + ".png.meta")).Replace("\r\n", "\n");
                foreach (string setting in new[] { "\n  textureType: 8\n", "\n  spriteMode: 1\n", "\n  spritePixelsToUnits: 32\n",
                    "\n  alignment: 0\n", "\n    enableMipMap: 0\n", "\n  alphaIsTransparency: 1\n" })
                    Check(meta.Contains(setting), name + " meta에 없음: " + setting.Trim());
                Equal(3, meta.Split("\n    textureCompression: 0\n").Length - 1, name + " 압축 없음 플랫폼 수");
                // 아이템과 폭발은 Point, 화면 크기로 늘리는 고리만 Bilinear입니다.
                string filter = name == FieldItemSprites.BlastRingName ? "1" : "0";
                Check(meta.Contains("\n    filterMode: " + filter + "\n"), name + " 필터");
                string guid = meta.Split('\n')[1];
                Check(guid.StartsWith("guid: ", StringComparison.Ordinal) && guid.Length == 38, name + " GUID 형식");
                Check(guids.Add(guid), name + " GUID 중복");
            }
            Equal(18, guids.Count, "meta 수");
        });
    }

    static int Main()
    {
        // ── 드랍 판정 ──────────────────────────────────────────────
        Test("기본 임시 확률은 세 종류 모두 0보다 크고 합이 1 이하", () =>
        {
            var s = new FieldItemDropSettings();
            Check(s.healPackChance > 0f && s.magnetChance > 0f && s.bombChance > 0f, "0 이하 확률");
            Check(s.healPackChance + s.magnetChance + s.bombChance <= 1f, "합이 1 초과");
        });
        Test("난수 구간은 힐팩→자석→폭탄→없음 순서", () =>
        {
            var s = new FieldItemDropSettings { healPackChance = .2f, magnetChance = .3f, bombChance = .1f };
            Equal(FieldItemKind.HealPack, s.Roll(0f), "0");
            Equal(FieldItemKind.HealPack, s.Roll(.199f), ".199");
            Equal(FieldItemKind.Magnet, s.Roll(.2f), ".2");
            Equal(FieldItemKind.Magnet, s.Roll(.499f), ".499");
            Equal(FieldItemKind.Bomb, s.Roll(.5f), ".5");
            Equal(FieldItemKind.Bomb, s.Roll(.599f), ".599");
            Equal(FieldItemKind.None, s.Roll(.61f), ".61");
            Equal(FieldItemKind.None, s.Roll(1f), "1");
        });
        Test("균등 난수 10만 개의 종류별 빈도가 설정 확률과 일치", () =>
        {
            var s = new FieldItemDropSettings();
            const int samples = 100000;
            var counts = new int[4];
            for (int i = 0; i < samples; i++) counts[(int)s.Roll((i + .5f) / samples)]++;
            Check(Math.Abs(counts[1] - s.healPackChance * samples) <= 1, "힐팩 " + counts[1]);
            Check(Math.Abs(counts[2] - s.magnetChance * samples) <= 1, "자석 " + counts[2]);
            Check(Math.Abs(counts[3] - s.bombChance * samples) <= 1, "폭탄 " + counts[3]);
        });
        Test("확률 0인 종류는 나오지 않음", () =>
        {
            var s = new FieldItemDropSettings { healPackChance = 0f, magnetChance = 1f, bombChance = 0f };
            for (int i = 0; i < 1000; i++) Equal(FieldItemKind.Magnet, s.Roll(i / 1000f), "roll " + i);
            var none = new FieldItemDropSettings { healPackChance = 0f, magnetChance = 0f, bombChance = 0f };
            for (int i = 0; i <= 1000; i++) Equal(FieldItemKind.None, none.Roll(i / 1000f), "roll " + i);
        });
        Test("잘못된 확률과 난수는 드랍하지 않음", () =>
        {
            var s = new FieldItemDropSettings { healPackChance = float.NaN, magnetChance = -1f, bombChance = float.NaN };
            Equal(FieldItemKind.None, s.Roll(0f), "잘못된 확률");
            var valid = new FieldItemDropSettings { healPackChance = 1f, magnetChance = 0f, bombChance = 0f };
            Equal(FieldItemKind.None, valid.Roll(float.NaN), "NaN 난수");
            Equal(FieldItemKind.None, valid.Roll(-.1f), "음수 난수");
            Near(1f, FieldItemDropSettings.Chance(5f), "1 초과");
            Near(1f, FieldItemDropSettings.Chance(float.PositiveInfinity), "무한대");
            Near(0f, FieldItemDropSettings.Chance(float.NaN), "NaN");
        });
        Test("힐팩 회복량은 최대 체력 × 비율, 잘못된 값은 0", () =>
        {
            var s = new FieldItemDropSettings { healPackHealthRatio = .3f };
            Near(30f, s.HealAmount(100f), "100");
            Near(45f, s.HealAmount(150f), "150");
            Near(0f, s.HealAmount(0f), "0");
            Near(0f, s.HealAmount(float.NaN), "NaN");
            Near(0f, s.HealAmount(float.PositiveInfinity), "무한대");
            s.healPackHealthRatio = float.NaN;
            Near(0f, s.HealAmount(100f), "NaN 비율");
        });
        Test("화면 판정은 경계 포함, 밖과 잘못된 크기는 제외", () =>
        {
            var center = new Vector2(10f, -4f);
            Check(FieldItemDropSettings.IsInsideView(new Vector2(10f, -4f), center, 8f, 5f), "중심");
            Check(FieldItemDropSettings.IsInsideView(new Vector2(18f, 1f), center, 8f, 5f), "경계");
            Check(!FieldItemDropSettings.IsInsideView(new Vector2(18.01f, -4f), center, 8f, 5f), "가로 밖");
            Check(!FieldItemDropSettings.IsInsideView(new Vector2(10f, 1.01f), center, 8f, 5f), "세로 밖");
            Check(!FieldItemDropSettings.IsInsideView(center, center, float.NaN, 5f), "NaN");
            Check(!FieldItemDropSettings.IsInsideView(center, center, -1f, 5f), "음수");
        });

        // ── 임시 그림 ──────────────────────────────────────────────
        Test("세 종류 그림 모두 16×16이고 팔레트가 유효", () =>
        {
            foreach (var kind in new[] { FieldItemKind.HealPack, FieldItemKind.Magnet, FieldItemKind.Bomb })
            {
                string[] rows = FieldItemPixelArt.Rows(kind);
                Equal(16, rows.Length, kind + " 행 수");
                foreach (string row in rows)
                {
                    Equal(16, row.Length, kind + " 열 수");
                    foreach (char symbol in row) Check(FieldItemPixelArt.TryColor(symbol, out _), kind + " 미정의 문자 " + symbol);
                }
                Color32[] pixels = FieldItemPixelArt.BuildPixels(kind);
                Equal(256, pixels.Length, kind + " 픽셀 수");
                int opaque = 0;
                foreach (Color32 pixel in pixels) if (pixel.a == 255) opaque++;
                Check(opaque >= 60, kind + " 불투명 픽셀 " + opaque);
            }
        });
        Test("없는 종류는 그림과 스프라이트가 없음", () =>
        {
            Check(FieldItemPixelArt.Rows(FieldItemKind.None) == null, "None 행");
            Check(FieldItemPixelArt.BuildPixels(FieldItemKind.None) == null, "None 픽셀");
            Check(FieldItemPixelArt.BuildPixels((FieldItemKind)99) == null, "범위 밖");
            Check(FieldItemSprites.Get(FieldItemKind.None) == null, "None 스프라이트");
            Check(FieldItemSprites.Get((FieldItemKind)99) == null, "범위 밖 스프라이트");
            Check(!FieldItemPixelArt.TryColor('?', out _), "미정의 문자");
        });
        Test("텍스처용 픽셀은 위아래를 뒤집어 채움", () =>
        {
            // 폭탄 그림 맨 윗줄 10열의 불꽃(y)이 텍스처 맨 윗줄(15행)에 있어야 합니다.
            Color32[] pixels = FieldItemPixelArt.BuildPixels(FieldItemKind.Bomb);
            Color32 spark = pixels[15 * 16 + 10];
            Check(spark.r == 255 && spark.g == 224 && spark.b == 102 && spark.a == 255, "불꽃 위치");
            Equal((byte)0, pixels[0].a, "맨 아랫줄 모서리");
        });
        Test("스프라이트는 종류별로 한 번만 만들고 Point 필터", () =>
        {
            Sprite heal = FieldItemSprites.Get(FieldItemKind.HealPack);
            Check(heal != null, "힐팩 없음");
            Check(ReferenceEquals(heal, FieldItemSprites.Get(FieldItemKind.HealPack)), "재생성됨");
            Check(!ReferenceEquals(heal, FieldItemSprites.Get(FieldItemKind.Magnet)), "종류 구분 없음");
            Check(FieldItemSprites.Get(FieldItemKind.Bomb) != null, "폭탄 없음");
            Equal(FilterMode.Point, heal.texture.filterMode, "필터");
            Equal(16, heal.texture.width, "크기");
            Near(16f, heal.pixelsPerUnit, "PPU");
        });
        Test("폭탄 파동 고리는 가장자리 띠가 밝고 모서리는 투명", () =>
        {
            Sprite ring = FieldItemSprites.BlastRing();
            Check(ReferenceEquals(ring, FieldItemSprites.BlastRing()), "재생성됨");
            Color32[] pixels = ring.texture.pixels;
            int size = ring.texture.width;
            Equal(64, size, "해상도");
            Equal((byte)0, pixels[0].a, "모서리");
            byte middle = pixels[32 * size + 32].a;
            byte band = pixels[32 * size + 59].a; // 중심에서 약 0.86 반지름
            Check(middle > 0 && middle < 80, "가운데 " + middle);
            Check(band > 200, "띠 " + band);
        });

        Test("Resources에 구운 그림이 있으면 그 그림을 쓰고 없으면 코드 그림으로 대신함", () =>
        {
            Sprite baked = Asset(FieldItemSprites.ResourceName(FieldItemKind.HealPack));
            Check(ReferenceEquals(baked, FieldItemSprites.Get(FieldItemKind.HealPack)), "구운 그림 미사용");
            Check(ReferenceEquals(baked, FieldItemSprites.Get(FieldItemKind.HealPack)), "다시 불러옴");
            Equal(1, Resources.LoadCalls, "불러온 횟수");
            Sprite fallback = FieldItemSprites.Get(FieldItemKind.Magnet);
            Check(fallback != null && fallback.texture != null && fallback.texture.width == 16, "대체 그림 없음");
            Sprite ring = Asset(FieldItemSprites.BlastRingName, 4f);
            Check(ReferenceEquals(ring, FieldItemSprites.BlastRing()), "구운 고리 미사용");
        });
        Test("종류별 그림 파일 이름이 정해져 있고 없는 종류는 이름이 없음", () =>
        {
            Equal("HealPack", FieldItemSprites.ResourceName(FieldItemKind.HealPack), "힐팩");
            Equal("Magnet", FieldItemSprites.ResourceName(FieldItemKind.Magnet), "자석");
            Equal("Bomb", FieldItemSprites.ResourceName(FieldItemKind.Bomb), "폭탄");
            Check(FieldItemSprites.ResourceName(FieldItemKind.None) == null, "None");
            Asset("None"); Asset("99");
            Check(FieldItemSprites.Get(FieldItemKind.None) == null && FieldItemSprites.Get((FieldItemKind)99) == null, "없는 종류 그림");
        });
        Test("폭발 프레임은 접두사가 맞는 것만 이름순으로 돌려주고 없으면 빈 배열", () =>
        {
            Equal(0, FieldItemSprites.BlastFrames().Length, "없을 때");
            Sprite[] baked = BlastAssets(14);
            Asset("HealPack"); Asset(FieldItemSprites.BlastRingName); Asset("BombBlastOther");
            Sprite[] frames = FieldItemSprites.BlastFrames();
            Equal(14, frames.Length, "프레임 수");
            for (int i = 0; i < frames.Length; i++) Check(ReferenceEquals(baked[i], frames[i]), "순서 " + i);
            Check(ReferenceEquals(frames, FieldItemSprites.BlastFrames()), "다시 불러옴");
        });
        AssetFileTests();

        // ── 필드 아이템 획득 ───────────────────────────────────────
        Test("획득 거리 안으로 들어오면 효과를 한 번만 알림", () =>
        {
            var player = NewPlayer(); var effects = new List<FieldItemKind>();
            FieldItemPickup item = NewItem(player, effects, 5f, FieldItemKind.Magnet);
            Frame(.016f);
            Tick(item); Equal(0, effects.Count, "멀리서 획득");
            Check(!item.Collected, "멀리서 획득 상태");
            player.transform.position = new Vector3(4.2f, 0f, 0f);
            Tick(item); Equal(1, effects.Count, "획득");
            Equal(FieldItemKind.Magnet, effects[0], "종류");
            Check(item.Collected, "획득 상태");
            Tick(item); Check(!item.TryCollect(player), "재획득");
            Equal(1, effects.Count, "중복 효과");
        });
        Test("획득 거리 경계는 포함하고 0 이하·잘못된 거리는 획득 없음", () =>
        {
            Check(FieldItemPickup.IsWithin(new Vector2(0f, 0f), new Vector2(3f, 4f), 5f), "경계");
            Check(!FieldItemPickup.IsWithin(new Vector2(0f, 0f), new Vector2(3f, 4f), 4.99f), "밖");
            Check(!FieldItemPickup.IsWithin(new Vector2(0f, 0f), new Vector2(0f, 0f), 0f), "0");
            Check(!FieldItemPickup.IsWithin(new Vector2(0f, 0f), new Vector2(0f, 0f), float.NaN), "NaN");
            Check(!FieldItemPickup.IsWithin(new Vector2(0f, 0f), new Vector2(0f, 0f), float.PositiveInfinity), "무한대");
        });
        Test("사망한 플레이어는 획득하지 못하고 아이템이 남음", () =>
        {
            var player = NewPlayer(5f); var effects = new List<FieldItemKind>();
            FieldItemPickup item = NewItem(player, effects);
            player.IsDead = true;
            Frame(.016f); Tick(item);
            Equal(0, effects.Count, "효과"); Check(!item.Collected, "획득 상태");
            Check(item != null, "파괴됨");
        });
        Test("전투가 멈춘 동안(timeScale 0)에는 획득하지 않고 재개 후 획득", () =>
        {
            var player = NewPlayer(5f); var effects = new List<FieldItemKind>();
            FieldItemPickup item = NewItem(player, effects);
            Frame(.016f, 0f); Tick(item);
            Equal(0, effects.Count, "정지 중 획득");
            Frame(.016f); Tick(item);
            Equal(1, effects.Count, "재개 후 획득");
        });
        Test("획득 연출은 실제 시간으로 끝나고 오브젝트가 파괴됨", () =>
        {
            var player = NewPlayer(5f); var effects = new List<FieldItemKind>();
            FieldItemPickup item = NewItem(player, effects);
            Frame(.016f); Tick(item);
            Check(item != null, "획득 직후 파괴");
            Frame(.1f, 0f); Tick(item);
            Check(item != null, "연출 중 파괴");
            Check(item.transform.localScale.x > 1f, "커지지 않음");
            Frame(.1f, 0f); Tick(item);
            Check(item == null, "연출 후 남음");
        });
        Test("효과 처리에서 예외가 나도 다시 지급하지 않음", () =>
        {
            var player = NewPlayer(5f); int calls = 0;
            FieldItemPickup item = FieldItemPickup.Create(FieldItemKind.Bomb, new Vector3(5f, 0f, 0f), TestSprite, 1f, 1f,
                player.transform, (_, __) => { calls++; throw new InvalidOperationException("효과 실패"); });
            Frame(.016f); Tick(item); Tick(item);
            Equal(1, calls, "호출 수"); Check(item.Collected, "획득 상태");
            Equal(1, Debug.Exceptions, "기록된 예외");
        });
        Test("Discard는 효과 없이 제거", () =>
        {
            var player = NewPlayer(5f); var effects = new List<FieldItemKind>();
            FieldItemPickup item = NewItem(player, effects);
            item.Discard();
            Check(item == null, "남음"); Equal(0, effects.Count, "효과");
        });
        Test("종류나 그림이 없으면 만들지 않음", () =>
        {
            var player = NewPlayer();
            Check(FieldItemPickup.Create(FieldItemKind.None, default, TestSprite, 1f, 1f, player.transform, null) == null, "None");
            Check(FieldItemPickup.Create(FieldItemKind.Bomb, default, null, 1f, 1f, player.transform, null) == null, "그림 없음");
            Equal(0, Items().Count, "생성된 오브젝트");
        });
        Test("대상이 없으면 씬의 플레이어를 찾아 획득하고 잘못된 크기는 1배", () =>
        {
            var player = NewPlayer(5f); var effects = new List<FieldItemKind>();
            FieldItemPickup item = FieldItemPickup.Create(FieldItemKind.HealPack, new Vector3(5f, 0f, 0f), TestSprite,
                float.NaN, 1f, null, (i, c) => effects.Add(i.Kind));
            Near(1f, item.transform.localScale.x, "크기");
            Frame(.016f); Tick(item);
            Equal(1, effects.Count, "획득");
        });

        // ── 드랍과 효과 연결 (SceneEnemyPool.FieldItems) ───────────
        Test("확률 1이면 처치한 자리에 그 종류가 떨어짐", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool(bomb: 1f);
            EnemyHealth enemy = NewEnemy(pool, 7f, -3f);
            UnityEngine.Random.value = .5f;
            enemy.Kill();
            List<FieldItemPickup> items = Items();
            Equal(1, items.Count, "개수"); Equal(FieldItemKind.Bomb, items[0].Kind, "종류");
            Near(7f, items[0].transform.position.x, "x"); Near(-3f, items[0].transform.position.y, "y");
            Equal(1, items[0].gameObject.scene.id, "씬");
            Equal(1, pool.ActiveFieldItemCount, "풀 집계");
            Check(items[0].GetComponent<SpriteRenderer>().sprite != null, "그림 없음");
        });
        Test("난수가 확률 구간 밖이면 떨어지지 않음", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool(.01f, .01f, .01f);
            UnityEngine.Random.value = .5f;
            for (int i = 0; i < 20; i++) NewEnemy(pool, i).Kill();
            Equal(0, Items().Count, "개수");
        });
        Test("Inspector에 지정한 스프라이트를 코드 그림보다 우선 사용", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            var custom = new Sprite();
            pool.FieldItems.magnetSprite = custom;
            Check(pool.TryDropFieldItem(FieldItemKind.Magnet, default), "드랍 실패");
            Check(ReferenceEquals(custom, Items()[0].GetComponent<SpriteRenderer>().sprite), "지정 그림 미사용");
        });
        Test("동시 개수 제한을 넘으면 떨어지지 않고, 획득하면 다시 떨어짐", () =>
        {
            PlayerData player = NewPlayer(100f); SceneEnemyPool pool = NewPool(heal: 1f);
            pool.FieldItems.maxFieldItems = 2;
            UnityEngine.Random.value = 0f;
            for (int i = 0; i < 5; i++) NewEnemy(pool, i).Kill();
            Equal(2, pool.ActiveFieldItemCount, "제한");
            Check(Items()[0].TryCollect(player), "획득 실패");
            Equal(1, pool.ActiveFieldItemCount, "획득 후 집계");
            NewEnemy(pool, 9f).Kill();
            Equal(2, pool.ActiveFieldItemCount, "획득 후 드랍");
            pool.FieldItems.maxFieldItems = 0;
            Check(!pool.TryDropFieldItem(FieldItemKind.Bomb, default), "제한 0");
        });
        Test("힐팩 획득 시 최대 체력 × 비율만큼 회복 요청", () =>
        {
            PlayerData player = NewPlayer(); SceneEnemyPool pool = NewPool();
            player.maxHealth = 150f; pool.FieldItems.healPackHealthRatio = .3f;
            pool.TryDropFieldItem(FieldItemKind.HealPack, default);
            Frame(.016f); Tick(Items()[0]);
            Equal(1, player.RestoreCalls, "회복 호출"); Near(45f, player.Restored, "회복량");
        });
        Test("자석 획득 시 같은 씬의 경험치만 설정 속도로 끌어당김", () =>
        {
            PlayerData player = NewPlayer(); SceneEnemyPool pool = NewPool();
            pool.FieldItems.magnetAttractionSpeed = 18f;
            var gems = new List<ExperiencePickup>();
            for (int i = 0; i < 3; i++)
            {
                var gem = new GameObject("Gem").AddComponent<ExperiencePickup>();
                gem.gameObject.scene = new Scene(1); gems.Add(gem);
            }
            var other = new GameObject("OtherSceneGem").AddComponent<ExperiencePickup>();
            other.gameObject.scene = new Scene(2);
            pool.TryDropFieldItem(FieldItemKind.Magnet, default);
            Frame(.016f); Tick(Items()[0]);
            foreach (ExperiencePickup gem in gems) Near(18f, gem.MagnetSpeed, "흡입 속도");
            Near(0f, other.MagnetSpeed, "다른 씬");
            Equal(0, player.RestoreCalls, "회복 호출");
        });
        Test("폭탄 획득 시 화면 안 일반 몬스터만 처치하고 밖은 남김", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            pool.recycleCamera = NewCamera(0f, 0f, 5f, 2f); // 가로 ±10, 세로 ±5
            pool.FieldItems.bombViewMargin = .5f;
            EnemyHealth center = NewEnemy(pool, 3f, 2f), edge = NewEnemy(pool, 10.5f, -5.5f);
            EnemyHealth outsideX = NewEnemy(pool, 10.6f, 0f), outsideY = NewEnemy(pool, 0f, 5.6f);
            pool.TryDropFieldItem(FieldItemKind.Bomb, default);
            Frame(.016f); Tick(Items()[0]);
            Check(center.IsDead && edge.IsDead, "화면 안 생존");
            Check(!outsideX.IsDead && !outsideY.IsDead, "화면 밖 처치");
            Equal(2, pool.activeEnemies.Count, "남은 활성 수");
        });
        Test("폭탄으로 죽은 몬스터는 아이템을 떨어뜨리지 않고 이후 처치는 다시 떨어뜨림", () =>
        {
            NewPlayer(100f); SceneEnemyPool pool = NewPool(bomb: 1f);
            pool.recycleCamera = NewCamera();
            UnityEngine.Random.value = 0f;
            for (int i = 0; i < 6; i++) NewEnemy(pool, i);
            Equal(6, pool.DefeatEnemiesInView(default), "처치 수");
            Equal(0, Items().Count, "연쇄 드랍");
            NewEnemy(pool, 1f).Kill();
            Equal(1, Items().Count, "이후 드랍");
        });
        Test("폭탄 처리 중 대여가 바뀐 몬스터와 이미 죽은 몬스터는 건너뜀", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            pool.recycleCamera = NewCamera();
            EnemyHealth first = NewEnemy(pool, 1f), second = NewEnemy(pool, 2f), dead = NewEnemy(pool, 3f);
            dead.IsDead = true;
            var secondItem = second.GetComponent<PooledObject>();
            Action<EnemyHealth> original = first.Killed;
            first.Killed = enemy => { original(enemy); pool.activeEnemies[secondItem] = 99; };
            Equal(1, pool.DefeatEnemiesInView(default), "처치 수");
            Check(first.IsDead && !second.IsDead, "재대여된 몬스터 처치");
            Equal(0, second.KillCalls, "재대여된 몬스터에 처치 시도");
        });
        Test("화면을 판정할 카메라가 없으면 활성 일반 몬스터 전체를 처치", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            Camera.main = null;
            EnemyHealth near = NewEnemy(pool, 1f), far = NewEnemy(pool, 500f);
            Equal(2, pool.DefeatEnemiesInView(default), "처치 수");
            Check(near.IsDead && far.IsDead, "생존");
        });
        Test("풀이 쓰는 카메라가 없으면 MainCamera 화면으로 판정", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            Camera.main = NewCamera(100f, 0f, 5f, 2f);
            EnemyHealth visible = NewEnemy(pool, 105f), hidden = NewEnemy(pool, 0f);
            Equal(1, pool.DefeatEnemiesInView(default), "처치 수");
            Check(visible.IsDead && !hidden.IsDead, "판정");
        });
        Test("폭탄 파동은 화면 대각선까지 퍼진 뒤 사라짐", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            pool.recycleCamera = NewCamera(0f, 0f, 3f, 4f / 3f); // 가로 ±4, 세로 ±3 → 대각선 5
            pool.FieldItems.bombViewMargin = 0f;
            pool.DefeatEnemiesInView(new Vector3(1f, 2f, 0f));
            FieldItemBlastFx[] blasts = UnityEngine.Object.FindObjectsByType<FieldItemBlastFx>();
            Equal(1, blasts.Length, "파동 수");
            Near(1f, blasts[0].transform.position.x, "중심 x");
            Equal(1, blasts[0].gameObject.scene.id, "씬");
            SpriteRenderer ring = RingOf(blasts[0]);
            Frame(.34f, 0f); Tick(blasts[0]);
            Check(blasts[0] != null, "일찍 사라짐");
            float diameter = RingWorldDiameter(blasts[0]);
            Check(diameter > 9.9f && diameter <= 10.001f, "지름 " + diameter);
            Check(blasts[0].GetComponent<SpriteRenderer>() == null, "프레임이 없는데 폭발 그림을 만듦");
            Frame(.02f, 0f); Tick(blasts[0]);
            Check(blasts[0] == null, "남음");
            Check(ring == null, "고리가 남음");
        });
        Test("구운 고리 그림의 크기와 무관하게 파동 지름은 화면 대각선", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            Asset(FieldItemSprites.BlastRingName, 4f); // 128픽셀 ÷ 32 PPU
            pool.recycleCamera = NewCamera(0f, 0f, 3f, 4f / 3f);
            pool.FieldItems.bombViewMargin = 0f;
            pool.DefeatEnemiesInView(default);
            FieldItemBlastFx blast = UnityEngine.Object.FindObjectsByType<FieldItemBlastFx>()[0];
            Frame(.34f, 0f); Tick(blast);
            float diameter = RingWorldDiameter(blast);
            Check(diameter > 9.9f && diameter <= 10.001f, "지름 " + diameter);
        });
        Test("폭발 그림은 실제 시간 30fps로 한 번 재생되고 끝나면 고리와 함께 사라짐", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool();
            Sprite[] frames = BlastAssets(14);
            pool.recycleCamera = NewCamera();
            pool.DefeatEnemiesInView(new Vector3(3f, 4f, 0f));
            FieldItemBlastFx blast = UnityEngine.Object.FindObjectsByType<FieldItemBlastFx>()[0];
            SpriteRenderer burst = blast.GetComponent<SpriteRenderer>(), ring = RingOf(blast);
            Check(burst != null && ReferenceEquals(frames[0], burst.sprite), "첫 프레임");
            Near(3f, blast.transform.position.x, "중심 x");
            Check(burst.sortingOrder > ring.sortingOrder, "폭발이 고리 뒤에 그려짐");
            // 전투가 멈춘 상태(timeScale 0)에서도 진행합니다.
            Frame(.05f, 0f); Tick(blast);
            Check(ReferenceEquals(frames[1], burst.sprite), "0.05초");
            Frame(.15f, 0f); Tick(blast);
            Check(ReferenceEquals(frames[6], burst.sprite) && ring.enabled, "0.2초");
            Frame(.2f, 0f); Tick(blast);
            Check(blast != null, "고리 시간에 맞춰 일찍 사라짐");
            Check(ReferenceEquals(frames[12], burst.sprite) && burst.enabled, "0.4초");
            Check(!ring.enabled, "고리가 남아 보임");
            Frame(.05f, 0f); Tick(blast);
            Check(blast != null && ReferenceEquals(frames[13], burst.sprite), "마지막 프레임(0.45초)");
            Frame(.03f, 0f); Tick(blast);
            Check(blast == null, "재생 후 남음");
            Check(burst == null && ring == null, "자식이 남음");
        });
        Test("풀 정리 시 남은 아이템은 효과 없이 제거되고 종료 중에는 떨어지지 않음", () =>
        {
            PlayerData player = NewPlayer(); SceneEnemyPool pool = NewPool(heal: 1f);
            UnityEngine.Random.value = 0f;
            NewEnemy(pool, 0f).Kill(); NewEnemy(pool, 0f).Kill();
            pool.DiscardAll();
            Equal(0, Items().Count, "남은 아이템"); Equal(0, player.RestoreCalls, "회복 호출");
            Equal(0, pool.ActiveFieldItemCount, "집계");
            pool.closing = true;
            NewEnemy(pool, 0f).Kill();
            Equal(0, Items().Count, "종료 중 드랍");
            Equal(0, pool.DefeatEnemiesInView(default), "종료 중 폭탄");
        });
        Test("몬스터 스폰 때 기억한 플레이어가 획득 대상", () =>
        {
            PlayerData bystander = NewPlayer(0f); PlayerData tracked = NewPlayer(50f);
            SceneEnemyPool pool = NewPool();
            pool.RememberTarget(tracked.transform);
            pool.TryDropFieldItem(FieldItemKind.HealPack, new Vector3(0f, 0f, 0f));
            Frame(.016f); Tick(Items()[0]);
            Equal(0, bystander.RestoreCalls + tracked.RestoreCalls, "기억하지 않은 플레이어가 획득");
            tracked.transform.position = new Vector3(.5f, 0f, 0f);
            Tick(Items()[0]);
            Equal(1, tracked.RestoreCalls, "기억한 플레이어 획득"); Equal(0, bystander.RestoreCalls, "다른 플레이어");
        });
        Test("비활성 풀은 떨어뜨리지 않음", () =>
        {
            NewPlayer(); SceneEnemyPool pool = NewPool(heal: 1f);
            pool.enabled = false;
            Check(!pool.TryDropFieldItem(FieldItemKind.HealPack, default), "드랍됨");
            Check(!pool.TryDropFieldItem(FieldItemKind.None, default), "None 드랍됨");
        });

        Console.WriteLine($"{count - failures}/{count} 통과");
        return failures == 0 ? 0 : 1;
    }
}

// ── SceneEnemyPool의 나머지 절반과 게임 타입 대역 ──────────────────
public sealed partial class SceneEnemyPool : MonoBehaviour
{
    public Camera recycleCamera;
    public readonly Dictionary<PooledObject, uint> activeEnemies = new Dictionary<PooledObject, uint>();
    public bool closing;
    // 실제 TrySpawn이 스폰 대상(플레이어)을 기억하는 동작의 대역입니다.
    public void RememberTarget(Transform target) => fieldItemTarget = target;
    public void Defeat(EnemyHealth enemy) => HandleEnemyDefeated(enemy);
    public void DiscardAll() => DiscardFieldItems();
}

public class PooledObject : MonoBehaviour { }

public class EnemyHealth : MonoBehaviour
{
    public bool IsDead;
    public int KillCalls;
    public Action<EnemyHealth> Killed;
    public bool Kill()
    {
        KillCalls++;
        if (IsDead) return false;
        IsDead = true;
        Killed?.Invoke(this);
        return true;
    }
}

public class ExperiencePickup : MonoBehaviour
{
    public float MagnetSpeed;
    public bool BeginMagnetAttraction(float minimumSpeed) { MagnetSpeed = minimumSpeed; return true; }
}

public class PlayerData : MonoBehaviour
{
    public bool IsDead;
    public float maxHealth = 100f;
    public int RestoreCalls;
    public float Restored;
    public float RestoreHealth(float amount) { RestoreCalls++; Restored += amount; return amount; }
}

// ── Unity 대역 ─────────────────────────────────────────────────────
namespace UnityEngine
{
    public class Object
    {
        internal bool destroyed;
        internal static readonly List<Component> World = new List<Component>();
        public string name;

        public static void ResetWorld()
        {
            World.Clear();
            Resources.Assets.Clear();
            Resources.LoadCalls = 0;
            Camera.main = null;
            Random.value = 0f;
            Debug.Exceptions = 0;
            Time.timeScale = 1f; Time.deltaTime = 0f; Time.unscaledDeltaTime = 0f;
        }

        // 파괴된 오브젝트는 Unity처럼 null과 같다고 판정합니다.
        public static bool operator ==(Object a, Object b)
        {
            bool aMissing = a is null || a.destroyed, bMissing = b is null || b.destroyed;
            return aMissing || bMissing ? aMissing && bMissing : ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) => !(a == b);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

        public static void Destroy(Object target)
        {
            if (target is null) return;
            target.destroyed = true;
            if (target is GameObject gameObject)
            {
                foreach (Component component in gameObject.Components) component.destroyed = true;
                // 자식 오브젝트도 함께 파괴됩니다.
                foreach (Transform child in gameObject.transform.Children.ToArray()) Destroy(child.gameObject);
            }
        }

        public static T[] FindObjectsByType<T>() where T : Object
        {
            var found = new List<T>();
            foreach (Component component in World)
                if (!component.destroyed && component is T match) found.Add(match);
            return found.ToArray();
        }

        public static T FindAnyObjectByType<T>() where T : Object
        {
            T[] found = FindObjectsByType<T>();
            return found.Length > 0 ? found[0] : null;
        }
    }

    public class GameObject : Object
    {
        internal readonly List<Component> Components = new List<Component>();
        public Transform transform;
        public SceneManagement.Scene scene;

        public GameObject(string name)
        {
            this.name = name;
            transform = new Transform { gameObject = this };
            Components.Add(transform);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            var component = new T { gameObject = this };
            Components.Add(component);
            World.Add(component);
            return component;
        }
    }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class
        {
            foreach (Component component in gameObject.Components)
                if (!component.destroyed && component is T match) return match;
            return null;
        }
        public T GetComponentInParent<T>() where T : class => GetComponent<T>();
        public T GetComponentInChildren<T>() where T : class => GetComponent<T>();
        public bool TryGetComponent<T>(out T component) where T : class
        {
            component = GetComponent<T>();
            return component != null;
        }
    }

    public class Transform : Component
    {
        public Vector3 position;
        public Vector3 localScale = Vector3.one;
        public Transform parent;
        internal readonly List<Transform> Children = new List<Transform>();
        public void SetParent(Transform newParent, bool worldPositionStays)
        {
            if (!(parent is null)) parent.Children.Remove(this);
            parent = newParent;
            if (newParent is null) return;
            newParent.Children.Add(this);
            if (!worldPositionStays) position = newParent.position;
        }
    }

    public static class Resources
    {
        public static readonly Dictionary<string, Object> Assets = new Dictionary<string, Object>();
        public static int LoadCalls;
        public static T Load<T>(string path) where T : Object
        {
            LoadCalls++;
            return Assets.TryGetValue(path, out Object asset) ? asset as T : null;
        }
        public static T[] LoadAll<T>(string folder) where T : Object
        {
            var found = new List<T>();
            foreach (var entry in Assets)
                if (entry.Key.StartsWith(folder + "/", StringComparison.Ordinal) && entry.Value is T match) found.Add(match);
            return found.ToArray();
        }
    }

    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => !destroyed && enabled && !gameObject.destroyed;
    }

    public class MonoBehaviour : Behaviour { }

    public class Camera : Behaviour
    {
        public static Camera main;
        public bool orthographic;
        public float orthographicSize;
        public float aspect;
    }

    public class SpriteRenderer : Component
    {
        public Sprite sprite;
        public string sortingLayerName;
        public int sortingOrder;
        public Rendering.ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;
        public bool enabled = true;
        public Color color = new Color(1f, 1f, 1f, 1f);
    }

    public struct Bounds { public Vector3 size; }

    public class Sprite : Object
    {
        public Texture2D texture;
        public float pixelsPerUnit;
        public float width = 1f; // 유닛 단위
        public Bounds bounds => new Bounds { size = new Vector3(width, width, 0f) };
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude,
            SpriteMeshType meshType) => new Sprite { texture = texture, pixelsPerUnit = pixelsPerUnit, width = rect.width / pixelsPerUnit };
    }

    public class Texture2D : Object
    {
        public readonly int width, height;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        public Color32[] pixels;
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { this.width = width; this.height = height; }
        public void SetPixels32(Color32[] colors)
        {
            if (colors.Length != width * height) throw new ArgumentException("픽셀 수 불일치");
            pixels = colors;
        }
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
    }

    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear }
    public enum TextureWrapMode { Clamp }
    public enum SpriteMeshType { FullRect }

    public struct Rect { public float width; public Rect(float x, float y, float width, float height) { this.width = width; } }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 one => new Vector3(1f, 1f, 1f);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static class Mathf
    {
        public static float Sin(float value) => (float)Math.Sin(value);
        public static float Sqrt(float value) => (float)Math.Sqrt(value);
        public static float Abs(float value) => Math.Abs(value);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static int RoundToInt(float value) => (int)Math.Round(value);
    }

    public static class Time
    {
        public static float deltaTime, unscaledDeltaTime, timeScale = 1f;
    }

    public static class Random { public static float value; }

    public static class Debug
    {
        public static int Exceptions;
        public static void LogException(Exception exception, Object context) { Exceptions++; }
        public static void LogWarning(string message, Object context) { }
    }

    public class DisallowMultipleComponent : Attribute { }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public readonly int id;
        public Scene(int id) { this.id = id; }
        public static bool operator ==(Scene a, Scene b) => a.id == b.id;
        public static bool operator !=(Scene a, Scene b) => a.id != b.id;
        public override bool Equals(object other) => other is Scene scene && scene.id == id;
        public override int GetHashCode() => id;
    }

    public static class SceneManager
    {
        public static void MoveGameObjectToScene(GameObject gameObject, Scene scene) { gameObject.scene = scene; }
    }
}
