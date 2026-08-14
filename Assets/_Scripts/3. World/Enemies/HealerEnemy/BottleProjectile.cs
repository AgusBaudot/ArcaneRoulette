using System.Collections;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class BottleProjectile : BaseProjectile, IEnemyProjectile
    {
        public override bool IsEnemy => _isEnemy;
        public override ElementType SpellElement => Element;

        // IEnemyProjectile implementation
        public int Damage { get; private set; }
        public ElementType Element { get; private set; }
        public GameObject Owner { get; private set; }

        [SerializeField] private BottleHazardArea _hazardPrefab;
        [SerializeField] private float _arcHeight = 3f;

        private bool _isEnemy = true;
        private Vector3 _startPos;
        private Vector3 _targetPos;
        private float _progress;
        private float _travelDuration;

        public void InitEnemyBottle(Vector3 dir, float speed, int damage, ElementType element, GameObject owner, Vector3 targetPos)
        {
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
                hazard.InitHazard(!IsEnemy); // If reflected by player, it affects enemies
            }
            Helpers.ProjFactory.Despawn(gameObject);
        }
        
        // Fired globally by a manager if a sequence bounce occurs.
        public void SpawnSequentialReflection(Vector3 dir, float speed, int bounceRunes)
        {
            _isEnemy = false;
            Damage = Mathf.RoundToInt(Helpers.PlayerStats.BaseDamage * 0.5f);
            
            _startPos = transform.position;
            _targetPos = transform.position + dir * 10f; 
            _travelDuration = 10f / speed;
            _progress = 0f;
            
            SetVelocity(dir, speed);
        }
    }
}