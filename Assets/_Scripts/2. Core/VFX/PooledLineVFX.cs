using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PooledLineVFX : MonoBehaviour, IPoolable
    {
        public LineRenderer Line { get; private set; }

        private void Awake()
        {
            Line = GetComponent<LineRenderer>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
        }

        public void OnDespawn()
        {
            transform.SetParent(null);
        }
    }
}