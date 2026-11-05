using NUnit.Framework;
using UnityEngine;

public sealed class GroundShadowTests
{
    [Test]
    public void AttackSpriteDoesNotResizeOrMoveShadow()
    {
        var bodyObject = new GameObject("Shadow Fixed Size Test");
        var texture = new Texture2D(16, 16);
        var idle = Sprite.Create(texture, new Rect(0, 0, 4, 8), Vector2.one * 0.5f, 4);
        var attack = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f, 4);
        try
        {
            var body = bodyObject.AddComponent<SpriteRenderer>();
            body.sprite = idle;
            var shadow = GroundShadow.Attach(body);
            Vector3 scale = shadow.transform.localScale;
            Vector3 offset = shadow.transform.localPosition;
            body.sprite = attack;
            bodyObject.transform.position = new Vector3(5, 3, 0);
            shadow.SendMessage("LateUpdate");
            Assert.AreEqual(scale, shadow.transform.localScale, "Attack must preserve shadow size.");
            Assert.AreEqual(offset, shadow.transform.localPosition, "Attack must preserve foot offset.");
            Assert.AreEqual(bodyObject.transform.TransformPoint(offset), shadow.transform.position);
            GroundShadow.Attach(body);
            Assert.AreEqual(scale, shadow.transform.localScale, "Reattachment must preserve size.");
        }
        finally
        {
            Object.DestroyImmediate(bodyObject);
            Object.DestroyImmediate(idle);
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void FootShadowSitsAcrossTheBottomOfTheBody()
    {
        Bounds body = new Bounds(new Vector3(2f, 5f, 0f), new Vector3(1f, 2f, 0.2f));

        GroundShadow.GetFootShadow(body, out Vector2 size, out Vector3 center);

        Assert.AreEqual(0.78f, size.x, 0.0001f);
        Assert.AreEqual(0.2496f, size.y, 0.0001f);
        Assert.AreEqual(2f, center.x, 0.0001f);
        Assert.AreEqual(4.1248f, center.y, 0.0001f);
        Assert.AreEqual(0f, center.z, 0.0001f);
    }

    [Test]
    public void AttachPlacesShadowJustUnderTheBody()
    {
        var bodyObject = new GameObject("Body");
        try
        {
            var body = bodyObject.AddComponent<SpriteRenderer>();
            var texture = new Texture2D(4, 8, TextureFormat.RGBA32, false);
            texture.SetPixel(1, 1, Color.white);
            texture.Apply();
            body.sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 8f), new Vector2(0.5f, 0.5f), 4f);
            body.sortingOrder = 3;

            GroundShadow.Attach(body);

            Transform child = bodyObject.transform.Find("Ground Shadow");
            Assert.NotNull(child);
            var shadow = child.GetComponent<SpriteRenderer>();
            Assert.IsTrue(GroundShadow.IsShadow(shadow));
            Assert.IsFalse(GroundShadow.IsShadow(body));
            Assert.AreEqual(-2, shadow.sortingOrder);
            Assert.AreEqual(body.sortingLayerID, shadow.sortingLayerID);
            Assert.IsTrue(shadow.enabled);
            Assert.AreEqual(body.bounds.center.x, child.position.x, 0.001f);
            Assert.Less(child.position.y, body.bounds.center.y);
        }
        finally
        {
            Object.DestroyImmediate(bodyObject);
        }
    }
}
