using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class LevelUpPopupTests
{
    [UnityTest]
    public IEnumerator ChoicesPreparedWhileHiddenDealWhenPopupOpens()
    {
        var popup = new GameObject("CombatLevelUpPopup", typeof(RectTransform), typeof(Canvas));
        float previousTimeScale = Time.timeScale;
        try
        {
            popup.SetActive(false);
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(popup.transform, false);
            var panel = popup.AddComponent(Type.GetType("LevelUpSelectionPanel, Assembly-CSharp", true));
            panel.GetType().GetMethod("Initialize").Invoke(panel, new object[] { content, null });
            Type cardType = Type.GetType("LevelUpCardData, Assembly-CSharp", true);
            Array choices = Array.CreateInstance(cardType, 2);
            for (int i = 0; i < choices.Length; i++)
                choices.SetValue(Activator.CreateInstance(cardType), i);

            int selected = -1;
            Time.timeScale = 0f;
            panel.GetType().GetMethod("SetChoices").Invoke(panel, new object[]
            {
                choices, new Action<int>(index => selected = index), 1, new Action<int>(_ => { }), -1
            });
            LogAssert.NoUnexpectedReceived();

            var card = content.Find("LevelUpSelection/Choice0").GetComponent<Button>();
            var reroll = content.Find("LevelUpSelection/Reroll0").GetComponent<Button>();
            popup.SetActive(true);
            Assert.That(card.interactable, Is.False);
            Assert.That(reroll.interactable, Is.False);

            float deadline = Time.realtimeSinceStartup + 3f;
            while (!card.interactable && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(card.interactable, Is.True);
            Assert.That(reroll.interactable, Is.True);
            card.onClick.Invoke();
            Assert.That(selected, Is.Zero);
        }
        finally
        {
            Time.timeScale = previousTimeScale;
            UnityEngine.Object.Destroy(popup);
        }
    }
}
