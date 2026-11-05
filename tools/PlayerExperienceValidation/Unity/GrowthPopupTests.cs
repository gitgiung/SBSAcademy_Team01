using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class GrowthPopupTests
{
    GameObject root;
    PlayerData data;
    CombatPopupController controller;
    UI_CombatPopup level;
    UI_CombatPopup pause;
    static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static void Call(object target,string method) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    static int Pending(PlayerData d) => (int)typeof(PlayerData).GetProperty("PendingLevelUps").GetValue(d);
    static Button NamedButton(Component owner,string name) => owner.GetComponentsInChildren<Button>(true).First(b=>b.name==name);
    UI_CombatPopup Popup(string name)
    {
        var go = new GameObject(name,typeof(RectTransform)); go.SetActive(false);
        var panel = new GameObject("Panel",typeof(RectTransform)); panel.transform.SetParent(go.transform,false);
        var content = new GameObject("Content",typeof(RectTransform)); content.transform.SetParent(panel.transform,false);
        var back = new GameObject("Back",typeof(RectTransform),typeof(Image),typeof(Button)); back.transform.SetParent(panel.transform,false);
        var p = go.AddComponent<UI_CombatPopup>(); Set(p,"contentRoot",content.transform); Set(p,"closeButton",back.GetComponent<Button>());
        return p;
    }
    [SetUp] public void Setup()
    {
        Time.timeScale=1;
        Managers.UI=new UIManager();
        // 추첨/획득도 실제 구현을 실행하되 표 입력만 고정합니다.
        var skills=Enumerable.Range(10001,10).ToDictionary(i=>i,i=>new CombatSkillRecord{Id=i,Name="검증 스킬",Rarity="Common"});
        typeof(CombatSkillTable).GetField("skills",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,skills);
        typeof(CombatSkillTable).GetField("rarities",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,
            new System.Collections.Generic.List<SkillRarityRecord>{new SkillRarityRecord{Id="Common",Weight=1}});
        root=new GameObject("Fixture"); root.SetActive(false);
        data=root.AddComponent<PlayerData>(); var player=root.AddComponent<Player>(); player.data=data;
        controller=root.AddComponent<CombatPopupController>();
        var game=root.AddComponent<GameManager>(); Set(controller,"gameManager",game);
        level=Popup("Level"); pause=Popup("Pause");
        var pauseButton=new GameObject("PauseButton",typeof(RectTransform),typeof(Button)); pauseButton.transform.SetParent(root.transform);
        Set(controller,"pauseButton",pauseButton.GetComponent<Button>()); Set(controller,"pausePopup",pause); Set(controller,"levelUpPopup",level); Set(controller,"statusPlayer",player);
        root.SetActive(true);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Object.Destroy(root); Managers.UI.Clear();
        var uiRoot=GameObject.Find("@UI_Root"); if(uiRoot!=null) Object.Destroy(uiRoot);
        yield return null; Time.timeScale=1;
    }
    [UnityTest] public IEnumerator XPAutomaticallyOpensAndAwardsExistingCard()
    {
        yield return null; data.AddEXP(13);
        Assert.That(level.gameObject.activeSelf,Is.True,"실제 XP 획득으로 자동 표시"); Assert.That(Time.timeScale,Is.Zero);
        NamedButton(level,"Choice0").onClick.Invoke();
        yield return null; yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(data.Loadout.GetAttackSkill(0),Is.Not.Null);
        Assert.That(data.Loadout.GetEquippedSkillId(PlayerSkillButton.Light),Is.Zero);
        Assert.That(level.gameObject.activeSelf,Is.False); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator MultipleChoicesKeepPauseAndRejectSameFrameDuplicate()
    {
        yield return null; data.AddEXP(30);
        Assert.That(level.gameObject.activeSelf,Is.True);
        var button=NamedButton(level,"Choice0"); button.onClick.Invoke(); button.onClick.Invoke();
        Assert.That(Pending(data),Is.EqualTo(1)); Assert.That(Time.timeScale,Is.Zero);
        yield return null; yield return null;
        Assert.That(level.gameObject.activeSelf,Is.True); Assert.That(Time.timeScale,Is.Zero);
        NamedButton(level,"Choice0").onClick.Invoke(); yield return null; yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(Time.timeScale,Is.EqualTo(1));
        Assert.That(data.Loadout.GetAttackSkill(1),Is.Not.Null);
    }
    [UnityTest] public IEnumerator FullSlotsConfirmConsumesOneChoiceAtATime()
    {
        yield return null;
        for(int i=0;i<6;i++) data.Loadout.TryAddAttackSkill(new Data.SkillData{DataId=11000+i,Name="보유"});
        data.AddEXP(30); Assert.That(level.gameObject.activeSelf,Is.True);
        var confirm=NamedButton(level,"Confirm"); confirm.onClick.Invoke(); confirm.onClick.Invoke();
        Assert.That(Pending(data),Is.EqualTo(1));
        yield return null; yield return null;
        confirm.onClick.Invoke(); yield return null; yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator ExhaustedCardsConfirmCompletesAutomaticChoice()
    {
        yield return null;
        typeof(CombatSkillTable).GetField("skills",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,new System.Collections.Generic.Dictionary<int,CombatSkillRecord>());
        data.AddEXP(13); Assert.That(level.gameObject.activeSelf,Is.True);
        NamedButton(level,"Confirm").onClick.Invoke(); yield return null; yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator PauseBelowLevelUpRemainsPausedUntilBack()
    {
        yield return null; controller.ShowPause(); data.AddEXP(13);
        Assert.That(level.gameObject.activeSelf,Is.True);
        NamedButton(level,"Choice0").onClick.Invoke(); yield return null; yield return null;
        Assert.That(pause.gameObject.activeSelf,Is.True); Assert.That(Time.timeScale,Is.Zero);
        pause.ClosePopupUI(); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator DisableAndReenablePreservesUnfinishedChoice()
    {
        yield return null; data.AddEXP(30); controller.enabled=false;
        Assert.That(Pending(data),Is.EqualTo(2)); Assert.That(level.gameObject.activeSelf,Is.False);
        controller.enabled=true; yield return null;
        Assert.That(level.gameObject.activeSelf,Is.True); Assert.That(Time.timeScale,Is.Zero);
    }
    [UnityTest] public IEnumerator DeathCancelsPendingAndDoesNotResumeCombat()
    {
        yield return null; data.AddEXP(30); data.ApplyDamage(100);
        yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(level.gameObject.activeSelf,Is.False); Assert.That(Time.timeScale,Is.Zero);
    }
    [UnityTest] public IEnumerator StatusHeadingShowsBlackCurrentLevelAndRefreshes()
    {
        yield return null; controller.ShowPause();
        var text=pause.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.name=="PlayerLevel");
        Assert.That(text,Is.Not.Null,"스탯 제목 오른쪽 레벨 표시"); Assert.That(text.text,Is.EqualTo("Lv.1")); Assert.That(text.color,Is.EqualTo(Color.black));
        data.AddEXP(13); Assert.That(text.text,Is.EqualTo("Lv.2"));
    }
    [UnityTest] public IEnumerator ManualPreviewDoesNotConsumeGrowthAndCloseCannotSkipAutomaticChoice()
    {
        yield return null; controller.ShowLevelUp(); controller.CompleteLevelUp(); Assert.That(Pending(data),Is.Zero);
        data.AddEXP(13); Assert.That(level.gameObject.activeSelf,Is.True);
        level.ClosePopupUI(); Assert.That(level.gameObject.activeSelf,Is.True); Assert.That(Pending(data),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator LevelUpBelowPauseCanFinishWithoutLeavingStalePopup()
    {
        yield return null; data.AddEXP(13); controller.ShowPause();
        NamedButton(level,"Choice0").onClick.Invoke(); yield return null; yield return null;
        Assert.That(level.gameObject.activeSelf,Is.False,"다른 팝업 아래에서도 완료된 선택은 정리");
        Assert.That(Time.timeScale,Is.Zero); pause.ClosePopupUI(); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator VictoryCancelsChoicesAndStopsAdvancing()
    {
        yield return null; data.AddEXP(30); NamedButton(level,"Choice0").onClick.Invoke();
        root.GetComponent<GameManager>().Finish(GameState.Victory); yield return null; yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(level.gameObject.activeSelf,Is.False); Assert.That(Time.timeScale,Is.Zero);
    }
    [UnityTest] public IEnumerator ResetWhileChoosingClosesPopupAndStartsAtLevelOne()
    {
        yield return null; data.AddEXP(30); data.InitEXP(); yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(level.gameObject.activeSelf,Is.False); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator VictoryWhileDisabledCancelsChoicesOnReenable()
    {
        yield return null; data.AddEXP(30); controller.enabled=false;
        root.GetComponent<GameManager>().Finish(GameState.Victory); controller.enabled=true; yield return null;
        Assert.That(Pending(data),Is.Zero); Assert.That(level.gameObject.activeSelf,Is.False); Assert.That(Time.timeScale,Is.Zero);
    }
    [UnityTest] public IEnumerator ExistingPlayerViewSliderUsesRemainderAndNewRequirement()
    {
        yield return null;
        var go=new GameObject("ExpSlider",typeof(RectTransform),typeof(Slider)); go.transform.SetParent(root.transform);
        var slider=go.GetComponent<Slider>(); slider.minValue=0; slider.maxValue=1;
        var view=new PlayerView(root.transform,null,null,data,null); view.BindResourceSliders(null,slider);
        data.AddEXP(12); Assert.That(slider.value,Is.EqualTo(12f/13).Within(.0001f));
        data.AddEXP(3); Assert.That(slider.value,Is.EqualTo(2f/15).Within(.0001f)); view.Dispose();
    }
    [UnityTest] public IEnumerator DestroyControllerRemovesReparentedPopupsAndRestoresPause()
    {
        yield return null; data.AddEXP(30); Object.Destroy(root);
        yield return null; yield return null;
        Assert.That(level==null,Is.True); Assert.That(pause==null,Is.True);
        Assert.That(Managers.UI.GetPopupCount(),Is.Zero); Assert.That(Time.timeScale,Is.EqualTo(1));
    }
}
