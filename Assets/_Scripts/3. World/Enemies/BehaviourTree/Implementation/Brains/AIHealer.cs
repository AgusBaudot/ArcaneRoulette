using Foundation;
using UnityEngine;

namespace World 
{
    public class AIHealer : AIBrain
    {
        #region SerializeField
        [SerializeField] private HealingArea _healingAreaPrefab;
        [SerializeField] private KnockbackHandler _knockback;
        [SerializeField] private float _spellLifetime = 2f;
        [SerializeField] private float _spellPulseFrequency = 0.5f;
        #endregion
        private bool _hasHealedOnce = false;
        private IAlly _ally;
        protected override void Awake()
        {
            _knockback = GetComponent<KnockbackHandler>();
            base.Awake();
        }
        public bool SearchInjuredAlly()
        {
            if (HasValidAlly())
                return true;

            _ally = FindBestAlly();
            

            return _ally != null;
        }

        private bool HasValidAlly()
        {
            if (_ally == null) return false;
            // is dead -> clear || if ally has 100% of HP and i already healed it -> clear
            if (_ally.Healable.CurrentHp <= 0 || (_ally.Healable.CurrentHp >= _ally.Healable.MaxHp && _hasHealedOnce))
            {
                ClearAlly();
                return false;
            }
            return true;
        }

        private IAlly FindBestAlly()
        {
            Collider[] alliesInRange = Physics.OverlapSphere(transform.position, _enemyStats.ViewDistance, _enemyStats.HitLayer);

            IAlly bestAlly = null;
            IAlly fallbackNonHealer = null;
            IAlly fallbackHealer = null; 

            float lowestHealthPercentage = 1f;

            foreach (var col in alliesInRange)
            {
                if (!col.TryGetComponent<IAlly>(out var ally)) continue;
                if (ally.IsBeingHealed) continue;

                bool isHealer = ally.Type == EnemyType.Healer;

                if (!isHealer)
                {
                    // healer fallback
                    if (fallbackNonHealer == null)
                        fallbackNonHealer = ally;
                    float healthPct = ally.Healable.CurrentHp / ally.Healable.MaxHp;
                    if (healthPct < lowestHealthPercentage)
                    {
                        lowestHealthPercentage = healthPct;
                        bestAlly = ally;
                    }
                }
                else
                {
                    // if there is no options heal another healer
                    if (fallbackHealer == null)
                        fallbackHealer = ally;
                }
            }

            // Priority: injured -> healthy ally non healer -> healer
            return bestAlly ?? fallbackNonHealer ?? fallbackHealer;
        }
        private void ClearAlly()
        {
            if (_ally != null)
                _ally.IsBeingHealed = false;
            _ally = null;
            _hasHealedOnce = false;
        }
        protected override BehaviorTree BuildTree()
        {
            BehaviorTree tree = new BehaviorTree(base._behaviourTreeName);
            SequenceNode root = new SequenceNode("Root");

            // ---- Heal ally ----
            SequenceNode HealSequence = new SequenceNode("Heal");
            HealSequence.AddChild(new LeafNode("SearchInjuredAlly", new ConditionNode(() => SearchInjuredAlly())));
            HealSequence.AddChild(new LeafNode("IsInRange", new ConditionNode(() => IsInStableDistance(_ally.Transform))));
            LeafNode HealAction = new LeafNode("Heal", new Attack(_animator, _agent, () => EffectiveAttackSpeed, "HealerPHanim"));
            CooldownDecorator attackCooldown = new CooldownDecorator(HealAction, () => EffectiveAttackSpeed);
            HealSequence.AddChild(attackCooldown);

            // ---- Follow Ally ----
            SequenceNode Move = new SequenceNode("Move");
            Move.AddChild(new LeafNode("Follow", new FollowAlly(() => _ally.Transform, transform, _agent, () => EffectiveChaseSpeed)));

            SelectorNode behavior = new SelectorNode("Behavior");
            behavior.AddChild(HealSequence);
            behavior.AddChild(Move);
            behavior.AddChild(new LeafNode("Wait", new Wait(2f)));

            root.AddChild(new LeafNode("NotKnockedBack", new ConditionNode(() => !_knockback.IsKnockedBack)));
            root.AddChild(behavior);
            tree.AddChild(root);
            return tree;
        }
        public void HealALly()
        {
            _hasHealedOnce = true;
            _ally.IsBeingHealed = true;
            //ATK debuff weakens the Healer's healing output!
            float finalHeal = _enemyStats.AttackDamage;
            if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.ATK))
                finalHeal *= 1f - _debuffs.GetDebuffStrength(DebuffType.ATK);

            //Spawn the stationary healing area at the target's feet
            Vector3 spawnPos = GetFlatPos(_ally.Transform.position);
            HealingArea healingArea = Instantiate(_healingAreaPrefab, spawnPos, Quaternion.identity);
            healingArea.Init(_enemyStats.AttackRadius, finalHeal, _enemyStats.HitLayer, _spellLifetime, _spellPulseFrequency);
        }
        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);
        public void OnDrawGizmos()
        {
            if(_ally != null) 
            {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _ally.Transform.position);
            }
        }
    }

}
