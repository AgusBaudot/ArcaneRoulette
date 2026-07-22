using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour, IUpdatable, IFixedUpdatable
    {
        #region Properties

        public PlayerStats Stats => _playerStats;
        public Rigidbody Rigidbody => _rb;
        public PlayerHealth Health => _health;
        public GameObject Hurtbox => _hurtBox;
        //True when a HoldSpellInstance with an active ShieldState is the last-pressed hold.
        public bool IsShielding
        {
            get
            {
                if (_heldHoldSlots.Count == 0) return false;
                var slot = _spellSlots[_heldHoldSlots[^1]];
                return slot?.ShieldState?.Active == true;
            }
        }
        
        //IUpdatable
        public int UpdatePriority => Foundation.UpdatePriority.Input;
        public int FixedUpdatePriority => Foundation.UpdatePriority.Player;

        //Last intentional input direction - used by DashAbilityRune for dash direction.
        //Falls back to facing direction when stick/WASD is neutral.
        public Vector2 LastInputDirection => _input.sqrMagnitude > 0.01f ? _input : _facingDirection;
        #endregion
        
        #region Variables & State
        
        [Header("Dependencies")]
        [SerializeField] private Transform _spriteTransform;
        [SerializeField] private GameObject _hurtBox;

        //Populated by DebugSpellSeeder in Phase 1, by AttunementSystem in Phase 2.
        private readonly SpellInstance[] _spellSlots = new SpellInstance[3];

        private readonly List<int> _heldHoldSlots = new(); //Insertion order = press order
        private readonly HashSet<int> _heldAutoSlots = new();

        private Rigidbody _rb;
        public Rigidbody Rb => _rb;
        private PlayerHealth _health;
        private PlayerStats _playerStats;

        private Vector2 _input;
        private Vector3 _velocity;
        private Vector2 _facingDirection = Vector2.right;
        private bool _canMove = true;
        private bool _isAlive = true;
        private PlayerInputActions _inputActions;
        private AudioHandle _runAudioHandle;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotation;

            _playerStats = Resources.Load<PlayerStats>("PlayerStats");

            _health = GetComponent<PlayerHealth>();
            _health.Initialize(_playerStats);
            
            GetComponentInChildren<PlayerHurtBox>()?.Initialize(_health);
            
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);
            EventBus.Subscribe((PlayerDiedEvent _) => _isAlive = false);
            EventBus.Subscribe<PlayerTeleportRequestEvent>(TeleportTo);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SpellEquippedEvent>(OnSpellEquipped);
            EventBus.Unsubscribe<PlayerTeleportRequestEvent>(TeleportTo);
        }

        private void OnEnable()
        {
            //Update Manager
            UpdateManager.Instance.Register((IUpdatable)this);
            UpdateManager.Instance.Register((IFixedUpdatable)this);
            
            //Input
            Helpers.Input.OnSlot0Started += HandleSlot0Press;
            Helpers.Input.OnSlot1Started += HandleSlot1Press;
            Helpers.Input.OnSlot2Started += HandleSlot2Press;
            
            Helpers.Input.OnSlot0Canceled += HandleSlot0Release;
            Helpers.Input.OnSlot1Canceled += HandleSlot1Release;
            Helpers.Input.OnSlot2Canceled += HandleSlot2Release;
            
            _heldAutoSlots.Clear();
            _heldHoldSlots.Clear();
            if (_runAudioHandle != null && _runAudioHandle.IsValid)
            {
                EventBus.Publish(new AudioStopRequest
                {
                    Handle = _runAudioHandle,
                    FadeOut = false
                });
                
                _runAudioHandle = null;
            }
        }

        private void OnDisable()
        {
            //Update Manager
            UpdateManager.Instance?.Unregister((IUpdatable)this);
            UpdateManager.Instance?.Unregister((IFixedUpdatable)this);
            
            //Input
            Helpers.Input.OnSlot0Started -= HandleSlot0Press;
            Helpers.Input.OnSlot1Started -= HandleSlot1Press;
            Helpers.Input.OnSlot2Started -= HandleSlot2Press;
            
            Helpers.Input.OnSlot0Canceled -= HandleSlot0Release;
            Helpers.Input.OnSlot1Canceled -= HandleSlot1Release;
            Helpers.Input.OnSlot2Canceled -= HandleSlot2Release;

            if (_runAudioHandle != null && _runAudioHandle.IsValid)
            {
                EventBus.Publish(new AudioStopRequest
                {
                    Handle = _runAudioHandle,
                    FadeOut = false
                });
                
                _runAudioHandle = null;
            }
        }

        private void Start()
        {
            Helpers.Input.EnablePlayerInput();
        }

        #endregion

        #region Update Loops (IUpdatable)

        public void Tick(float dt)
        {
            ReadInput();
            TickSpells();
        }

        public void FixedTick(float dt)
        {
            if (!_canMove) return;
            HandleMovement();
        }
        
        #endregion
        
        #region Input Handling

        private void ReadInput()
        {
            if (!_isAlive)
                return;
            
            _input = Helpers.Input.MoveDirection.normalized;

            if (_input.sqrMagnitude > 0.01f)
                _facingDirection = _input;
        }
        
        //Delegate Wrappers
        private void HandleSlot0Press() => HandleSlotPress(0);
        private void HandleSlot1Press() => HandleSlotPress(1);
        private void HandleSlot2Press() => HandleSlotPress(2);
        
        private void HandleSlot0Release() => HandleSlotRelease(0);
        private void HandleSlot1Release() => HandleSlotRelease(1);
        private void HandleSlot2Release() => HandleSlotRelease(2);

        private void HandleSlotPress(int slotIndex)
        {
            if (!_isAlive)
                return;
            
            ISpellSlot spell  = _spellSlots[slotIndex];
            if (spell == null || Time.deltaTime == 0)
                return;

            if (spell is IHoldAbility hold)
            {
                if (_heldHoldSlots.Count > 0)
                    if (_heldHoldSlots[^1] != slotIndex && _spellSlots[_heldHoldSlots[^1]] is IHoldAbility activeHold)
                        activeHold.StopHold(this);
                
                _heldHoldSlots.Remove(slotIndex);
                _heldHoldSlots.Add(slotIndex);
                hold.StartHold(this);
            }
            else if (spell is IAbility ability)
            {
                _heldAutoSlots.Add(slotIndex);
                
                if (_spellSlots[slotIndex] is SpellInstance instance && instance.CooldownRemaining <= 0f)
                {
                    ability.Activate(this);
                }
            }
        }

        private void HandleSlotRelease(int slotIndex)
        {
            if (!_isAlive)
                return;
    
            ISpellSlot spell = _spellSlots[slotIndex];
            if (spell == null)
                return;

            if (spell is IHoldAbility hold)
            {
                bool wasActive = _heldHoldSlots.Count > 0 && _heldHoldSlots[^1] == slotIndex;

                hold.StopHold(this);
                _heldHoldSlots.Remove(slotIndex);

                if (wasActive && _heldHoldSlots.Count > 0)
                {
                    if (_spellSlots[_heldHoldSlots[^1]] is IHoldAbility resumeHold)
                        resumeHold.StartHold(this);
                }
            }
            else if (spell is IAbility)
            {
                _heldAutoSlots.Remove(slotIndex); 
            }
        }
        
        public void ClearHeldInputs()
        {
            foreach (var slotIndex in _heldHoldSlots)
            {
                if (_spellSlots[slotIndex] is IHoldAbility hold)
                    hold.StopHold(this);
            }
    
            _heldHoldSlots.Clear();
            _heldAutoSlots.Clear();
        }
        
        #endregion

        #region Handle Movement & Physics

        private void HandleMovement()
        {
            // Input XY maps to world XZ — Y axis is reserved for gravity/height
            Vector3 targetVelocity = new Vector3(_input.x * _playerStats.BaseSpeed, 0f, _input.y * (_playerStats.BaseSpeed * _playerStats.VerticalSpeedMultiplier));

            bool isMoving = _input.sqrMagnitude > 0.01f;
            
            _velocity = targetVelocity;
            _rb.velocity = _velocity;

            if (isMoving)
            {
                UpdateSpriteFlip();

                if (_runAudioHandle == null || !_runAudioHandle.IsValid)
                {
                    EventBus.Publish(new AudioPlayTrackedRequest
                    {
                        Event = Helpers.PlayerAudio.Footsteps,
                        WorldPosition = transform.position,
                        OnHandleReady = handle => _runAudioHandle = handle
                    });
                }
            }
            else
            {
                if (_runAudioHandle != null && _runAudioHandle.IsValid)
                {
                    EventBus.Publish(new AudioStopRequest
                    {
                        Handle = _runAudioHandle,
                        FadeOut =  true
                    });
                    
                    _runAudioHandle = null;
                }
            }

            if (_input.sqrMagnitude > 0.01f)
                UpdateSpriteFlip();
        }

        private void UpdateSpriteFlip()
        {
            if (_spriteTransform == null) 
                return;

            //COMMENTED OUT FOR ANIMATIONS
            
            // var size = _spriteTransform.localScale.y;
            // _spriteTransform.localScale = new Vector3(
            //     _facingDirection.x < 0f ? size : -size, size, size);
        }
        
        public void SetCanMove(bool canMove) => _canMove = canMove;
        
        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            _rb.velocity = velocity;
        }

        public void TeleportTo(PlayerTeleportRequestEvent evt)
        {
            _velocity = Vector3.zero;
            _rb.velocity = Vector3.zero;
            
            var previousInterpolation = _rb.interpolation;
            _rb.interpolation = RigidbodyInterpolation.None;
            
            _rb.position = evt.Position;
            
            _rb.interpolation = previousInterpolation;
        }
        
        #endregion
        
        #region Spell & Combat State
        
        private void TickSpells()
        {
            foreach (var spell in _spellSlots)
                spell?.Tick(Time.deltaTime);

            if (_heldHoldSlots.Count > 0)
            {
                if (_spellSlots[_heldHoldSlots[^1]] is IHoldAbility hold)
                {
                    hold.HoldTick(Time.deltaTime, this);
                }
            }
            
            foreach (var slotIndex in _heldAutoSlots)
            {
                if (_spellSlots[slotIndex] is IAbility ability && _spellSlots[slotIndex] is SpellInstance instance)
                {
                    if (instance.CooldownRemaining <= 0f)
                    {
                        ability.Activate(this);
                    }
                }
            }
        }

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            if ((int)evt.Slot < _spellSlots.Length)
                _spellSlots[(int)evt.Slot] = evt.Instance as SpellInstance;
        }
        
        //Protect against IndexOutOfRange
        public SpellInstance GetSlot(int index)
        {
            if (index < 0 || index >= _spellSlots.Length)
                return null;
            
            return _spellSlots[index];
        }

        /// <summary>
        /// Force-stops the active shield any fully deplete energy.
        /// Called by world hazards. Does not resume any suspended hold spells - intentional.
        /// </summary>
        public void ForceDestroyActiveShield()
        {
            if (_heldHoldSlots.Count == 0)
                return;

            if (_spellSlots[_heldHoldSlots[^1]] is IHoldAbility hold)
            {
                (_spellSlots[_heldHoldSlots[^1]] as HoldSpellInstance)?.Energy.ForceDeplete();
                hold.StopHold(this);
                _heldHoldSlots.Remove(_heldHoldSlots[^1]);
            }
        }

        public void DamageShield()
        {
            (_spellSlots[_heldHoldSlots[^1]] as HoldSpellInstance)?.Energy.DrainOnHit();
        }
        
        #endregion
    }
}