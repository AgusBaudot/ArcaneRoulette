using System.Collections;
using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Rigidbody))]
    public class KnockbackHandler : MonoBehaviour, IKnockbackable
    {
        [SerializeField] private float _drag = 12f; //How fast it bleeds off
        [SerializeField] private float _maxKnockbackDuration = 2f;
        
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
            
            //Ensure we aren't knocking them directly into the floor/sky
            direction.y = 0;
            
            _rb.velocity = direction.normalized * force; //Set directly, don't add - cleaner reset

            float timeElapsed = 0f;
            
            while (new Vector3(_rb.velocity.x, 0, _rb.velocity.z).sqrMagnitude > 0.05f && timeElapsed < _maxKnockbackDuration)
            {
                timeElapsed += Time.deltaTime;
                Vector3 horizontalVel = Vector3.MoveTowards(new Vector3(_rb.velocity.x, 0, _rb.velocity.z), Vector3.zero, _drag * Time.deltaTime);
                
                _rb.velocity = new Vector3(horizontalVel.x, _rb.velocity.y, horizontalVel.z);
                yield return null;
            }
            
            _rb.velocity = Vector3.zero;
            IsKnockedBack = false;
            _current = null;
        }
    }
}