using UnityEngine;
using Foundation;
using System.Collections;

namespace World
{
    public class HealingArea : MonoBehaviour
    {
        private float _radius;
        private float _healAmount;
        private LayerMask _allyMask;

        public void Init(float radius, float amount, LayerMask allyMask, float lifetime = 2, float frequency = 0.5f)
        {
            _radius = radius;
            _healAmount = amount;
            _allyMask = allyMask;

            // Optional: Scale a visual indicator (like a green circle decal) to match the radius
            // transform.localScale = new Vector3(_radius * 2, 0.1f, _radius * 2);

            StartCoroutine(HealRoutine(lifetime, frequency));
        }

        private IEnumerator HealRoutine(float lifetime, float frequency)
        {
            float elapsedTime = 0f;

            while (elapsedTime < lifetime)
            {
                yield return CoroutineUtils.GetWait(frequency);
                elapsedTime += frequency;
                
                Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _allyMask);
                
                foreach (var hit in hits)
                {
                    if (hit.TryGetComponent<IHealable>(out var healable))
                    {
                        healable.Heal(_healAmount);
                    }
                }
                
                //Play burst particle effect to show burst here.
            }

            // Clean up the spell object
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, _radius);
        }
    }
}