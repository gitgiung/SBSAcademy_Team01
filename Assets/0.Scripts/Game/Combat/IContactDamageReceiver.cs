using UnityEngine;

/// <summary>Receives damage caused by sustained contact with a gameplay entity.</summary>
public interface IContactDamageReceiver
{
    void TakeContactDamage(float damage, GameObject source);
}
