using System;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class MeleeAIBrain : AIBrain, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;
        
        [Header("Melee-specific")]
        [Tooltip("Assign once the weapon/rig exists — a child BoxCollider under the sword's socket.")]
        [SerializeField] private MeleeWeaponHitbox _hitbox;

        private MeleeEnemyStats MeleeStats => _enemyStats as MeleeEnemyStats;

        private PlayerController _playerController;
        private Vector3 _lastAttackDirection;
        private float _dashDistanceMoved;
        private bool _isStepping;
        private bool _isDashing;
        
        public event Action<float> OnSpawnStarted;
        public event Action<float> OnWindupStarted;
        public event Action<int, float> OnSwingStarted;
        public event Action OnGapStarted;
        public event Action<float> OnDashStarted;
        public event Action<float> OnRecomposing;
        public MeleeEnemyStats ActiveMeleeStats => MeleeStats;
        
        public float CurrentAttack1Duration => GetAttack1Duration();
        public float CurrentAttack2Duration => GetAttack2Duration();
        public float CurrentAttack3Duration => GetAttack3Duration();
        public Vector3 CurrentAttackDirection => _lastAttackDirection;

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance.Unregister(this);
            SwarmManager.ReleaseSlot(gameObject.GetInstanceID());
        }

        public override void ResetComponent()
        {
            base.ResetComponent();
            _isStepping = false;
            _isDashing = false;
            SwarmManager.ReleaseSlot(gameObject.GetInstanceID());
            if (_hitbox != null)
            {
                _hitbox.Deactivate();
            }
        }
        
        public void Tick(float dt)
        {
            if (_agent == null || !IsState(AIState.Attack)) return;
            
            base.Tick();

            if (_isStepping)
            {
                float speed = EffectiveChaseSpeed * MeleeStats.Attack1MovementSpeedMultiplier;
                _agent.Move(_lastAttackDirection * speed * dt);
            }
            else if (_isDashing && _dashDistanceMoved < MeleeStats.Attack3DashDistance)
            {
                float speed = EffectiveChaseSpeed * MeleeStats.Attack3DashSpeedMultiplier;
                float step = Mathf.Min(speed * dt, MeleeStats.Attack3DashDistance - _dashDistanceMoved);
                _agent.Move(_lastAttackDirection * step);
                _dashDistanceMoved += step;
            }
        }

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Melee");

            if (MeleeStats == null)
            {
                Debug.LogError($"{name}: EnemyController's stats asset isn't a MeleeEnemyStats " +
                               $"(got '{(_enemyStats == null ? "null" : _enemyStats.GetType().Name)}'). " +
                               "Assign a Melee Stats asset instead — every duration in this tree reads through it.");
                return tree;
            }

            var root = new PrioritySelectorNode("Melee Root");

            root.AddChild(new LeafNode("Spawning",
                new OneShotGateStrategy(BeginSpawning, () => MeleeStats.SpawnDuration), priority: 50));

            root.AddChild(BuildAttackSequence());

            root.AddChild(new LeafNode("Chase", new ActionNode(DoChase), priority: 0));

            tree.AddChild(root);
            return tree;
        }

        // ---- Spawning ----
        private void BeginSpawning()
        {
            SetState(AIState.Spawning);
            OnSpawnStarted?.Invoke(MeleeStats.SpawnDuration);
        }

        // ---- Chase ----
        private void DoChase()
        {
            SetState(AIState.Chase);
            Transform player = GetPlayer();
            if (player == null || _agent == null) return;

            if (_playerController == null) _playerController = player.GetComponentInParent<PlayerController>();

            _agent.isStopped = false;
            _agent.speed = EffectiveChaseSpeed;

            if (!IsInLos())
            {
                _agent.SetDestination(player.position);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float predictionTime = Mathf.Clamp(distanceToPlayer / EffectiveChaseSpeed, 0f, MeleeStats.MaxPredictionTime);

            Vector3 futurePos = player.position + (_playerController.LogicalVelocity * predictionTime);
            Vector3 basePoint = Vector3.Lerp(player.position, futurePos, MeleeStats.TargetPredictionWeight);

            Vector3 rawSlotOffset = SwarmManager.GetOrClaimSlot(gameObject.GetInstanceID()) * MeleeStats.FormationRadius;
            
            rawSlotOffset.z *= Helpers.PlayerStats.VerticalSpeedMultiplier; 

            _agent.SetDestination(basePoint + rawSlotOffset);
        }

        // ---- Attack ----
        private Node BuildAttackSequence()
        {
            var sequence = new SequenceNode("Attack", priority: 10);

            sequence.AddChild(new LeafNode("CanAttack",
                new ConditionNode(() => IsInStableDistance(GetPlayer()) && IsInLos())));

            sequence.AddChild(new LeafNode("Windup",
                new TimedActionStrategy(BeginWindup, () => MeleeStats.WindupDuration)));

            sequence.AddChild(new LeafNode("Swing1",
                new TimedActionStrategy(() => BeginSwing(0), GetAttack1Duration)));

            sequence.AddChild(new LeafNode("Gap1",
                new TimedActionStrategy(EndSwing, () => MeleeStats.Attack1EndDelay)));

            sequence.AddChild(new LeafNode("Swing2",
                new TimedActionStrategy(() => BeginSwing(1), GetAttack2Duration)));

            sequence.AddChild(new LeafNode("Gap2",
                new TimedActionStrategy(EndSwing, () => MeleeStats.Attack2EndDelay)));

            sequence.AddChild(new LeafNode("Swing3Dash",
                new TimedActionStrategy(BeginSwing3Dash, GetAttack3Duration)));
                
            sequence.AddChild(new LeafNode("Recomposing",
                new TimedActionStrategy(BeginRecomposing, () => MeleeStats.RecomposingDuration)));

            return sequence;
        }

        private void BeginWindup()
        {
            SetState(AIState.Attack);
            _agent.isStopped = true;
            Transform player = GetPlayer();
            _lastAttackDirection = player != null
                ? (player.position - transform.position).normalized
                : transform.forward;
                
            OnWindupStarted?.Invoke(MeleeStats.WindupDuration);
        }

        private float GetAttack1Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack1SwingSpeedMultiplier);

        private float GetAttack2Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack2SwingSpeedMultiplier);

        private void BeginSwing(int attackIndex)
        {
            RedirectTowardPlayer();
            
            int damage = Mathf.RoundToInt(EffectiveAttackDamage * MeleeStats.Attack1DamageMultiplier);
            ActivateHitbox(damage, MeleeStats.Attack1HitboxSize);
            _isStepping = true;
            
            float duration = attackIndex == 0 ? GetAttack1Duration() : GetAttack2Duration();
            OnSwingStarted?.Invoke(attackIndex, duration);
        }

        private void EndSwing()
        {
            _hitbox?.Deactivate();
            _isStepping = false;
            
            OnGapStarted?.Invoke();
        }

        private float GetAttack3Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack3SwingSpeedMultiplier);

        private void BeginSwing3Dash()
        {
            RedirectTowardPlayer();
            _dashDistanceMoved = 0f;
            int damage = Mathf.RoundToInt(EffectiveAttackDamage * MeleeStats.Attack3DamageMultiplier);
            Vector3 size = MeleeStats.Attack1HitboxSize * (1f + MeleeStats.Attack3HitboxSizeMultiplier);
            ActivateHitbox(damage, size);
            
            _isDashing = true;
            
            OnDashStarted?.Invoke(GetAttack3Duration());
        }

        private void BeginRecomposing()
        {
            _hitbox?.Deactivate();
            SetState(AIState.Attack);
            _agent.isStopped = true;
            
            _isStepping = false;
            _isDashing = false;
            
            OnRecomposing?.Invoke(MeleeStats.RecomposingDuration);
        }

        private void RedirectTowardPlayer()
        {
            Transform player = GetPlayer();
            if (player == null) return;
            Vector3 toPlayer = (player.position - transform.position).normalized;
            float maxRadians = MeleeStats.MaxRedirectAngle * Mathf.Deg2Rad;
            _lastAttackDirection = Vector3.RotateTowards(_lastAttackDirection, toPlayer, maxRadians, 0f).normalized;
        }

        private void ActivateHitbox(int damage, Vector3 size)
        {
            if (_hitbox == null)
            {
                Debug.LogWarning($"{name}: no MeleeWeaponHitbox assigned — this swing deals no damage.");
                return;
            }
            _hitbox.Configure(damage, MeleeStats.ElementType, size, _lastAttackDirection);
            _hitbox.Activate();
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_enemyStats == null) return;

            // Attack Range Sphere
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _enemyStats.AttackRange);

            // Vision Sphere
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _enemyStats.ViewDistance);

            // ---- ATTACK HITBOX PREVIEW ----
            if (_enemyStats is MeleeEnemyStats stats)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                
                // Draw a preview of the hitbox facing forward
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                
                Vector3 previewSize = stats.Attack1HitboxSize;
                Vector3 previewCenter = new Vector3(0, 0, previewSize.z / 2f);
                
                Gizmos.DrawWireCube(previewCenter, previewSize);
                
                Gizmos.matrix = oldMatrix;
            }
        }
#endif
    }
}