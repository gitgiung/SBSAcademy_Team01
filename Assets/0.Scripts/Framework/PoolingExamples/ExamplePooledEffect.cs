using UnityEngine;

namespace PoolingExamples
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ExamplePooledEffect : MonoBehaviour, IPoolable
    {
        private ParticleSystem _particles;
        private PooledObject _pooled;

        private void CacheReferences()
        {
            if (_particles == null) _particles = GetComponent<ParticleSystem>();
            if (_pooled == null) _pooled = GetComponent<PooledObject>();
        }

        public void OnPoolRent()
        {
            CacheReferences();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnEnable()
        {
            CacheReferences();
            _particles.Play(true);
        }

        private void Update()
        {
            if (_pooled != null && _pooled.IsRented && !_particles.IsAlive(true))
                _pooled.ReturnToPool();
        }

        public void OnPoolReturn()
        {
            CacheReferences();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
