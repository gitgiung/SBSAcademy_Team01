using UnityEngine;

namespace PoolingExamples
{
    // 통합 예제: 별도 풀을 생성하지 않고 Managers의 공통 풀을 사용합니다.
    public sealed class ExamplePoolOwner : MonoBehaviour
    {
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private Transform player;
        [SerializeField] private LayerMask monsterLayers;

        public PoolManager Pools => Managers.Pool;

        private void Awake()
        {
            if (bulletPrefab != null) Pools.Prewarm(bulletPrefab, 32, 128);
            if (monsterPrefab != null) Pools.Prewarm(monsterPrefab, 16, 64);
        }

        // 입력 시스템에서 호출하세요. 이 예제는 Input Manager에 의존하지 않습니다.
        public void Fire(Vector2 direction)
        {
            if (bulletPrefab == null || player == null) return;
            Pools.Pop(bulletPrefab, player.position, Quaternion.identity,
                initialize: go => go.GetComponent<ExampleBullet2D>()
                    .Initialize(direction, 12f, 10, 3f, monsterLayers));
        }

        public void SpawnMonster(Vector3 position)
        {
            if (monsterPrefab == null || player == null) return;
            // 활성 몬스터 제한은 스포너가 담당합니다. maxSize와 다른 개념입니다.
            if (Pools.GetActiveCount(monsterPrefab) >= 60) return;
            Pools.Pop(monsterPrefab, position, Quaternion.identity,
                initialize: go => go.GetComponent<ExampleMonster2D>()
                    .Initialize(30, 1.5f, player));
        }

        // 이 컴포넌트는 풀 소유자가 아닙니다. Clear/Dispose는 Managers가 담당합니다.
    }
}
