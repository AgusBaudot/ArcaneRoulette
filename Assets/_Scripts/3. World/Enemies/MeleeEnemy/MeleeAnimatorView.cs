using Foundation;
using UnityEngine;
using UnityEngine.AI;

namespace World
{
    [RequireComponent(typeof(Animator), typeof(MeleeAIBrain), typeof(NavMeshAgent))]
    public sealed class MeleeAnimatorView : MonoBehaviour, IUpdatable
    {
        #region IUpdatable

        public int UpdatePriority => Foundation.UpdatePriority.Animations;

        #endregion
        
        [Header("Base Clip Lengths (Seconds)")]
        [Tooltip("The raw length of the separated animation files.")]
        [SerializeField] private float _windupClipLength = 0.5f;
        [SerializeField] private float _swing1ClipLength = 0.8f;
        [SerializeField] private float _swing2ClipLength = 0.8f;
        [SerializeField] private float _swing3ClipLength = 1.0f;
        [SerializeField] private float _recomposeClipLength = 1.0f;

        [Header("Visuals")] [SerializeField] private SpriteRenderer _renderer;

        private Animator _animator;
        private MeleeAIBrain _brain;
        private NavMeshAgent _agent;
        private AIState _currentState;

        private readonly int _bIsChasing = Animator.StringToHash("b_IsChasing");
        private readonly int _tAttack = Animator.StringToHash("t_Attack");
        private readonly int _tSwing1 = Animator.StringToHash("t_Swing1");
        private readonly int _tSwing2 = Animator.StringToHash("t_Swing2");
        private readonly int _tSwing3Dash = Animator.StringToHash("t_Swing3");
        private readonly int _tRecompose = Animator.StringToHash("t_Recompose");

        private readonly int _fWindupSpeed = Animator.StringToHash("f_WindupSpeed");
        private readonly int _fSwing1Speed = Animator.StringToHash("f_Swing1Speed");
        private readonly int _fSwing2Speed = Animator.StringToHash("f_Swing2Speed");
        private readonly int _fSwing3DashSpeed = Animator.StringToHash("f_Swing3Speed");
        private readonly int _fRecomposeSpeed = Animator.StringToHash("f_RecomposeSpeed");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _brain = GetComponent<MeleeAIBrain>();
            _agent = GetComponent<NavMeshAgent>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            
            _brain.OnStateChanged += HandleStateChanged;
            _brain.OnWindupStarted += HandleWindupStarted;
            _brain.OnSwingStarted += HandleSwingStarted;
            _brain.OnDashStarted += HandleDashStarted;
            _brain.OnRecomposing += HandleRecomposingStarted;
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            
            _brain.OnStateChanged -= HandleStateChanged;
            _brain.OnWindupStarted -= HandleWindupStarted;
            _brain.OnSwingStarted -= HandleSwingStarted;
            _brain.OnDashStarted -= HandleDashStarted;
            _brain.OnRecomposing -= HandleRecomposingStarted;
        }

        public void Tick(float dt)
        {
            if (_renderer == null) return;

            if (_currentState == AIState.Chase && _agent.velocity.sqrMagnitude > 0.01f)
            {
                _renderer.flipX = _agent.velocity.x > 0.01f;
                _renderer.transform.localPosition = new Vector3(_renderer.flipX ? 1 : -0.5f, 0, 0);
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
        }

        private void HandleWindupStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fWindupSpeed, _windupClipLength, logicDuration);
            _animator.SetTrigger(_tAttack);
        }

        private void HandleSwingStarted(int index, float swingDuration)
        {
            MeleeEnemyStats stats = _brain.ActiveMeleeStats;

            if (index == 0)
            {
                float totalTime = _brain.CurrentAttack12Duration + stats.Attack1EndDelay;
                SetSpeedMultiplier(_fSwing1Speed, _swing1ClipLength, totalTime);
                _animator.SetTrigger(_tSwing1);
            }
            else if (index == 1)
            {
                float totalTime = _brain.CurrentAttack12Duration + stats.Attack2EndDelay;
                SetSpeedMultiplier(_fSwing2Speed, _swing2ClipLength, totalTime);
                _animator.SetTrigger(_tSwing2);
            }
        }

        private void HandleDashStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fSwing3DashSpeed, _swing3ClipLength, logicDuration);
            _animator.SetTrigger(_tSwing3Dash);
        }

        private void HandleRecomposingStarted(float logicDuration)
        {
            SetSpeedMultiplier(_fRecomposeSpeed, _recomposeClipLength, logicDuration);
            _animator.SetTrigger(_tRecompose);
        }

        private void SetSpeedMultiplier(int paramHash, float clipLength, float targetDuration)
        {
            if (targetDuration <= 0.001f) return;
            _animator.SetFloat(paramHash, clipLength / targetDuration);
        }
    }
}