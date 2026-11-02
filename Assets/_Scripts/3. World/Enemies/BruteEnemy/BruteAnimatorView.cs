using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(Animator), typeof(BruteAIBrain), typeof(NavMeshAgent))]
    public sealed class BruteAnimatorView : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Animations;

        [Header("Base Clip Lengths (Seconds)")]
        [SerializeField] private float _thrustWindupClipLength = 2.30f;
        [SerializeField] private float _thrustClipLength = 0.58f;
        [SerializeField] private float _chargeWindupClipLength = 1.10f;
        [SerializeField] private float _stunRecoverClipLength = 0.60f;
        [SerializeField] private float _chargeMissClipLength = 1.10f; 

        [Header("Visuals")] 
        [SerializeField] private SpriteRenderer _renderer;

        private Animator _animator;
        private BruteAIBrain _brain;
        private NavMeshAgent _agent;
        private AIState _currentState;

        private readonly int _bIsChasing = Animator.StringToHash("b_IsChasing");
        private readonly int _bIsCharging = Animator.StringToHash("b_IsCharging");
        private readonly int _bIsStunned = Animator.StringToHash("b_IsStunned");

        private readonly int _tThrustWindup = Animator.StringToHash("t_ThrustWindup");
        private readonly int _tThrust = Animator.StringToHash("t_Thrust");
        private readonly int _tChargeWindup = Animator.StringToHash("t_ChargeWindup");
        private readonly int _tStunRecover = Animator.StringToHash("t_StunRecover");
        private readonly int _tEndCharge = Animator.StringToHash("t_EndCharge");

        private readonly int _fThrustWindupSpeed = Animator.StringToHash("f_ThrustWindupSpeed");
        private readonly int _fThrustSpeed = Animator.StringToHash("f_ThrustSpeed");
        private readonly int _fChargeWindupSpeed = Animator.StringToHash("f_ChargeWindupSpeed");
        private readonly int _fStunRecoverSpeed = Animator.StringToHash("f_StunRecoverSpeed");
        private readonly int _fChargeMissSpeed = Animator.StringToHash("f_ChargeMissSpeed");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _brain = GetComponent<BruteAIBrain>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnThrustWindupStarted += HandleThrustWindup;
            _brain.OnThrustStarted += HandleThrust;
            _brain.OnChargeWindupStarted += HandleChargeWindup;
            _brain.OnChargeStarted += HandleCharge;
            _brain.OnChargeEndedNaturally += HandleChargeEndedNaturally;
            _brain.OnStunLoopStarted += HandleStunLoop;
            _brain.OnStunRecoverStarted += HandleStunRecover;
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnThrustWindupStarted -= HandleThrustWindup;
            _brain.OnThrustStarted -= HandleThrust;
            _brain.OnChargeWindupStarted -= HandleChargeWindup;
            _brain.OnChargeStarted -= HandleCharge;
            _brain.OnChargeEndedNaturally -= HandleChargeEndedNaturally;
            _brain.OnStunLoopStarted -= HandleStunLoop;
            _brain.OnStunRecoverStarted -= HandleStunRecover;
        }

        public void Tick(float dt)
        {
            if (_renderer == null) return;

            if (_currentState == AIState.Chase && _agent.velocity.sqrMagnitude > 0.01f)
            {
                _renderer.flipX = _agent.velocity.x > 0.01f;
            }
            else if (_currentState == AIState.Attack)
            {
                _renderer.flipX = _brain.CurrentAttackDirection.x > 0.01f;
            }
        }

        private void HandleStateChanged(AIState newState)
        {
            _currentState = newState;
            _animator.SetBool(_bIsChasing, newState == AIState.Chase);

            if (newState != AIState.Attack) _animator.SetBool(_bIsCharging, false);
            if (newState != AIState.Stunned) _animator.SetBool(_bIsStunned, false);
        }

        private void SetSpeedMultiplier(int paramHash, float clipLength, float targetDuration)
        {
            if (targetDuration <= 0.001f) return;
            _animator.SetFloat(paramHash, clipLength / targetDuration);
        }

        private void HandleThrustWindup(float duration)
        {
            SetSpeedMultiplier(_fThrustWindupSpeed, _thrustWindupClipLength, duration);
            _animator.SetTrigger(_tThrustWindup);
        }

        private void HandleThrust(float duration)
        {
            SetSpeedMultiplier(_fThrustSpeed, _thrustClipLength, duration);
            _animator.SetTrigger(_tThrust);
        }

        private void HandleChargeWindup(float duration)
        {
            SetSpeedMultiplier(_fChargeWindupSpeed, _chargeWindupClipLength, duration);
            _animator.SetTrigger(_tChargeWindup);
        }

        private void HandleCharge()
        {
            _animator.SetBool(_bIsCharging, true);
        }

        private void HandleChargeEndedNaturally(float duration)
        {
            _animator.SetBool(_bIsCharging, false);
            SetSpeedMultiplier(_fChargeMissSpeed, _chargeMissClipLength, duration);
            _animator.SetTrigger(_tEndCharge); 
        }

        private void HandleStunLoop()
        {
            _animator.SetBool(_bIsStunned, true);
        }

        private void HandleStunRecover(float duration)
        {
            _animator.SetBool(_bIsStunned, false);
            SetSpeedMultiplier(_fStunRecoverSpeed, _stunRecoverClipLength, duration);
            _animator.SetTrigger(_tStunRecover);
        }
    }
}