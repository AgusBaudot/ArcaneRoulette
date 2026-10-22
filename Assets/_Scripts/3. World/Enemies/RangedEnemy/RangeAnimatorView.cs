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
        [SerializeField] private float _windupClipLength = 0.5f;
        [SerializeField] private float _coverClipLength = 0.3f;
        [SerializeField] private float _uncoverClipLength = 0.3f;
        [SerializeField] private float _teleportClipLength = 0.6f;

        [Header("Visuals")] 
        [SerializeField] private SpriteRenderer _renderer;

        private Animator _animator;
        private RangeAIBrain _brain;
        private NavMeshAgent _agent;
        private AIState _currentState;

        private readonly int _bIsChasing = Animator.StringToHash("b_IsChasing");
        private readonly int _tWindup = Animator.StringToHash("t_Windup");
        private readonly int _tFireNormal = Animator.StringToHash("t_FireNormal");
        private readonly int _tFireBig = Animator.StringToHash("t_FireBig");
        private readonly int _tCover = Animator.StringToHash("t_Cover");
        private readonly int _tUncover = Animator.StringToHash("t_Uncover");
        private readonly int _tTeleportStart = Animator.StringToHash("t_TeleportStart");

        private readonly int _fWindupSpeed = Animator.StringToHash("f_WindupSpeed");
        private readonly int _fCoverSpeed = Animator.StringToHash("f_CoverSpeed");
        private readonly int _fUncoverSpeed = Animator.StringToHash("f_UncoverSpeed");
        private readonly int _fTeleportSpeed = Animator.StringToHash("f_TeleportSpeed");

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
            _brain.OnFire += HandleFire;
            _brain.OnCoverStarted += HandleCoverStarted;
            _brain.OnUncoverStarted += HandleUncoverStarted;
            _brain.OnTeleportStarted += HandleTeleportStarted;
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnWindupStarted -= HandleWindupStarted;
            _brain.OnFire -= HandleFire;
            _brain.OnCoverStarted -= HandleCoverStarted;
            _brain.OnUncoverStarted -= HandleUncoverStarted;
            _brain.OnTeleportStarted -= HandleTeleportStarted;
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
            SetSpeedMultiplier(_fWindupSpeed, _windupClipLength, logicDuration);
            _animator.SetTrigger(_tWindup);
        }

        private void HandleFire(int index)
        {
            if (index == 3)
            {
                _animator.SetTrigger(_tFireBig);
            }
            else
            {
                _animator.SetTrigger(_tFireNormal);
            }
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

        private void HandleTeleportStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fTeleportSpeed, _teleportClipLength, logicDuration);
            _animator.SetTrigger(_tTeleportStart);
        }

        private void SetSpeedMultiplier(int paramHash, float clipLength, float targetDuration)
        {
            if (targetDuration <= 0.001f) return;
            _animator.SetFloat(paramHash, clipLength / targetDuration);
        }
    }
}