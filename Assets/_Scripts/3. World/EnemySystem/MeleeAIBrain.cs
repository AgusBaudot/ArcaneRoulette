using UnityEngine;

namespace World
{
    public sealed class MeleeAIBrain : AIBrain
    {
        [Header("Melee-specific")]
        [Tooltip("Assign once the weapon/rig exists — a child BoxCollider under the sword's socket.")]
        [SerializeField] private MeleeWeaponHitbox _hitbox;

        private MeleeEnemyStats MeleeStats => _enemyStats as MeleeEnemyStats;

        private Vector3 _lastAttackDirection;
        private float _dashDistanceMoved;

        public override void ResetComponent()
        {
            base.ResetComponent();
            // Guards against a mid-swing death leaving the hitbox live on next spawn —
            // Reset() rewinds the tree's position but doesn't run strategy cleanup,
            // so this has to happen explicitly.
            if (_hitbox != null)
                _hitbox.Deactivate();
        }

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Melee");

            if (MeleeStats == null)
            {
                Debug.LogError($"{name}: EnemyController's stats asset isn't a MeleeEnemyStats " +
                    $"(got '{(_enemyStats == null ? "null" : _enemyStats.GetType().Name)}'). " +
                    "Assign a Melee Stats asset instead — every duration in this tree reads through it.");
                return tree; // empty tree: BehaviorTree.Process() returns Failure on no children,
                             // not a crash — the enemy just sits idle instead of NRE-ing every tick.
            }

            var spawning = new LeafNode("Spawning",
                new TimedActionStrategy(BeginSpawning, () => MeleeStats.SpawnDuration));

            var combat = new PrioritySelectorNode("Combat");
            combat.AddChild(BuildAttackSequence());
            combat.AddChild(new LeafNode("Chase", new ActionNode(DoChase), priority: 0));

            var root = new SequenceNode("Melee Root");
            root.AddChild(spawning);
            root.AddChild(combat);

            tree.AddChild(root);
            return tree;
        }

        // ---- Spawning ----
        // FDD: enemies go straight from Spawning to Chase, no Idle/detection-wake
        // step — matches what you confirmed a few rounds back.
        private void BeginSpawning() => SetState(AIState.Spawning);

        // ---- Chase ----
        private void DoChase()
        {
            SetState(AIState.Chase);
            Transform player = GetPlayer();
            if (player == null || _agent == null) return;
            _agent.isStopped = false;
            _agent.speed = EffectiveChaseSpeed;
            _agent.SetDestination(player.position);
        }

        // ---- Attack ----
        // PrioritySelectorNode checks this branch first every tick, in the same
        // frame it'd otherwise try Chase. Once this sequence starts and is
        // Running, it's found before Chase is ever reached — that's what gives
        // "cannot be interrupted until the combo finishes," with nothing extra
        // needed to enforce it.
        private Node BuildAttackSequence()
        {
            var sequence = new SequenceNode("Attack", priority: 10);

            sequence.AddChild(new LeafNode("CanAttack",
                new ConditionNode(() => IsInStableDistance(GetPlayer()) && IsInLos())));

            sequence.AddChild(new LeafNode("Windup",
                new TimedActionStrategy(BeginWindup, () => MeleeStats.WindupDuration)));

            sequence.AddChild(new LeafNode("Swing1",
                new TimedActionStrategy(() => BeginSwing(0), GetAttack12Duration, TickStepMovement)));
            sequence.AddChild(new LeafNode("Gap1",
                new TimedActionStrategy(EndSwing, () => MeleeStats.Attack1EndDelay)));

            sequence.AddChild(new LeafNode("Swing2",
                new TimedActionStrategy(() => BeginSwing(1), GetAttack12Duration, TickStepMovement)));
            sequence.AddChild(new LeafNode("Gap2",
                new TimedActionStrategy(EndSwing, () => MeleeStats.Attack2EndDelay)));

            sequence.AddChild(new LeafNode("Swing3Dash",
                new TimedActionStrategy(BeginSwing3Dash, GetAttack3Duration, TickDashMovement)));
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
            // TODO: WindupSlowPercentage isn't applied to anything yet — there's no
            // movement during windup itself in the FDD (the step is part of the
            // swing, not the windup), so it likely only matters once animation
            // playback rate or a locomotion blend reads it. Left unwired rather
            // than guessed.
        }

        // EffectiveAttackSpeed is an interval — seconds per attack, lower is
        // faster (EnemyStats' own tooltip). Dividing by the per-attack multiplier
        // so a multiplier above 1 shortens this swing's share of that interval.
        private float GetAttack12Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack1SwingSpeedMultiplier);

        private void BeginSwing(int attackIndex)
        {
            RedirectTowardPlayer();
            int damage = Mathf.RoundToInt(EffectiveAttackDamage * MeleeStats.Attack1DamageMultiplier);
            ActivateHitbox(damage, MeleeStats.Attack1HitboxSize);
        }

        private void TickStepMovement(float dt)
        {
            if (_agent == null) return;
            float speed = EffectiveChaseSpeed * MeleeStats.Attack1MovementSpeedMultiplier;
            _agent.Move(_lastAttackDirection * speed * dt);
        }

        private void EndSwing() => _hitbox?.Deactivate();

        private float GetAttack3Duration() =>
            EffectiveAttackSpeed / Mathf.Max(0.01f, MeleeStats.Attack3SwingSpeedMultiplier);

        private void BeginSwing3Dash()
        {
            RedirectTowardPlayer();
            _dashDistanceMoved = 0f;
            int damage = Mathf.RoundToInt(EffectiveAttackDamage * MeleeStats.Attack3DamageMultiplier);
            Vector3 size = MeleeStats.Attack1HitboxSize * (1f + MeleeStats.Attack3HitboxSizeMultiplier);
            ActivateHitbox(damage, size);
        }

        // See chat for why this is a specific interpretation, not a confirmed one:
        // the swing's own (Attack Speed-derived) duration is what gates the whole
        // phase; the dash moves at its own (Movement Speed-derived) rate toward
        // its fixed distance within that window, and simply stops early if it
        // gets there first.
        private void TickDashMovement(float dt)
        {
            if (_agent == null) return;
            if (_dashDistanceMoved >= MeleeStats.Attack3DashDistance) return;

            float speed = EffectiveChaseSpeed * MeleeStats.Attack3DashSpeedMultiplier;
            float step = Mathf.Min(speed * dt, MeleeStats.Attack3DashDistance - _dashDistanceMoved);
            _agent.Move(_lastAttackDirection * step);
            _dashDistanceMoved += step;
        }

        private void BeginRecomposing()
        {
            _hitbox?.Deactivate();
            SetState(AIState.Attack); // still locked out of Chase until this ends
            _agent.isStopped = true;
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
            _hitbox.Configure(damage, MeleeStats.ElementType, size);
            _hitbox.Activate();
        }
    }
}