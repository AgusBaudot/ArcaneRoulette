using UnityEngine;

namespace World
{
    /// <summary>
    /// Programmer art! Delete this once the Animator is fully wired up.
    /// </summary>
    public sealed class TempStateVisuals : MonoBehaviour
    {
        [SerializeField] private AIBrain _brain;
        [SerializeField] private SpriteRenderer _sprite;

        private void OnEnable()
        {
            if (_brain != null)
                _brain.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (_brain != null)
                _brain.OnStateChanged -= HandleStateChanged;
                
            if (_sprite != null) 
                _sprite.color = Color.white;
        }

        private void HandleStateChanged(AIState newState)
        {
            if (_sprite == null) return;

            switch (newState)
            {
                case AIState.Attack:
                    _sprite.color = new Color(1f, 0.5f, 0.5f); // Light Red / Pinkish for Windup
                    break;
                case AIState.Stunned:
                    _sprite.color = new Color(0.3f, 0.5f, 1f); // Blueish for Stunned
                    break;
                case AIState.Blocking:
                    _sprite.color = new Color(0.6f, 0.6f, 0.6f); // Gray for Block/Cover
                    break;
                case AIState.Teleporting:
                    _sprite.color = new Color(0.8f, 0.2f, 1f); // Purple/Magenta for Teleport
                    break;
                case AIState.Chase:
                    _sprite.color = Color.white; // Normal
                    break;
                case AIState.Spawning:
                    _sprite.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent for Spawning
                    break;
                default:
                    _sprite.color = Color.white;
                    break;
            }
        }
    }
}