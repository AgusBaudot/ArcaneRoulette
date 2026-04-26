using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace World
{
    [RequireComponent(typeof(Rigidbody), typeof(KnockbackHandler), typeof(EnemyHealth))]
    public class BasicBruteEnemy : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private int _attackDamage = 4;
        [SerializeField] private float _attackCooldown = 1f; // "1 Attack Speed" usually means 1 sec between attacks
        [SerializeField] private float _attackWindup = 0.6f; // Time player has to dodge
        [SerializeField] private float _attackRange = 2.5f;

        [Header("Slam Hitbox (Rectangular)")]
        [SerializeField] private float _slamLength = 3f;
        [SerializeField] private float _slamWidth = 2f;
        [SerializeField] private LayerMask _hitLayers; // Set this to include Player AND Enemy layers

        private Rigidbody _rb;
        private KnockbackHandler _knockback;
        private Transform _playerTarget;
        
        private float _lastAttackTime;
        private bool _isAttacking;

        public void Init(Transform playerTarget)
        {
            _playerTarget = playerTarget;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _knockback = GetComponent<KnockbackHandler>();
        }

        private void FixedUpdate()
        {
            if (_playerTarget == null || _knockback.IsKnockedBack) 
            {
                if (!_knockback.IsKnockedBack && !_isAttacking) 
                    _rb.velocity = Vector3.zero;
                return;
            }

            if (_isAttacking) return; // Cannot move while slamming

            float distanceToPlayer = Vector3.Distance(GetFlatPos(transform.position), GetFlatPos(_playerTarget.position));

            if (distanceToPlayer > _attackRange)
            {
                // Unrelenting Chase (Tank behavior: never flee)
                Vector3 dir = (GetFlatPos(_playerTarget.position) - GetFlatPos(transform.position)).normalized;
                _rb.velocity = dir * _moveSpeed;
            }
            else if (Time.time >= _lastAttackTime + _attackCooldown)
            {
                // In range. Stop and Slam.
                _rb.velocity = Vector3.zero;
                StartCoroutine(SlamRoutine());
            }
        }

        private IEnumerator SlamRoutine()
        {
            _isAttacking = true;

            // 1. Lock in the attack direction
            Vector3 attackDir = (GetFlatPos(_playerTarget.position) - GetFlatPos(transform.position)).normalized;
            if (attackDir == Vector3.zero) attackDir = Vector3.forward;

            // 2. Windup (Telegraph the attack)
            // Optional: You could make the sprite flash red here
            yield return new WaitForSeconds(_attackWindup);

            // 3. Execute the Rectangular Hitbox
            ExecuteRectangularSlam(attackDir);

            // 4. Cooldown
            _lastAttackTime = Time.time;
            _isAttacking = false;
        }

        private void ExecuteRectangularSlam(Vector3 direction)
        {
            // Calculate the center of the box in front of the brute
            Vector3 boxCenter = GetFlatPos(transform.position) + (direction * (_slamLength / 2f));
            Vector3 halfExtents = new Vector3(_slamWidth / 2f, 1f, _slamLength / 2f);
            Quaternion boxRotation = Quaternion.LookRotation(direction);

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, boxRotation, _hitLayers);
            
            // Deduplicate hits (in case an enemy has multiple colliders)
            HashSet<GameObject> processedTargets = new HashSet<GameObject>();

            foreach (var hit in hits)
            {
                // Prevent hitting itself
                if (hit.gameObject == this.gameObject) continue;

                // Process each unique object only once
                if (processedTargets.Contains(hit.gameObject)) continue;
                processedTargets.Add(hit.gameObject);

                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    // Hits player AND enemies (Friendly Fire enabled)
                    damageable.TakeDamage(_attackDamage, ElementType.Neutral);
                }
            }
        }

        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);

        // --- Visual Debugging ---
        // This draws the rectangle in the Scene view so you can perfectly tune the size
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            
            // Assume it attacks forward if player is null in editor
            Vector3 dir = Application.isPlaying && _isAttacking ? transform.forward : transform.forward;
            Vector3 center = transform.position + (dir * (_slamLength / 2f));
            Vector3 size = new Vector3(_slamWidth, 2f, _slamLength);

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, Quaternion.LookRotation(dir), size);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }
}