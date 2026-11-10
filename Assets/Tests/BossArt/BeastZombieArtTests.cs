using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BeastZombieArtTests
{
    private const string Art = "Assets/2.Resources/Art/Beast_Zombie_Boss";

    [Test]
    public void EveryClipKeyResolvesToTheMainSprite()
    {
        var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { Art + "/Animator/Clips" });
        Assert.That(clips, Is.Not.Empty);

        foreach (string guid in clips)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .Single(curve => curve.propertyName == "m_Sprite");
            foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
            {
                string frame = $"{clip.name} at {key.time:0.###}s";
                var sprite = key.value as Sprite;
                Assert.That(sprite != null, Is.True, frame);
                var mainSprite = MainSprite(AssetDatabase.GetAssetPath(sprite));
                Assert.That(sprite, Is.EqualTo(mainSprite), frame);
            }
        }
    }

    [TestCase("body-charge/east/0")]
    [TestCase("body-charge/east/1")]
    [TestCase("body-charge/east/4")]
    [TestCase("body-charge/west/4")]
    [TestCase("swipe/north-east/3")]
    public void SpriteRectContainsTheSourcePixels(string frame)
    {
        string path = $"{Art}/Idle/animations/{frame}.png";
        Rect rect = MainSprite(path).rect;
        var source = new Texture2D(2, 2);
        try
        {
            Assert.That(source.LoadImage(File.ReadAllBytes(path)), Is.True, frame);
            Color32[] pixels = source.GetPixels32();
            int outside = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a != 0 && !rect.Contains(new Vector2(i % source.width, i / source.width)))
                    outside++;
            }
            Assert.That(outside, Is.Zero, frame);
        }
        finally
        {
            Object.DestroyImmediate(source);
        }
    }

    [TestCase("SwipeEastWestFullFrame", "swipe", "east")]
    [TestCase("SwipeEastWestFullFrame", "swipe", "west")]
    [TestCase("SwipeNorthEastFullFrame", "swipe", "north-east")]
    [TestCase("SwipeLeftEastNorthEast", "swipe-left", "east")]
    [TestCase("SwipeLeftEastNorthEast", "swipe-left", "north-east")]
    [TestCase("SwipeLeftWestFullFrame", "swipe-left", "west")]
    public void FullFrameCopiesMatchTheSourceArt(string group, string animation, string direction)
    {
        for (int i = 0; i < 8; i++)
        {
            string original = $"{Art}/Idle/animations/{animation}/{direction}/{i}.png";
            string copy = $"{Art}/Animator/FrameSources/{group}/{direction}-{i}.png";
            Assert.That(File.ReadAllBytes(copy), Is.EqualTo(File.ReadAllBytes(original)), copy);
        }
    }

    private static Sprite MainSprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height).First();
    }
}
