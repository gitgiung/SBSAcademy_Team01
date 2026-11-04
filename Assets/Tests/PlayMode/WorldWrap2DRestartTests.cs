#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public class WorldWrap2DRestartTests
{
    private readonly List<Object> owned = new List<Object>();

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        for (int i = owned.Count - 1; i >= 0; i--)
        {
            if (owned[i] != null)
                Object.DestroyImmediate(owned[i]);
        }

        owned.Clear();

        GameObject leftoverRoot = GameObject.Find("WorldWrap Visuals");
        if (leftoverRoot != null)
            Object.DestroyImmediate(leftoverRoot);

        yield return null;
    }

    [UnityTest]
    public IEnumerator DisableAndRestartClearsWrapCopies()
    {
        GameObject player = CreateWrapFixture();

        // 8장 복제가 프레임마다 하나씩 끝나길 기다립니다.
        for (int i = 0; i < 12; i++)
            yield return null;

        Assert.That(CountWrapCopies(), Is.EqualTo(8));
        Assert.That(GameObject.Find("WorldWrap Visuals"), Is.Not.Null);

        // 씬 언로드·리스타트와 같이 비활성되면 사본이 즉시 사라져야 합니다.
        player.SetActive(false);
        yield return null;

        Assert.That(CountWrapCopies(), Is.EqualTo(0));
        Assert.That(GameObject.Find("WorldWrap Visuals"), Is.Null);

        player.SetActive(true);
        for (int i = 0; i < 12; i++)
            yield return null;

        Assert.That(CountWrapCopies(), Is.EqualTo(8));

        Object.DestroyImmediate(player);
        yield return null;

        Assert.That(CountWrapCopies(), Is.EqualTo(0));
        Assert.That(GameObject.Find("WorldWrap Visuals"), Is.Null);
    }

    private GameObject CreateWrapFixture()
    {
        GameObject gridObject = new GameObject("Ground");
        owned.Add(gridObject);
        gridObject.AddComponent<Grid>();

        GameObject tilemapObject = new GameObject("Tilemap");
        owned.Add(tilemapObject);
        tilemapObject.transform.SetParent(gridObject.transform);
        Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemapObject.AddComponent<TilemapRenderer>();

        Tile tile = ScriptableObject.CreateInstance<Tile>();
        owned.Add(tile);
        tile.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        owned.Add(tile.sprite);
        tilemap.SetTile(new Vector3Int(0, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        tilemap.SetTile(new Vector3Int(0, 1, 0), tile);
        tilemap.SetTile(new Vector3Int(1, 1, 0), tile);

        GameObject player = new GameObject("Player Target");
        owned.Add(player);
        player.SetActive(false);
        player.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        WorldWrap2D wrap = player.AddComponent<WorldWrap2D>();
        SetPrivate(wrap, "sourceTilemap", tilemap);
        SetPrivate(wrap, "boundsRect", new Rect(-1f, -1f, 2f, 2f));
        player.SetActive(true);
        return player;
    }

    private static int CountWrapCopies()
    {
        int count = 0;
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform root = transforms[i];
            if (root != null && root.name.Contains("(Wrap "))
                count++;
        }

        return count;
    }

    private static void SetPrivate(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(target, value);
    }
}
#endif
