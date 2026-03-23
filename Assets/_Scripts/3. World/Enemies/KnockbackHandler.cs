using System.Collections;
using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(Rigidbody))]
    public class KnockbackHandler : MonoBehaviour, IKnockbackable
    {
        [SerializeField] private float _drag = 12f; //How fast it bleeds off
        
        public bool IsKnockedBack { get; private set; }

        private Rigidbody _rb;
        private Coroutine _current;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(Run(direction, force));
        }

        private IEnumerator Run(Vector3 direction, float force)
        {
            IsKnockedBack = true;
            _rb.velocity = direction.normalized * force; //Set directly, don't add - cleaner reset

            while (_rb.velocity.sqrMagnitude > 0.05f)
            {
                _rb.velocity = Vector3.MoveTowards(_rb.velocity, Vector3.zero, _drag * Time.deltaTime);
                yield return null;
            }
            
            _rb.velocity = Vector3.zero;
            IsKnockedBack = false;
            _current = null;
        }
    }
}