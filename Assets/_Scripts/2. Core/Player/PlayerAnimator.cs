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

        private Vector2 _currentAnimDir = Vector2.down; 
        private Vector2 _lastRawInput = Vector2.zero;

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this); 
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
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
        }
    }
}