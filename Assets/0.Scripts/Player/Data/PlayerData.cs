using UnityEngine;

public class PlayerData : MonoBehaviour // 플레이어의 데이터
{
    public float speed = 5f;
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    public float attackRange = 5f;
    public float attackCooldown = 1f;
    public float attackDamage = 10f;
    public float bulletSpeed = 3f;

    void Awake()
    {
        currentHealth = maxHealth;
    }
}