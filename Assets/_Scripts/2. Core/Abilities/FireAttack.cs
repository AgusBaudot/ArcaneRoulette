using System.Collections;
using UnityEngine;

namespace Core
{
    public class FireAttack : MonoBehaviour
    {
        [SerializeField] private Projectile _fireball;
        
        private Plane _floorPlane;
        private float _fireCooldownTimer;

        private void Start()
        {
            _floorPlane = new Plane(Vector3.up, Vector3.zero);
        }

        private void Update()
        {
            _fireCooldownTimer -= Time.deltaTime;
        }

        public void Execute(PlayerController player, Vector2 inputDirection)
        {
            if (_fireCooldownTimer > 0f)
                return;

            _fireCooldownTimer = 1f / player.Stats.FireRate;
            StartCoroutine(WindUp(player));
        }
        
        private void Attack(PlayerController player)
        {
            Ray ray = Helpers.GetCamera().ScreenPointToRay(Input.mousePosition);
            
            if (_floorPlane.Raycast(ray, out float distance))
            {
                Vector3 dir = (ray.GetPoint(distance) - player.transform.position).normalized;
                
                var go = Instantiate(_fireball, player.transform.position, Quaternion.identity);
                go.Initialize(dir, player.Stats.ProjectileSpeed, player.Stats);
            }
        }
        
        private IEnumerator WindUp(PlayerController player)
        {
            yield return Helpers.GetWait(player.Stats.Windup);
            Attack(player);
        }
    }
}