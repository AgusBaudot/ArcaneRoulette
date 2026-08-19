using System.Collections;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class BottleProjectile : BaseProjectile, IEnemyProjectile, ICustomReflectable
    {
        public override bool IsEnemy => _isEnemy;
        public override ElementType SpellElement => Element;

        public int Damage { get; private set; }
        public ElementType Element { get; private set; }
        public GameObject Owner { get; private set; }

        [SerializeField] private BottleHazardArea _hazardPrefab;
        [SerializeField] private float _arcHeight = 3f;
        [Tooltip("Amount of time before next bottle is thrown.")]
        [SerializeField] private float _sequenceWait = 0.15f;

        private BottleProjectile _bottlePrefab;
        private bool _isEnemy = true;
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _progress;
        private float _travelDuration;

        public void InitEnemyBottle(BottleProjectile prefab, Vector3 dir, float speed, int damage, ElementType element, GameObject owner, Vector3 targetPos)
        {
            _bottlePrefab = prefab;
            
            _isEnemy = true;
            Damage = damage;
            Element = element;
            Owner = owner;
            
            _startPos = transform.position;
            _targetPos = targetPos;
            
            float distance = Vector3.Distance(_startPos, _targetPos);
            _travelDuration = distance / speed;
            _progress = 0f;

            SetVelocity(dir, speed);
            BounceCount = 0; 
        }

        private void Update()
        {
            if (_travelDuration <= 0f) return;

            _progress += Time.deltaTime / _travelDuration;
            
            Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, _progress);
            currentPos.y += Mathf.Sin(Mathf.Clamp01(_progress) * Mathf.PI) * _arcHeight;
            Rb.MovePosition(currentPos);

            if (_progress >= 1f)
            {
                Detonate(currentPos);
            }
        }

        protected override void OnHitDamageable(Collider other)
        {
            var batch = new DamageBatch();
            batch.Deal(other.GetComponentInParent<IDamageable>(), other.gameObject, Damage, Element);
            batch.Commit(IsEnemy ? Helpers.Combat.PlayerDamage : Helpers.Combat.NormalDMG);
            Detonate(transform.position);
        }

        protected override void OnHitWall(Collider other)
        {
            Detonate(transform.position);
        }

        private void Detonate(Vector3 pos)
        {
            if (_hazardPrefab != null)
            {
                var hazard = Instantiate(_hazardPrefab, new Vector3(pos.x, 0f, pos.z), Quaternion.identity);
                hazard.InitHazard(!IsEnemy);
            }
            Helpers.ProjFactory.Despawn(gameObject);
        }
        
        public bool TryCustomReflect(Vector3 reflectDir, int bounceRunes, IStatResolver playerStats)
        {
            float baseAtk = playerStats != null ? playerStats.AttackDamage : 10f;
            int reflectionDamage = Mathf.Max(1, Mathf.RoundToInt(baseAtk * 0.5f));

            // Determine where to throw it back! Target the Owner if still alive, else use a fallback.
            Vector3 targetReturnPos = Owner != null ? Owner.transform.position : transform.position + reflectDir * 10f;

            Helpers.ProjFactory.StartCoroutine(SequentialReflectionRoutine(
                reflectDir, 
                Speed, 
                reflectionDamage, 
                bounceRunes,
                targetReturnPos
            ));

            return true;
        }
        
        private IEnumerator SequentialReflectionRoutine(Vector3 dir, float speed, int damage, int bounceRunes, Vector3 targetPos)
        { 
            Vector3 spawnPoint = transform.position;

            for (int i = 0; i < bounceRunes; i++)
            {
                var reflectedBottle = Helpers.ProjFactory.Spawn<BottleProjectile>(_bottlePrefab, spawnPoint, Quaternion.LookRotation(dir));
                
                reflectedBottle.SpawnSequentialReflection(dir, speed, damage, targetPos);
                yield return CoroutineUtils.GetWait(0.15f);
            }
        }

        public void SpawnSequentialReflection(Vector3 dir, float speed, int damage, Vector3 targetPos)
        {
            _isEnemy = false;
            Damage = damage;
            
            _startPos = transform.position;
            _targetPos = targetPos;
            
            float distance = Vector3.Distance(_startPos, _targetPos);
            _travelDuration = distance / Mathf.Max(0.1f, speed);
            _progress = 0f;
            
            SetVelocity(dir, speed);
        }
    }
}