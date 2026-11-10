using System.Collections;
using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    /// <summary>
    /// Knockback for agent-driven enemies. While the NavMeshAgent is active the push is applied
    /// through agent.Move (it respects the NavMesh and can't fight the agent), and the agent's own
    /// walking is suppressed for the duration. Falls back to Rigidbody velocity when there is no
    /// active agent and the body isn't kinematic.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class KnockbackHandler : MonoBehaviour, IKnockbackable
    {
        [SerializeField] private float _duration = 0.15f; 
        [SerializeField] private AnimationCurve _velocityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        public bool IsKnockedBack { get; private set; }

        private static readonly WaitForFixedUpdate Fixed = new WaitForFixedUpdate();

        private Rigidbody _rb;
        private NavMeshAgent _agent;
        private Coroutine _current;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnDisable()
        {
            _current = null;
            IsKnockedBack = false;
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (!isActiveAndEnabled) return;
            
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(Run(direction, force));
        }

        private bool AgentUsable => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;

        private IEnumerator Run(Vector3 direction, float force)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) { _current = null; yield break; }
            direction.Normalize();

            bool useAgent = AgentUsable;
            if (!useAgent && _rb.isKinematic) { _current = null; yield break; }

            IsKnockedBack = true;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                float speed = force * _velocityCurve.Evaluate(elapsed / _duration);

                if (useAgent)
                {
                    if (!AgentUsable) break;
                    float dt = Time.deltaTime;
                    _agent.velocity = Vector3.zero;
                    _agent.Move(direction * speed * dt);
                    elapsed += dt;
                    yield return null;
                }
                else
                {
                    Vector3 v = direction * speed;
                    _rb.velocity = new Vector3(v.x, _rb.velocity.y, v.z);
                    elapsed += Time.fixedDeltaTime;
                    yield return Fixed;
                }
            }

            if (!useAgent && !_rb.isKinematic)
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);

            IsKnockedBack = false;
            _current = null;
        }
    }
}