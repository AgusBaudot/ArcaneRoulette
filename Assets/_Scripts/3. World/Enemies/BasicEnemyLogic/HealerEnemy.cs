using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Rigidbody), typeof(KnockbackHandler), typeof(EnemyHealth))]
    public class HealerEnemy : BaseEnemy, IDebuffReceiver
    {
        [Header("Stats")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _healAmount = 10f;
        [SerializeField] private float _healCooldown = 2f;

        [Header("Areas")]
        [SerializeField] private float _fleeArea = 6f;
        [SerializeField] private float _detectionArea = 15f;
        [SerializeField] private float _healingRadius = 4f; 
        [SerializeField] private LayerMask _allyMask;

        [Header("Prefabs")]
        [SerializeField] private HealingArea _healingAreaPrefab;
        
        [Header("Spell Settings")]
        [SerializeField] private float _spellLifetime = 2f;
        [SerializeField] private float _spellPulseFrequency = 0.5f;

        private Rigidbody _rb;
        private KnockbackHandler _knockback;
        private float _lastHealTime;
        
        private IDebuffReadable _debuffs;
        
        private float EffectiveAttackSpeed
        {
            get
            {
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.AttackSpeed))
                {
                    return Mathf.Max(0.1f, 1f - _debuffs.GetDebuffStrength(DebuffType.AttackSpeed));
                }
                return 1f;
            }
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _knockback = GetComponent<KnockbackHandler>();
        }

        private void FixedUpdate()
        {
            if (_playerTarget == null || _knockback.IsKnockedBack) return;

            Vector3 flatMyPos = GetFlatPos(transform.position);
            Vector3 flatPlayerPos = GetFlatPos(_playerTarget.position);
            float distanceToPlayer = Vector3.Distance(flatMyPos, flatPlayerPos);

            //Flee logic
            float finalSpeed = _moveSpeed;
            if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.Speed))
                finalSpeed *= 1f - _debuffs.GetDebuffStrength(DebuffType.Speed);
            
            if (distanceToPlayer < _fleeArea)
            {
                Vector3 dirAway = (flatMyPos - flatPlayerPos).normalized;
                _rb.velocity = dirAway * finalSpeed;
            }
            else
            {
                _rb.velocity = Vector3.zero; // Safe, stand still
            }

            //Cast logic
            if (Time.time >= _lastHealTime + _healCooldown / EffectiveAttackSpeed)
            {
                TryCastHeal();
            }
        }

        private void TryCastHeal()
        {
            //Find all allies in the detection area
            Collider[] alliesInRange = Physics.OverlapSphere(transform.position, _detectionArea, _allyMask);
                
            Transform bestTarget = null;
            float lowestHealthPercentage = 1f; // Start at 100%

            foreach (var col in alliesInRange)
            {
                if (col.TryGetComponent<IHealable>(out var ally))
                {
                    float healthPct = ally.CurrentHp / ally.MaxHp;
                    
                    //Prioritize whoever is missing the most health percentage
                    if (healthPct < lowestHealthPercentage && healthPct < 1f)
                    {
                        lowestHealthPercentage = healthPct;
                        bestTarget = col.transform;
                    }
                }
            }

            //If we found an injured ally, cast the spell
            if (bestTarget != null)
            {
                _lastHealTime = Time.time;

                //ATK debuff weakens the Healer's healing output!
                float finalHeal = _healAmount;
                if (_debuffs != null && _debuffs.IsDebuffed(DebuffType.ATK))
                    finalHeal *= 1f - _debuffs.GetDebuffStrength(DebuffType.ATK);

                //Spawn the stationary healing area at the target's feet
                Vector3 spawnPos = GetFlatPos(bestTarget.position);
                var healingArea = Instantiate(_healingAreaPrefab, spawnPos, Quaternion.identity);
                
                healingArea.Init(_healingRadius, finalHeal, _allyMask, _spellLifetime, _spellPulseFrequency);
            }
        }

        private Vector3 GetFlatPos(Vector3 pos) => new Vector3(pos.x, 0f, pos.z);

        //Draw the 3 areas in the editor so the designer can tune them easily
        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(pos, _fleeArea); // Red = Flee
            Gizmos.color = new Color(0, 0, 1, 0.2f);
            Gizmos.DrawWireSphere(pos, _detectionArea); // Blue = Detect
        }

        //IDebuffReceiver Implementation---------------------------
        public void RegisterDebuff(IDebuffReadable debuff) => _debuffs = debuff;
        public void UnregisterDebuff() => _debuffs = null;
    }
}