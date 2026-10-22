using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    private Vector3 moveDirection; // 발사될 직선 방향
    private float damage;
    private float speed;
    public float lifeTime = 3f;

    private IObjectPool<Bullet> pool;
    private Coroutine disableCoroutine;

    public void SetPool(IObjectPool<Bullet> pool)
    {
        this.pool = pool;
    }

    public void Init(Transform target, float damage, float speed)
    {
        this.damage = damage;
        this.speed = speed;

        // [중요] 타겟이 있다면 발사되는 순간의 방향을 '딱 한 번'만 계산
        if (target != null)
        {
            moveDirection = (target.position - transform.position).normalized;
        }
        else
        {
            moveDirection = transform.up; // 타겟이 없을 경우 기본 바라보는 방향
        }

        // 수명 제한 코루틴
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
        }
        disableCoroutine = StartCoroutine(DeactivateAfterTime());
    }

    private IEnumerator DeactivateAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        ReleaseToPool();
    }

    void Update()
    {
        // 실시간 target 추적이 아닌, 정해진 방향(moveDirection)으로 계속 직선 이동
        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }

            ReleaseToPool();
        }
    }

    private void ReleaseToPool()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }

        if (pool != null && gameObject.activeSelf)
        {
            pool.Release(this);
        }
    }
}