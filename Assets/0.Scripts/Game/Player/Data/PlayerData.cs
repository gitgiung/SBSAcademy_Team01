using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [Header("플레이어 스탯 설정")]
    public float speed = 5f;
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("공격 설정")]
    public float attackRange = 5f;
    public float attackCooldown = 1f;
    public float attackDamage = 10f;
    public float bulletSpeed = 10f;

    private void Awake()
    {
        InitHealth();
    }


    public void InitHealth()
    {
        // maxHealth가 0 이하로 설정되어 있어도 최소 100으로 안전 보장
        if (maxHealth <= 0f) maxHealth = 100f;
        currentHealth = maxHealth;
    }
}