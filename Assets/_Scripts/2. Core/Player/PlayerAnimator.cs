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

        // Track the filtered cardinal direction and the raw input history
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
                // 1. Strictly horizontal input
                if (Mathf.Abs(rawInput.x) > 0.01f && Mathf.Abs(rawInput.y) < 0.01f)
                {
                    _currentAnimDir = new Vector2(Mathf.Sign(rawInput.x), 0f);
                }
                // 2. Strictly vertical input
                else if (Mathf.Abs(rawInput.y) > 0.01f && Mathf.Abs(rawInput.x) < 0.01f)
                {
                    _currentAnimDir = new Vector2(0f, Mathf.Sign(rawInput.y));
                }
                // 3. Diagonal input detected
                else 
                {
                    // Calculate which axis experienced the largest change this frame
                    float deltaX = Mathf.Abs(rawInput.x - _lastRawInput.x);
                    float deltaY = Mathf.Abs(rawInput.y - _lastRawInput.y);

                    if (deltaX > deltaY)
                    {
                        // X changed more drastically (e.g., from 0 to 0.707)
                        _currentAnimDir = new Vector2(Mathf.Sign(rawInput.x), 0f);
                    }
                    else if (deltaY > deltaX)
                    {
                        // Y changed more drastically
                        _currentAnimDir = new Vector2(0f, Mathf.Sign(rawInput.y));
                    }
                    // If deltaX == deltaY (e.g., dead stop straight to diagonal), 
                    // _currentAnimDir safely retains its previous frame's facing direction.
                }
            }

            // Cache the raw input to compare against on the next frame
            _lastRawInput = rawInput;

            // Feed strictly cardinal vectors (0, 1), (1, 0), etc., to the Animator
            _animator.SetFloat(MoveXHash, _currentAnimDir.x);
            _animator.SetFloat(MoveYHash, _currentAnimDir.y);
            _animator.SetBool(IsMovingHash, isMoving);
        }
    }
}