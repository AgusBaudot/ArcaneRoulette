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

        private Vector2 _lastFacing = Vector2.down;

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
            Vector2 input = Helpers.Input.MoveDirection;
            bool isMoving = input.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                _lastFacing = input.normalized;
            }

            _animator.SetFloat(MoveXHash, _lastFacing.x);
            _animator.SetFloat(MoveYHash, _lastFacing.y);
            _animator.SetBool(IsMovingHash, isMoving);
        }
    }
}