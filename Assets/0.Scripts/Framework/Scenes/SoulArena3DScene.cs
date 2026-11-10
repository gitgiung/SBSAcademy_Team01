using UnityEngine;

/// <summary>SoulArena3D용 BaseScene. Managers.Scene 정리와 호환.</summary>
public sealed class SoulArena3DScene : BaseScene
{
    public override bool Init()
    {
        if (base.Init() == false)
            return false;

        SceneType = Define.EScene.SoulArena3D;
        return true;
    }

    public override void Clear()
    {
        // 일시정지(P) 후 씬 이탈 시 다음 씬이 멈춘 채로 남지 않게.
        Time.timeScale = 1f;
    }
}
