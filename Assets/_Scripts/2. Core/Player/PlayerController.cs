using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(PlayerEnergy))]
    public class PlayerController : MonoBehaviour, IUpdatable, IFixedUpdatable
    {
        #region Properties

        public PlayerStats Stats => _playerStats;
        public Rigidbody Rigidbody => _rb;
        public PlayerHealth Health => _health;
        public PlayerEnergy Energy => _energy;
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
        public int UpdatePriority => Foundation.UpdatePriority.Player;
        public int FixedUpdatePriority => Foundation.UpdatePriority.Player;

        //Last intentional input direction - used by DashAbilityRune for dash direction.
        //Falls back to facing direction when stick/WASD is neutral.
        public Vector2 LastInputDirection => _input.sqrMagnitude > 0.01f ? _input : _facingDirection;
        #endregion
        
        #region Variables & State
        
        [Header("Dependencies")]
        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private Transform _spriteTransform;
        [SerializeField] private GameObject _hurtBox;

        //Populated by DebugSpellSeeder in Phase 1, by AttunementSystem in Phase 2.
        private readonly SpellInstance[] _spellSlots = new SpellInstance[3];

        private readonly List<int> _heldHoldSlots = new(); //Insertion order = press order

        private Rigidbody _rb;
        private PlayerHealth _health;
        private PlayerEnergy _energy;

        private Vector2 _input;
        private Vector3 _velocity;
        private Vector2 _facingDirection = Vector2.right;
        private bool _canMove = true;
        private PlayerInputActions _inputActions;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                              | RigidbodyConstraints.FreezeRotation;
            
            _health = GetComponent<PlayerHealth>();
            _energy = GetComponent<PlayerEnergy>();
            _health.Initialize(_playerStats);
            _energy.Initialize(_playerStats);
            
            GetComponentInChildren<PlayerHurtBox>()?.Initialize(_health);
            
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SpellEquippedEvent>(OnSpellEquipped);
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
        }

        private void OnDisable()
        {
            //Update Manager
            UpdateManager.Instance?.Unregister((IUpdatable)this);
            UpdateManager.Instance?.Unregister((IFixedUpdatable)this);
            
            //Input
            //Input
            Helpers.Input.OnSlot0Started -= HandleSlot0Press;
            Helpers.Input.OnSlot1Started -= HandleSlot1Press;
            Helpers.Input.OnSlot2Started -= HandleSlot2Press;
            
            Helpers.Input.OnSlot0Canceled -= HandleSlot0Release;
            Helpers.Input.OnSlot1Canceled -= HandleSlot1Release;
            Helpers.Input.OnSlot2Canceled -= HandleSlot2Release;
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
                ability.Activate(this);
            }
        }

        private void HandleSlotRelease(int slotIndex)
        {
            ISpellSlot spell = _spellSlots[slotIndex];
            if (spell == null || Time.deltaTime == 0)
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
        }
        
        #endregion

        // private void HandleSlotInput(int slotIndex, KeyCode key, ISpellSlot spell)
        // {
        //     //If time is 0, game is paused. Ignore input.
        //     if (spell == null || Time.deltaTime == 0) 
        //         return;
        //
        //     if (spell is IHoldAbility hold)
        //     {
        //         if (Input.GetKeyDown(key))
        //         {
        //             //Suspend currently active hold (last in list) if different slot
        //             if (_heldHoldSlots.Count > 0)
        //             {
        //                 int activeSlot = _heldHoldSlots[^1];
        //                 if (activeSlot != slotIndex && _spellSlots[activeSlot] is IHoldAbility activeHold)
        //                     activeHold.StopHold(this);
        //             }
        //
        //             _heldHoldSlots.Remove(slotIndex); //Safety - shouldn't be present
        //             _heldHoldSlots.Add(slotIndex);
        //             hold.StartHold(this);
        //         }
        //         
        //         //Only tick the last-pressed (active) hold
        //         if (Input.GetKey(key) && _heldHoldSlots.Count > 0 && _heldHoldSlots[^1] == slotIndex)
        //         {
        //             hold.HoldTick(Time.deltaTime, this);
        //         }
        //
        //         if (Input.GetKeyUp(key))
        //         {
        //             bool wasActive = _heldHoldSlots.Count > 0 && _heldHoldSlots[^1] == slotIndex;
        //
        //             hold.StopHold(this);
        //             _heldHoldSlots.Remove(slotIndex);
        //             
        //             //If the released slot was active and another is still held, resume it
        //             if (wasActive && _heldHoldSlots.Count > 0)
        //             {
        //                 if (_spellSlots[_heldHoldSlots[^1]] is IHoldAbility resumeHold)
        //                     resumeHold.StartHold(this);
        //             }
        //         }
        //     }
        //     else if (spell is IAbility ability)
        //     {
        //         if (Input.GetKeyDown(key))
        //             ability.Activate(this);
        //     }
        // }

        #region Handle Movement & Physics

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
                UpdateSpriteFlip();
        }

        private void UpdateSpriteFlip()
        {
            if (_spriteTransform == null) return;
            _spriteTransform.localScale = new Vector3(
                _facingDirection.x < 0f ? -0.75f : 0.75f, 0.75f, 0.75f);
        }
        
        public void SetCanMove(bool canMove) => _canMove = canMove;
        
        public void SetVelocity(Vector3 velocity) => _rb.velocity = velocity;
        
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
                hold.StopHold(this);
                _heldHoldSlots.Remove(_heldHoldSlots[^1]);
            }

            _energy.ForceDeplete();
        }
        
        #endregion
    }
}