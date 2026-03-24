using System;
using UnityEngine;
using Foundation;

namespace Core
{
    //Sits on the shield visual prefab.
    //ShieldAbilityRune configures it at StartHold and reads results via events.
    [RequireComponent(typeof(Collider))]
    public sealed class ShieldCollider : MonoBehaviour
    {
        public bool ReflectsProjectiles { get; set; }
        
        //ShieldAbilityRune subscribes to this in StartHold
        public event Action<Vector3, GameObject> OnProjectileAbsorbed;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<IProjectile>(out var projectile))
                return;
            if (!projectile.IsEnemy)
                return; //player projectiles pass through

            if (ReflectsProjectiles)
                projectile.Rb.velocity = -projectile.Rb.velocity;
            else
                Destroy(other.gameObject);
            
            OnProjectileAbsorbed?.Invoke(transform.position, other.gameObject);
        }
    }
}