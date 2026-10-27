using System;
using UnityEngine;

public class ButtonForEvent : MonoBehaviour
{
    public event Action PlayerDeath;
    public event Action BossDefeated;

    public void TriggerDeath()
    { 
        PlayerDeath?.Invoke();
    }

    public void TriggerBossDefeated()
    {
        BossDefeated?.Invoke();
    }
}
