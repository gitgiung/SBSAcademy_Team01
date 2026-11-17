using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BeastZombieArtTests
{
    private const string Art = "Assets/2.Resources/Art/Beast_Zombie_Boss";

    [Test]
    public void SwipeBothHasSixteenFramesWithoutChangingDamageTiming()
    {
        foreach (string direction in new[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{Art}/Animator/Clips/BossSwipeBoth_{direction}.anim");
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .Single(curve => curve.propertyName == "m_Sprite");
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            Assert.That(clip.frameRate, Is.EqualTo(24), direction);
            Assert.That(keys.Select(key => key.value).Distinct().Count(), Is.EqualTo(16), direction);
            Assert.That(clip.length, Is.EqualTo(2f / 3f).Within(0.00001f), direction);
            var damage = AnimationUtility.GetAnimationEvents(clip)
                .Single(evt => evt.functionName == "AnimationEvent_ApplyAreaDamage");
            Assert.That(damage.time, Is.EqualTo(0.5833333f).Within(0.00001f), direction);
            Assert.That(keys[14].time, Is.EqualTo(damage.time).Within(0.00001f), direction);
            Assert.That(keys.All(key => key.value != null), Is.True, direction);
        }
    }

    [Test]
    public void PhaseTwoRunUsesCorrectedRunInEveryDirection()
    {
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
            Art + "/Animator/Boss.controller");
        var run = controller.layers[0].stateMachine.states.Single(state => state.state.name == "BossRun").state;
        var tree = run.motion as UnityEditor.Animations.BlendTree;
        Assert.That(tree, Is.Not.Null);
        Assert.That(tree.children.Length, Is.EqualTo(8));
        foreach (var child in tree.children)
        {
            string direction = child.position.y > 0.5f ? "North" : child.position.y < -0.5f ? "South" : "";
            direction += child.position.x > 0.5f ? "East" : child.position.x < -0.5f ? "West" : "";
            var expected = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                Art + "/Animator/Clips/BossRun_" + direction + ".anim");
            Assert.That(expected, Is.Not.Null, direction);
            Assert.That(child.motion, Is.EqualTo(expected), direction);
        }
    }

    [Test]
    public void RunFramesUseDedicatedArtWithStableFullCanvasRects()
    {
        foreach (string direction in new[] { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" })
        for (int i = 0; i < 8; i++)
        {
            string run = $"{Art}/Idle/animations/run/{direction}/{i}.png";
            string sprint = $"{Art}/Idle/animations/sprint/{direction}/{i}.png";
            Assert.That(File.ReadAllBytes(run), Is.Not.EqualTo(File.ReadAllBytes(sprint)), run);
            Assert.That(MainSprite(run).rect, Is.EqualTo(new Rect(0, 0, 132, 132)), run);
        }
    }

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
