using UnityEngine;

namespace World
{
    [RequireComponent(typeof(Animator), typeof(MeleeAIBrain))]
    public sealed class MeleeAnimatorView : MonoBehaviour
    {
        private Animator _animator;
        private MeleeAIBrain _brain;

        private readonly int _bIsChasing = Animator.StringToHash("b_IsChasing");
        private readonly int _tSpawn = Animator.StringToHash("t_Spawn");
        private readonly int _tAttack = Animator.StringToHash("t_Attack");
        private readonly int _tRecompose = Animator.StringToHash("t_Recompose");
        private readonly int _tDeath = Animator.StringToHash("t_Death");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _brain = GetComponent<MeleeAIBrain>();
        }

        private void OnEnable()
        {
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnSpawnStarted += HandleSpawnStarted;
            _brain.OnWindupStarted += HandleAttackStarted; 
            _brain.OnRecomposing += HandleRecomposingStarted;
        }

        private void OnDisable()
        {
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnSpawnStarted -= HandleSpawnStarted;
            _brain.OnWindupStarted -= HandleAttackStarted;
            _brain.OnRecomposing -= HandleRecomposingStarted;
        }

        private void HandleStateChanged(AIState newState)
        {
            _animator.SetBool(_bIsChasing, newState == AIState.Chase);

            if (newState == AIState.Death)
            {
                _animator.SetTrigger(_tDeath);
            }
        }

        private void HandleSpawnStarted(float duration)
        {
            _animator.SetTrigger(_tSpawn);
        }

        private void HandleAttackStarted(float duration)
        {
            _animator.SetTrigger(_tAttack);
        }

        private void HandleRecomposingStarted(float duration)
        {
            _animator.SetTrigger(_tRecompose);
        }
    }
}