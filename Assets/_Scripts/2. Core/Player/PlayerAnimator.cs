using UnityEngine;
using Foundation; 

namespace Core
{
    public sealed class PlayerAnimator : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Animations;

        [SerializeField] private Animator _animator;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsShieldingHash = Animator.StringToHash("IsShielding");
        private static readonly int Death = Animator.StringToHash("t_Death");
        private static readonly int DashLeftHash = Animator.StringToHash("t_DashLeft");
        private static readonly int DashRightHash = Animator.StringToHash("t_DashRight");

        private Vector2 _currentAnimDir = Vector2.down; 
        private Vector2 _lastRawInput = Vector2.zero;
        private AnimatorUpdateMode _defaultUpdateMode;
        
        private PlayerController _playerController;

        private void Awake()
        {
            _defaultUpdateMode = _animator.updateMode;
            _playerController = GetComponentInParent<PlayerController>();
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this); 
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDeath);
            EventBus.Subscribe<PlayerRevivedEvent>(OnPlayerRevived);
            EventBus.Subscribe<PlayerDashedEvent>(OnPlayerDashed); 
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDeath);
            EventBus.Unsubscribe<PlayerRevivedEvent>(OnPlayerRevived);
            EventBus.Unsubscribe<PlayerDashedEvent>(OnPlayerDashed);
        }

        public void Tick(float dt)
        {
            Vector2 rawInput = Helpers.Input.MoveDirection;
            bool isMoving = rawInput.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                if (Mathf.Abs(rawInput.x) > 0.01f && Mathf.Abs(rawInput.y) < 0.01f)
                {
                    _currentAnimDir = new Vector2(Mathf.Sign(rawInput.x), 0f);
                }
                else if (Mathf.Abs(rawInput.y) > 0.01f && Mathf.Abs(rawInput.x) < 0.01f)
                {
                    _currentAnimDir = new Vector2(0f, Mathf.Sign(rawInput.y));
                }
                else 
                {
                    float deltaX = Mathf.Abs(rawInput.x - _lastRawInput.x);
                    float deltaY = Mathf.Abs(rawInput.y - _lastRawInput.y);

                    if (deltaX > deltaY)
                    {
                        _currentAnimDir = new Vector2(Mathf.Sign(rawInput.x), 0f);
                    }
                    else if (deltaY > deltaX)
                    {
                        _currentAnimDir = new Vector2(0f, Mathf.Sign(rawInput.y));
                    }
                }
            }

            _lastRawInput = rawInput;

            _animator.SetFloat(MoveXHash, _currentAnimDir.x);
            _animator.SetFloat(MoveYHash, _currentAnimDir.y);
            _animator.SetBool(IsMovingHash, isMoving);
            
            _animator.SetBool(IsShieldingHash, _playerController.IsShielding);
        }
        
        private void OnPlayerDashed(PlayerDashedEvent evt)
        {
            float dirX = Mathf.Abs(evt.Direction.x) > 0.01f ? evt.Direction.x : _currentAnimDir.x;
            
            if (dirX < 0f)
                _animator.SetTrigger(DashLeftHash);
            else
                _animator.SetTrigger(DashRightHash);
        }

        private void OnPlayerDeath(PlayerDiedEvent _)
        {
            _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _animator.SetTrigger(Death);
        }

        private void OnPlayerRevived(PlayerRevivedEvent _)
        {
            _animator.updateMode = _defaultUpdateMode;
            _animator.Rebind();
            _animator.Update(0f);
        }
    }
}