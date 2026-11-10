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
        [Tooltip("Stopping distance while chasing a swarm slot.")]
        [SerializeField] private float _chaseStopDistance = 0.3f;
        [Tooltip("During windup the enemy keeps closing in (slowed) but never walks closer than this to the player.")]
        [SerializeField] private float _windupMinDistance = 1.2f;
        [Tooltip("Swarm slots are clamped to this fraction of Attack Range, so a slot can never be outside attack range.")]
        [SerializeField, Range(0.3f, 1f)] private float _slotMaxRangeFraction = 0.85f;

        private MeleeEnemyStats MeleeStats => _enemyStats as MeleeEnemyStats;

        private Vector3 _lastAttackDirection;
        private float _dashDistanceMoved;
        private bool _isWindingUp;
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
            UpdateManager.Instance?.Unregister(this);
            SwarmManager.ReleaseSlot(gameObject.GetInstanceID());
        }

        public override void ResetComponent()
        {
            base.ResetComponent();
            _isWindingUp = false;
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
            ApplyPerspectiveSpeed();

            if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh || !IsState(AIState.Attack)) return;

            if (_isWindingUp)
            {
                WindupApproach(dt);
            }
            else if (_isStepping)
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

        private void WindupApproach(float dt)
        {
            Transform player = GetPlayer();
            if (player == null) return;

            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance <= _windupMinDistance) return;

            float speed = EffectiveChaseSpeed * (1f - Mathf.Clamp01(MeleeStats.WindupSlowPercentage));
            float step = Mathf.Min(speed * dt, distance - _windupMinDistance);
            _agent.Move(toPlayer / distance * step);
        }

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Melee");

            if (MeleeStats == null)
            {
                Debug.LogError($"{name}: EnemyController's stats asset isn't a MeleeEnemyStats " +
                               $"(got '{(_enemyStats == null ? "null" : _enemyStats.GetType().Name)}'). " +
                               "Assign a Melee Stats asset instead.");
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

        private void BeginSpawning()
        {
            SetState(AIState.Spawning);
            OnSpawnStarted?.Invoke(MeleeStats.SpawnDuration);
        }

        private void DoChase()
        {
            SetState(AIState.Chase);
            Transform player = GetPlayer();
            if (player == null || _agent == null) return;

            ApplyChaseSpeed();

            Vector3 target = player.position;
            if (IsInLos())
            {
                target = GetPursuitPoint(player,
                    MeleeStats.TargetPredictionWeight,
                    MeleeStats.MaxPredictionTime,
                    MeleeStats.FormationRadius,
                    _enemyStats.AttackRange * _slotMaxRangeFraction);
            }

            MoveTo(target, _chaseStopDistance);
        }

        private Node BuildAttackSequence()
        {
            var sequence = new SequenceNode("Attack", priority: 10);

            sequence.AddChild(new LeafNode("CanAttack",
                new ConditionNode(CanInitiateAttack)));

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

        private bool CanInitiateAttack()
        {
            Transform player = GetPlayer();
            if (player == null || !IsInLos()) return false;

            return IsInStableDistance(player);
        }

        private void BeginWindup()
        {
            SetState(AIState.Attack);

            _agent.isStopped = true;
            if (_agent.hasPath)
            {
                _agent.ResetPath();
            }

            _isWindingUp = true;
            _lastAttackDirection = FlatDirectionToPlayer(transform.forward);
                
            OnWindupStarted?.Invoke(MeleeStats.WindupDuration);
        }

        private float GetAttack1Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack1SwingSpeedMultiplier);

        private float GetAttack2Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack2SwingSpeedMultiplier);

        private void EndSwing()
        {
            _hitbox?.Deactivate();
            _isStepping = false;
            
            OnGapStarted?.Invoke();
        }

        private float GetAttack3Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack3SwingSpeedMultiplier);

        private void BeginSwing(int attackIndex)
        {
            _isWindingUp = false;
            RedirectTowardPlayer();
            
            int damage = Mathf.RoundToInt(EffectiveAttackDamage * MeleeStats.Attack1DamageMultiplier);
            float duration = attackIndex == 0 ? GetAttack1Duration() : GetAttack2Duration();
            float sweepAngle = attackIndex == 0 ? 140f : -100f; 
            
            AudioEventSO swingSound = attackIndex == 0 ? MeleeStats.Attack1Sound : MeleeStats.Attack2Sound;
            if (swingSound != null)
            {
                EventBus.Publish(new AudioPlayRequest
                {
                    Event = swingSound,
                    WorldPosition = transform.position,
                });
            }
            
            ActivateHitbox(damage, MeleeStats.Attack1HitboxSize, sweepAngle, duration);
            _isStepping = true;
            
            OnSwingStarted?.Invoke(attackIndex, duration);
        }
        
        private void BeginSwing3Dash()
        {
            _isWindingUp = false;
            RedirectTowardPlayer();
            _dashDistanceMoved = 0f;
            
            int damage = Mathf.RoundToInt(EffectiveAttackDamage * MeleeStats.Attack3DamageMultiplier);

            Vector3 baseSize = MeleeStats.Attack1HitboxSize;
            Vector3 size = new Vector3(baseSize.x, baseSize.y, baseSize.z * (1f + MeleeStats.Attack3HitboxSizeMultiplier));
            float duration = GetAttack3Duration();
            float sweepAngle = 140f;
            
            if (MeleeStats.Attack3Sound != null)
            {
                EventBus.Publish(new AudioPlayRequest
                {
                    Event = MeleeStats.Attack3Sound,
                    WorldPosition = transform.position,
                });
            }
            
            ActivateHitbox(damage, size, sweepAngle, duration);
            _isDashing = true;
            
            OnDashStarted?.Invoke(duration);
        }

        private void BeginRecomposing()
        {
            _hitbox?.Deactivate();
            SetState(AIState.Attack);
            _agent.isStopped = true;
            
            _isWindingUp = false;
            _isStepping = false;
            _isDashing = false;
            
            OnRecomposing?.Invoke(MeleeStats.RecomposingDuration);
        }

        private void RedirectTowardPlayer()
        {
            Transform player = GetPlayer();
            if (player == null) return;
            Vector3 toPlayer = FlatDirectionToPlayer(_lastAttackDirection);
            float maxRadians = MeleeStats.MaxRedirectAngle * Mathf.Deg2Rad;
            _lastAttackDirection = Vector3.RotateTowards(_lastAttackDirection, toPlayer, maxRadians, 0f).normalized;
        }

        private void ActivateHitbox(int damage, Vector3 size, float sweepAngle, float duration)
        {
            if (_hitbox == null) return;
            
            _hitbox.Configure(damage, size, _lastAttackDirection, sweepAngle);
            _hitbox.Activate(duration);
        }
    }
}