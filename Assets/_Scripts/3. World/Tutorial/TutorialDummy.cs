using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(DamageFlash))]
    public class TutorialDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] private int _health = 20;

        public bool TakeDamage(int amount, ElementType elementType)
        {
            if (_health <= 0) return false;

            _health -= amount;

            if (_health <= 0)
            {
                GameStateManager.RunState.FireKill(default(KillContext));
                Destroy(gameObject);
            }

            return true;
        }
    }
}