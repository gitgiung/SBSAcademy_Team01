using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float speed;
    public float lifeTime = 3f;

    public void Init(Transform target, float damage, float speed) // 총알 초기화
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;

        Destroy(gameObject, lifeTime);
    }

    void Update() // 총알 이동
    {
        if (target == null) return;

        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision) // 총알이 적과 충돌했을 때
    {
        Debug.Log($"[Trigger 감지] 부딪힌 대상: {collision.gameObject.name} / 레이어: {LayerMask.LayerToName(collision.gameObject.layer)}");

        if (collision.gameObject.layer == LayerMask.NameToLayer("enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}