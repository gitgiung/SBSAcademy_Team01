using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Define
{
    public enum EScene
    {
        Unknown,
        TitleTestScene,
        LoadTestScene,
        MainTestScene
    }//테스트용 씬 이름이므로 나중에 변경 필요

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
