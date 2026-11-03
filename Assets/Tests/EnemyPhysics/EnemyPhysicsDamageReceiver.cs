using UnityEngine;

public sealed class EnemyPhysicsDamageReceiver : MonoBehaviour, IContactDamageReceiver
{
    public int Hits { get; private set; }
    public float Damage { get; private set; }

    public void TakeContactDamage(float damage, GameObject source)
    {
        Hits++;
        Damage += damage;
    }
}
