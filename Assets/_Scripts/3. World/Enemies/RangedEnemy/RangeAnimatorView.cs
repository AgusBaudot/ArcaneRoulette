using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(Animator), typeof(RangeAIBrain), typeof(NavMeshAgent))]
    public sealed class RangeAnimatorView : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Animations;

        [Header("Base Clip Lengths (Seconds)")]
        [Tooltip("The raw length of the separated animation files.")]
        [SerializeField] private float[] _windupClipLengths = new float[4] { 0.3f, 0.3f, 0.3f, 0.6f };
        [SerializeField] private float[] _fireClipLengths = new float[4] { 0.2f, 0.2f, 0.2f, 0.4f };
        [SerializeField] private float _coverClipLength = 0.3f;
        [SerializeField] private float _uncoverClipLength = 0.3f;
        [SerializeField] private float _tpInClipLength = 0.5f;
        [SerializeField] private float _tpOutClipLength = 0.5f;
        [SerializeField] private float _recoverClipLength = 0.8f;

        [Header("Visuals")] 
        [SerializeField] private SpriteRenderer _renderer;

        private Animator _animator;
        private RangeAIBrain _brain;
        private NavMeshAgent _agent;
        private AIState _currentState;

        private readonly int _bIsChasing = Animator.StringToHash("b_IsChasing");
        private readonly int _tCover = Animator.StringToHash("t_Cover");
        private readonly int _tUncover = Animator.StringToHash("t_Uncover");
        private readonly int _tTPIn = Animator.StringToHash("t_TPIn");
        private readonly int _tTPOut = Animator.StringToHash("t_TPOut");
        private readonly int _tRecover = Animator.StringToHash("t_Recover");

        private readonly int[] _tWindups = { 
            Animator.StringToHash("t_Windup1"), Animator.StringToHash("t_Windup2"), 
            Animator.StringToHash("t_Windup3"), Animator.StringToHash("t_Windup4") 
        };
        private readonly int[] _tFires = { 
            Animator.StringToHash("t_Fire1"), Animator.StringToHash("t_Fire2"), 
            Animator.StringToHash("t_Fire3"), Animator.StringToHash("t_Fire4") 
        };
        
        private readonly int[] _fWindupSpeeds = { 
            Animator.StringToHash("f_Windup1Speed"), Animator.StringToHash("f_Windup2Speed"), 
            Animator.StringToHash("f_Windup3Speed"), Animator.StringToHash("f_Windup4Speed") 
        };
        private readonly int[] _fFireSpeeds = { 
            Animator.StringToHash("f_Fire1Speed"), Animator.StringToHash("f_Fire2Speed"), 
            Animator.StringToHash("f_Fire3Speed"), Animator.StringToHash("f_Fire4Speed") 
        };

        private readonly int _fCoverSpeed = Animator.StringToHash("f_CoverSpeed");
        private readonly int _fUncoverSpeed = Animator.StringToHash("f_UncoverSpeed");
        private readonly int _fTPInSpeed = Animator.StringToHash("f_TPInSpeed");
        private readonly int _fTPOutSpeed = Animator.StringToHash("f_TPOutSpeed");
        private readonly int _fRecoverSpeed = Animator.StringToHash("f_RecoverSpeed");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _brain = GetComponent<RangeAIBrain>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnWindupStarted += HandleWindupStarted;
            _brain.OnFireStarted += HandleFireStarted;
            _brain.OnCoverStarted += HandleCoverStarted;
            _brain.OnUncoverStarted += HandleUncoverStarted;
            _brain.OnTeleportInStarted += HandleTPInStarted;
            _brain.OnTeleportOutStarted += HandleTPOutStarted;
            _brain.OnRecoverStarted += HandleRecoverStarted;
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnWindupStarted -= HandleWindupStarted;
            _brain.OnFireStarted -= HandleFireStarted;
            _brain.OnCoverStarted -= HandleCoverStarted;
            _brain.OnUncoverStarted -= HandleUncoverStarted;
            _brain.OnTeleportInStarted -= HandleTPInStarted;
            _brain.OnTeleportOutStarted -= HandleTPOutStarted;
            _brain.OnRecoverStarted -= HandleRecoverStarted;
        }

        public void Tick(float dt)
        {
            if (_renderer == null) return;

            if (_currentState == AIState.Chase && _agent.velocity.sqrMagnitude > 0.01f)
            {
                _renderer.flipX = _agent.velocity.x > 0.01f;
            }
            else if (_currentState == AIState.Attack || _currentState == AIState.Blocking || _currentState == AIState.Teleporting)
            {
                _renderer.flipX = _brain.CurrentTargetDirection.x > 0.01f;
            }
        }

        private void HandleStateChanged(AIState newState)
        {
            _currentState = newState;
            _animator.SetBool(_bIsChasing, newState == AIState.Chase);
        }

        private void HandleWindupStarted(int index, float logicDuration)
        {
            SetSpeedMultiplier(_fWindupSpeeds[index], _windupClipLengths[index], logicDuration);
            _animator.SetTrigger(_tWindups[index]);
        }

        private void HandleFireStarted(int index, float logicDuration)
        {
            SetSpeedMultiplier(_fFireSpeeds[index], _fireClipLengths[index], logicDuration);
            _animator.SetTrigger(_tFires[index]);
        }

        private void HandleCoverStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fCoverSpeed, _coverClipLength, logicDuration);
            _animator.SetTrigger(_tCover);
        }

        private void HandleUncoverStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fUncoverSpeed, _uncoverClipLength, logicDuration);
            _animator.SetTrigger(_tUncover);
        }

        private void HandleTPInStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fTPOutSpeed, _tpOutClipLength, logicDuration);
            _animator.SetTrigger(_tTPOut);
        }

        private void HandleTPOutStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fTPInSpeed, _tpInClipLength, logicDuration);
            _animator.SetTrigger(_tTPIn);
        }
        
        private void HandleRecoverStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fRecoverSpeed, _recoverClipLength, logicDuration);
            _animator.SetTrigger(_tRecover);
        }

        private void SetSpeedMultiplier(int paramHash, float clipLength, float targetDuration)
        {
            if (targetDuration <= 0.001f) return;
            _animator.SetFloat(paramHash, clipLength / targetDuration);
        }
    }
}