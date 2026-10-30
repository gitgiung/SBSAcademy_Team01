#if UNITY_EDITOR
// Lives outside the test asmdef so it can exercise the existing Assembly-CSharp BaseScene.
public sealed class SceneCleanupProbe : BaseScene
{
    public int ClearCount { get; private set; }
    public bool Reenter { get; set; }
    public bool ThrowOnClear { get; set; }
    public static int TotalClearCount { get; set; }
    public override void Clear()
    {
        ClearCount++;
        TotalClearCount++;
        if (Reenter && ClearCount == 1)
            Managers.Clear();
        if (ThrowOnClear)
            throw new System.InvalidOperationException("probe cleanup failure");
    }
}
#endif
