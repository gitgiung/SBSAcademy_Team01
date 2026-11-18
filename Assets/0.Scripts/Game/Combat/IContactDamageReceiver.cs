using UnityEngine;

/// <summary>Receives damage caused by sustained contact with a gameplay entity.</summary>
public interface IContactDamageReceiver
{
    void TakeContactDamage(float damage, GameObject source);
}

public interface IContactDamageResultReceiver : IContactDamageReceiver
{
    /// <summary>Returns true if this attack dealt damage or triggered a successful parry.</summary>
    bool TryDamageOrTriggerParry(float damage, GameObject source);
}
