using System;
using UnityEngine;

namespace PoolingExamples
{
    // 풀 연동을 보여주는 최소 예제입니다. 기존 MVC/상태 머신을 대체하지 않습니다.
    public sealed class ExampleMonster2D : MonoBehaviour, IPoolable, IDamageable2D
    {
        private PooledObject _pooled;
        private Transform _target;
        private float _moveSpeed;
        private bool _dead;

        public int CurrentHP { get; private set; }
        // 보상 처리는 구독자가 담당합니다. 반납 정리에서는 이 이벤트를 호출하지 않습니다.
        public event Action<Vector3> Died;

        public void Initialize(int maxHP, float speed, Transform target)
        {
            CurrentHP = Mathf.Max(1, maxHP);
            _moveSpeed = Mathf.Max(0f, speed);
            _target = target;
        }

        public void OnPoolRent()
        {
            if (_pooled == null) _pooled = GetComponent<PooledObject>();
            CurrentHP = 0;
            _moveSpeed = 0f;
            _target = null;
            _dead = false;
            Died = null;
        }

        private void Update()
        {
            if (_pooled == null || !_pooled.IsRented || _dead || _target == null) return;
            transform.position = Vector3.MoveTowards(transform.position,
                _target.position, _moveSpeed * Time.deltaTime);
        }

        public void TakeDamage(int damage)
        {
            if (_pooled == null || !_pooled.IsRented || _dead || damage <= 0) return;
            CurrentHP = Mathf.Max(0, CurrentHP - damage);
            if (CurrentHP > 0) return;
            _dead = true;
            uint lease = _pooled.LeaseVersion;
            try { Died?.Invoke(transform.position); }
            finally
            {
                if (_pooled != null) _pooled.ReturnToPool(lease);
            }
        }

        public void OnPoolReturn()
        {
            _target = null;
            Died = null;
            StopAllCoroutines();
            CancelInvoke();
            // 실제 구현에서는 피격 Tween, 넉백, 애니메이터, 상태 머신도 여기서 정리합니다.
        }
    }
}
