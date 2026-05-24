using Core;
using Foundation;
using UnityEngine;

namespace World 
{
    public class ExplosionArea : MonoBehaviour
    {
        private float _radius;
        private float _dmgAmount;
        private LayerMask _playerMask;
        private Transform _playerTransform;

        public void Init(float radius, float dmgAmount, LayerMask playerMask, Transform playerTransform, float lifetime = 1)
        {
            _radius = radius;
            _dmgAmount = dmgAmount;
            _playerMask = playerMask;
            _playerTransform = playerTransform;

            PlayAttackVFX();

            Destroy(gameObject, lifetime);
        }
        void PlayAttackVFX()
        {
            Collider[] hits = new Collider[1];
            int count = Physics.OverlapSphereNonAlloc(transform.position, _radius, hits, _playerMask);

            if (count > 0 && hits[0] != null && hits[0].TryGetComponent<IDamageable>(out IDamageable player))
            {
                DamageSystem.Deal(player, _playerTransform.gameObject, (int)_dmgAmount, ElementType.Neutral);
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, _radius);
        }
    }

}
