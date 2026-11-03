using System.Collections;
using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Rigidbody))]
    public class KnockbackHandler : MonoBehaviour, IKnockbackable
    {
        [SerializeField] private float _duration = 0.15f; 
        
        [SerializeField] private AnimationCurve _velocityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        public bool IsKnockedBack { get; private set; }

        private Rigidbody _rb;
        private Coroutine _current;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (!isActiveAndEnabled) return;
            
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(Run(direction, force));
        }

        private IEnumerator Run(Vector3 direction, float force)
        {
            if (_rb.isKinematic) yield break;
            
            IsKnockedBack = true;
            direction.y = 0;
            
            Vector3 initialVelocity = direction.normalized * force;
            float timeElapsed = 0f;
            
            while (timeElapsed < _duration)
            {
                float t = timeElapsed / _duration;
                float multiplier = _velocityCurve.Evaluate(t);
                
                Vector3 currentVel = initialVelocity * multiplier;
                
                _rb.velocity = new Vector3(currentVel.x, _rb.velocity.y, currentVel.z);
                
                yield return new WaitForFixedUpdate(); 
                timeElapsed += Time.fixedDeltaTime;
            }
            
            _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
            IsKnockedBack = false;
            _current = null;
        }
    }
}