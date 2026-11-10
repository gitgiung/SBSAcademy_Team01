using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Define
{
    public enum EScene
    {
        Unknown,
        TitleScene,
        LoadScene,
        MainScene,
        GiungScene,
        LobbyScene,
        /// <summary>본선 3D 소울 파이프라인 검증 씬. Main 합류 전 병행.</summary>
        SoulArena3D
    }

    public enum EUIEvent
    {
        Click,
        PointerDown,
        PointerUp,
        Drag,
    }

    public enum ESound
    {
        Bgm,
        Effect,
        Max,
    }

    public enum ELanguage
    {
        Korean,
        English,
        French,
        SimplifiedChinese,
        TraditionalChinese,
        Japanese
    }

    public enum EEffectSize
    {
        None,
    }
}
