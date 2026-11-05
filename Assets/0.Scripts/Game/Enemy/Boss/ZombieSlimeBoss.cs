using UnityEngine;

/// <summary>
/// 3라운드 좀비 슬라임. 플레이어에게 다가가 닿으면 피해를 준다.
/// 체력, 접촉 피해, 이동 속도는 기존 보스 프리팹의 임시 값이다. 슬라임만의 수치는 정하지 않았다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyFollow), typeof(EnemyHealth), typeof(Rigidbody2D))]
public sealed class ZombieSlimeBoss : MonoBehaviour
{
    private EnemyHealth health;
    private EnemyFollow follow;

    public EnemyHealth Health
    {
        get
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();
            return health;
        }
    }

    private void Awake()
    {
        follow = GetComponent<EnemyFollow>();
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (health != null && health.IsDead)
        {
            enabled = false;
            return;
        }

        if (follow == null)
            return;

        Transform player = FindPlayer();
        if (player != null)
            follow.SetTarget(player);
    }

    private Transform FindPlayer()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IContactDamageReceiver && behaviour.gameObject.scene == gameObject.scene)
                return behaviour.transform;
        }

        return null;
    }
}
