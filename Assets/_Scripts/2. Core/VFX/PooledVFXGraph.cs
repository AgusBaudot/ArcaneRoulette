using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using Foundation;

namespace Core 
{
    [RequireComponent(typeof(VisualEffect))]
    public class PooledVFXGraph : MonoBehaviour, IPoolable
    {
        [Tooltip("How long before this effect returns to the pool?")]
        [SerializeField] private float lifetime = 2f;
        
        private VisualEffect _vfx;

        private void Awake()
        {
            _vfx = GetComponent<VisualEffect>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            _vfx.Play();
            StartCoroutine(DespawnRoutine());
        }

        public void OnDespawn()
        {
            _vfx.Stop();
            StopAllCoroutines(); 
        }

        private IEnumerator DespawnRoutine()
        {
            yield return CoroutineUtils.GetWait(lifetime);
            
            Helpers.ProjFactory.Despawn(gameObject);
        }
    }
}