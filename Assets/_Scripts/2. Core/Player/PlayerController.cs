using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerStats Stats => _playerStats;
        public Rigidbody Rigidbody => _rb;
        
        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private Transform _spriteTransform;
        [SerializeField] private FireAttack _slot1; //Basic attack
        [SerializeField] private WindDash _slot2; //Dash
        [SerializeField] private FireShield _slot3; //Shield

        private Rigidbody _rb;

        private Vector2 _input;
        private Vector3 _velocity;
        private Vector2 _facingDirection = Vector2.right;

        private bool _canMove = true;


        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                            | RigidbodyConstraints.FreezeRotation;
        }

        private void OnValidate()
        {
            if (!_playerStats)
                Debug.LogWarning("PlayerStats SO must be assigned.", this);
        }

        private void Update()
        {
            ReadInput();
        }

        private void FixedUpdate()
        {
            if (!_canMove) return;
            HandleMovement();
        }

        private void ReadInput()
        {
            _input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")).normalized;
            
            var dir = _input.sqrMagnitude > 0.01f ? _input : _facingDirection;
            
            if (Input.GetKeyDown(_playerStats.BasicAttack))
                _slot1.Execute(this, dir);

            if (Input.GetKeyDown(_playerStats.DashKey))
                _slot2.Execute(this, dir);
            
            if (Input.GetKeyDown(_playerStats.DefenseKey))
                _slot3.Execute(this, dir);
        }

        private void HandleMovement()
        {
            // Input XY maps to world XZ — Y axis is reserved for gravity/height
            Vector3 targetVelocity = new Vector3(_input.x, 0f, _input.y) * _playerStats.BaseSpeed;

            float rate = _input.sqrMagnitude > 0.01f
                ? _playerStats.Acceleration
                : _playerStats.Deceleration;

            _velocity = Vector3.MoveTowards(_velocity, targetVelocity, rate * Time.deltaTime);
            _rb.velocity = _velocity;

            if (_input.sqrMagnitude > 0.01f)
            {
                _facingDirection = _input;
                UpdateSpriteFlip();
            }
        }

        private void UpdateSpriteFlip()
        {
            if (_spriteTransform == null) return;
            _spriteTransform.localScale = new Vector3(
                _facingDirection.x < 0f ? -10f : 10f, 10f, 1f);
        }
        
        public void SetCanMove(bool canMove) => _canMove = canMove;
    }
}