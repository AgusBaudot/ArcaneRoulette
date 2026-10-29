using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Renderer))]
    public class EnvironmentOccluder : MonoBehaviour, IOccluder
    {
        private Renderer _renderer;

        public Vector3 Position => transform.position; 
        public Renderer Renderer => _renderer;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            OcclusionRegistry.Register(this);
        }

        private void OnDisable()
        {
            OcclusionRegistry.Unregister(this);
        }
    }
}