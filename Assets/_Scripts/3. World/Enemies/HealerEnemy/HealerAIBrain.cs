using System.Collections.Generic;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public sealed class HealerAIBrain : AIBrain, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;

        private HealerEnemyStats HealerStats => _enemyStats as HealerEnemyStats;

        private float _healCooldownEndTime;
        private float _throwCooldownEndTime;
        private float _healTimer;
        private List<EnemyHealth> _activeHealTargets = new();

        private void OnEnable() => UpdateManager.Instance.Register(this);
        private void OnDisable() => UpdateManager.Instance?.Unregister(this);

        public override void ResetComponent()
        {
            base.ResetComponent();
            _healCooldownEndTime = 0f;
            _throwCooldownEndTime = 0f;
            _activeHealTargets.Clear();
        }

        public void Tick(float dt)
        {
            if (_agent == null || !IsState(AIState.Healing)) return;

            if (_activeHealTargets.Count > 0 && _activeHealTargets[0] != null)
            {
                Transform target = _activeHealTargets[0].transform;
                Transform player = GetPlayer();
                if (player != null)
                {
                    Vector3 toTarget = (target.position - player.position).normalized;
                    Vector3 idealPos = target.position + toTarget * HealerStats.SpacingOffset;
                    
                    _agent.SetDestination(idealPos);
                    _agent.speed = EffectiveChaseSpeed;
                }
            }
        }

        protected override BehaviorTree BuildTree()
        {
            var tree = new BehaviorTree("Healer");
            if (HealerStats == null) return tree;

            var root = new PrioritySelectorNode("Healer Root");

            root.AddChild(new LeafNode("Spawning",
                new OneShotGateStrategy(() => SetState(AIState.Spawning), () => HealerStats.SpawnDuration), priority: 50));

            root.AddChild(BuildHealingSequence());
            root.AddChild(BuildThrowingSequence());
            root.AddChild(new LeafNode("Chase", new ActionNode(DoChase), priority: 0));

            tree.AddChild(root);
            return tree;
        }

        private Node BuildHealingSequence()
        {
            var seq = new SequenceNode("HealingSequence", priority: 40);
            seq.AddChild(new LeafNode("CanHeal", new ConditionNode(() => 
                Time.time >= _healCooldownEndTime && GetValidHealTargets().Count > 0)));
            
            seq.AddChild(new LeafNode("HealAction", new TimedActionStrategy(BeginHealing, () => HealerStats.HealingDuration, OnHealingTick)));
            seq.AddChild(new LeafNode("EndHeal", new ActionNode(EndHealing)));
            return seq;
        }

        private Node BuildThrowingSequence()
        {
            var seq = new SequenceNode("ThrowingSequence", priority: 30);
            seq.AddChild(new LeafNode("CanThrow", new ConditionNode(() => 
                Time.time >= _throwCooldownEndTime && IsPlayerInThrowingRange())));
            
            seq.AddChild(new LeafNode("ThrowAction", new TimedActionStrategy(() => 
            {
                SetState(AIState.Throwing);
                _agent.isStopped = true;
            }, () => HealerStats.ThrowingDuration)));
            
            seq.AddChild(new LeafNode("EndThrow", new ActionNode(EndThrowing)));
            return seq;
        }

        private void DoChase()
        {
            SetState(AIState.Chase);
            if (_agent == null) return;
            
            _agent.isStopped = false;
            _agent.speed = EffectiveChaseSpeed;

            Transform player = GetPlayer();
            if (player == null) return;

            bool isHealReady = Time.time >= _healCooldownEndTime;
            bool isThrowReady = Time.time >= _throwCooldownEndTime;

            if (isThrowReady && !isHealReady)
            {
                _agent.SetDestination(player.position);
            }
            else if (isHealReady)
            {
                var lowestHpAlly = GetLowestHpAllyGlobal();
                if (lowestHpAlly != null)
                    _agent.SetDestination(lowestHpAlly.transform.position);
                else
                    _agent.SetDestination(player.position);
            }
            else
            {
                Vector3 toPlayer = player.position - transform.position;
                if (toPlayer.magnitude < HealerStats.HealingRange)
                {
                    Vector3 fleePos = transform.position - toPlayer.normalized * (HealerStats.HealingRange + 2f);
                    _agent.SetDestination(fleePos);
                }
                else
                {
                    _agent.isStopped = true;
                }
            }
        }

        private void BeginHealing()
        {
            SetState(AIState.Healing);
            _healTimer = 0f;
            _activeHealTargets = GetValidHealTargets();
        }

        private void OnHealingTick(float dt)
        {
            _healTimer += dt;
            if (_healTimer >= 2f)
            {
                _healTimer -= 2f;
                float healAmount = EffectiveAttackDamage / 3f;
                
                _activeHealTargets.RemoveAll(t => t == null || t.CurrentHp <= 0);
                
                if (_activeHealTargets.Count == 0)
                {
                    _activeHealTargets = GetValidHealTargets();
                    if (_activeHealTargets.Count == 0)
                    {
                        _tree.Reset();
                        return;
                    }
                }

                foreach (var target in _activeHealTargets)
                {
                    target.Heal(healAmount);
                }
            }
        }

        private void EndHealing()
        {
            _healCooldownEndTime = Time.time + HealerStats.HealingCooldown;
        }

        private List<EnemyHealth> GetValidHealTargets()
        {
            var targets = new List<EnemyHealth>();
            Collider[] hits = Physics.OverlapSphere(transform.position, HealerStats.HealingRange, HealerStats.HitLayer);
            
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (hit.TryGetComponent<EnemyHealth>(out var health) && health.CurrentHp < health.MaxHp)
                {
                    targets.Add(health);
                }
            }

            targets.Sort((a, b) => (a.CurrentHp / a.MaxHp).CompareTo(b.CurrentHp / b.MaxHp));
            if (targets.Count > 2) targets.RemoveRange(2, targets.Count - 2);
            return targets;
        }

        private EnemyHealth GetLowestHpAllyGlobal()
        {
            EnemyHealth lowest = null;
            float lowestPct = float.MaxValue;
            
            if (EntityController.ActiveController != null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 50f, HealerStats.HitLayer);
                foreach (var hit in hits)
                {
                    if (hit.gameObject == gameObject) continue;
                    if (hit.TryGetComponent<EnemyHealth>(out var health) && health.CurrentHp < health.MaxHp)
                    {
                        float pct = health.CurrentHp / health.MaxHp;
                        if (pct < lowestPct)
                        {
                            lowestPct = pct;
                            lowest = health;
                        }
                    }
                }
            }
            return lowest;
        }

        private bool IsPlayerInThrowingRange()
        {
            Transform p = GetPlayer();
            return p != null && Vector3.Distance(transform.position, p.position) <= HealerStats.ThrowingRange;
        }

        private void EndThrowing()
        {
            Transform player = GetPlayer();
            if (player != null && HealerStats.BottlePrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 1f;
                Vector3 dir = (player.position - spawnPos).normalized;
                
                var bottle = Helpers.ProjFactory.Spawn<BottleProjectile>(HealerStats.BottlePrefab, spawnPos, HealerStats.BottlePrefab.transform.rotation);
                
                bottle.InitEnemyBottle(
                    HealerStats.BottlePrefab, 
                    dir, 
                    HealerStats.BottleThrowSpeed, 
                    Mathf.RoundToInt(EffectiveAttackDamage * 0.5f), 
                    HealerStats.ElementType, 
                    gameObject, 
                    player.position
                );
            }
            _throwCooldownEndTime = Time.time + HealerStats.ThrowingCooldown;
        }
    }
}