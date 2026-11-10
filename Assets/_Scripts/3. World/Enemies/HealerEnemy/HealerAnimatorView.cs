using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(Animator), typeof(HealerAIBrain), typeof(NavMeshAgent))]
    public sealed class HealerAnimatorView : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Animations;

        [Header("Base Clip Lengths (Seconds)")]
        [Tooltip("The raw length of the separated animation files.")]
        [SerializeField] private float _healWindupClipLength = 0.3f;
        [SerializeField] private float _healRecoverClipLength = 0.4f;
        [SerializeField] private float _throwWindupClipLength = 0.3f;
        [SerializeField] private float _throwClipLength = 0.4f;

        [Header("Visuals")] 
        [SerializeField] private SpriteRenderer _renderer;

        private Animator _animator;
        private HealerAIBrain _brain;
        private NavMeshAgent _agent;
        private AIState _currentState;

        private readonly int _bIsChasing = Animator.StringToHash("b_IsChasing");
        private readonly int _bIsHealing = Animator.StringToHash("b_IsHealing");
        
        private readonly int _tHealWindup = Animator.StringToHash("t_HealWindup");
        private readonly int _tHealRecover = Animator.StringToHash("t_HealRecover");
        private readonly int _tThrowWindup = Animator.StringToHash("t_ThrowWindup");
        private readonly int _tThrow = Animator.StringToHash("t_Throw");
        
        private readonly int _fHealWindupSpeed = Animator.StringToHash("f_HealWindupSpeed");
        private readonly int _fHealRecoverSpeed = Animator.StringToHash("f_HealRecoverSpeed");
        private readonly int _fThrowWindupSpeed = Animator.StringToHash("f_ThrowWindupSpeed");
        private readonly int _fThrowSpeed = Animator.StringToHash("f_ThrowSpeed");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _brain = GetComponent<HealerAIBrain>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            
            _animator.Rebind();
            _animator.Update(0f);
            
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnHealWindupStarted += HandleHealWindupStarted;
            _brain.OnHealingStarted += HandleHealingStarted;
            _brain.OnHealRecoverStarted += HandleHealRecoverStarted;
            _brain.OnThrowWindupStarted += HandleThrowWindupStarted;
            _brain.OnThrowStarted += HandleThrowStarted;
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnHealWindupStarted -= HandleHealWindupStarted;
            _brain.OnHealingStarted -= HandleHealingStarted;
            _brain.OnHealRecoverStarted -= HandleHealRecoverStarted;
            _brain.OnThrowWindupStarted -= HandleThrowWindupStarted;
            _brain.OnThrowStarted -= HandleThrowStarted;
        }

        public void Tick(float dt)
        {
            if (_renderer == null) return;

            if (_currentState == AIState.Chase && _agent.velocity.sqrMagnitude > 0.01f)
            {
                _renderer.flipX = _agent.velocity.x > 0.01f;
            }
            else if (_currentState == AIState.Healing || _currentState == AIState.Throwing)
            {
                _renderer.flipX = _brain.CurrentTargetDirection.x > 0.01f;
            }
        }

        private void HandleStateChanged(AIState newState)
        {
            _currentState = newState;
            _animator.SetBool(_bIsChasing, newState == AIState.Chase);
            
            if (newState != AIState.Healing)
            {
                _animator.SetBool(_bIsHealing, false);
            }
        }

        private void HandleHealWindupStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fHealWindupSpeed, _healWindupClipLength, logicDuration);
            _animator.SetTrigger(_tHealWindup);
        }

        private void HandleHealingStarted(float logicDuration)
        {
            _animator.SetBool(_bIsHealing, true);
        }

        private void HandleHealRecoverStarted(float logicDuration)
        {
            _animator.SetBool(_bIsHealing, false);
            SetSpeedMultiplier(_fHealRecoverSpeed, _healRecoverClipLength, logicDuration);
            _animator.SetTrigger(_tHealRecover);
        }

        private void HandleThrowWindupStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fThrowWindupSpeed, _throwWindupClipLength, logicDuration);
            _animator.SetTrigger(_tThrowWindup);
        }

        private void HandleThrowStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fThrowSpeed, _throwClipLength, logicDuration);
            _animator.SetTrigger(_tThrow);
        }

        private void SetSpeedMultiplier(int paramHash, float clipLength, float targetDuration)
        {
            if (targetDuration <= 0.001f) return;
            _animator.SetFloat(paramHash, clipLength / targetDuration);
        }
    }
}