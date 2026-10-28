using UnityEngine;

namespace PoolingExamples
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ExampleBullet2D : MonoBehaviour, IPoolable
    {
        private Rigidbody2D _body;
        private PooledObject _pooled;
        private int _damage;
        private float _remainingLife;
        private Vector2 _velocity;
        private LayerMask _targetLayers;

        private void CacheReferences()
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();
            if (_pooled == null) _pooled = GetComponent<PooledObject>();
        }

        // Pop의 initialize에서 호출합니다. 활성화 전에 이번 발사 데이터를 모두 설정합니다.
        public void Initialize(Vector2 direction, float speed, int damage,
            float lifetime, LayerMask targetLayers)
        {
            CacheReferences();
            _velocity = direction.normalized * Mathf.Max(0f, speed);
            _damage = Mathf.Max(0, damage);
            _remainingLife = Mathf.Max(0f, lifetime);
            _targetLayers = targetLayers;
        }

        public void OnPoolRent()
        {
            CacheReferences();
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _damage = 0;
            _remainingLife = 0f;
            _velocity = Vector2.zero;
            _targetLayers = 0;
        }

        private void OnEnable()
        {
            CacheReferences();
            _body.linearVelocity = _velocity;
        }

        private void Update()
        {
            if (_pooled == null || !_pooled.IsRented) return;
            _remainingLife -= Time.deltaTime;
            if (_remainingLife <= 0f) _pooled.ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_pooled == null || !_pooled.IsRented) return;
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
            IDamageable2D target = other.GetComponentInParent<IDamageable2D>();
            if (target == null) return;
            uint lease = _pooled.LeaseVersion;
            try { target.TakeDamage(_damage); }
            finally
            {
                if (_pooled != null) _pooled.ReturnToPool(lease);
            }
        }

        public void OnPoolReturn()
        {
            CacheReferences();
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            StopAllCoroutines();
            CancelInvoke();
        }
    }
}
