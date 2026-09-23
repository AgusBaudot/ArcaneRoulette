using System.Collections;
using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledVFX : MonoBehaviour, IPoolable
    {
        private ParticleSystem _particleSystem;
        private Coroutine _despawnRoutine;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            
            _particleSystem.Play(true);
            _despawnRoutine = StartCoroutine(WaitAndDespawn());
        }

        public void OnDespawn()
        {
            // Clear mutable state per IPoolable protocol
            if (_despawnRoutine != null)
            {
                StopCoroutine(_despawnRoutine);
                _despawnRoutine = null;
            }

            // Force stop and clear to prevent ghost particles on next spawn
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.SetParent(null);
        }

        public void AttachTo(Transform target)
        {
            transform.SetParent(target);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        // Called by DashAbilityRune when the physical dash ends
        public void StopEmitting()
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private IEnumerator WaitAndDespawn()
        {
            // Wait for particles to naturally die off after StopEmitting is called
            while (_particleSystem.IsAlive(true))
            {
                // Check every 0.1s instead of every frame to save overhead
                yield return CoroutineUtils.GetWait(0.1f); 
            }
            
            // Decoupled architecture: ask factory to despawn us
            Helpers.ProjFactory.Despawn(gameObject);
        }
    }
}