using System;
using UnityEngine;
using UnityEngine.UI;
using Foundation;
using Core;

namespace UI
{
    public sealed class SlotDisplay : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.UI;
        
        [SerializeField] private int _slotIndex;
        [SerializeField] private Image _inputIcon;
        [SerializeField] private Image _abilityIcon;      
        [SerializeField] private Color _grayedColor = Color.gray;
        [SerializeField] private float _iconRecoverSpeed = 5f; // how quickly the icon color approaches the target

        private Slider _cooldownSlider;
        private SpellInstance _instance;
        private Color _iconNormalColor = Color.white;

        private void Awake()
        {
            _cooldownSlider = GetComponent<Slider>();
            
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);

            // Slider starts full — no spell equipped yet means nothing on cooldown
            // record normal icon color and ensure icon is grayed out until a spell is equipped
            if (_inputIcon != null)
                _iconNormalColor = _inputIcon.color;

            SetSliderNull();
        }

        private void Start()
        {
            // If a spell was already equipped before this UI came online,
            // initialize the icon from the current run state immediately.
            var runState = GameStateManager.RunState;
            if (runState != null)
            {
                var spell = runState.GetSlot((SlotIndex)_slotIndex) as SpellInstance;
                if (spell != null)
                {
                    _instance = spell;
                    RefreshAbilityIcon();
                    if (_inputIcon != null)
                        _inputIcon.color = _iconNormalColor;
                    if (_abilityIcon != null)
                        _abilityIcon.color = _iconNormalColor;
                }
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SpellEquippedEvent>(OnSpellEquipped);
        }

        private void OnEnable() => UpdateManager.Instance.Register(this);
        
        private void OnDisable() => UpdateManager.Instance?.Unregister(this);

        public void Tick(float dt)
        {
            if (_instance == null)
            {
                SetSliderNull();
                return;
            }

            // CooldownProgress: 1 = ready, 0 = fully on cooldown
            // Slider.value maps directly — no math needed
            _cooldownSlider.value = _instance.DisplayProgress;

            // Update icon color: immediately gray when completely empty,
            // otherwise smoothly move towards the color corresponding to the fill level.
            var target = Color.Lerp(_grayedColor, _iconNormalColor, _cooldownSlider.value);

            if (_cooldownSlider.value <= 0f)
            {
                // If completely empty, set gray immediately
                if (_inputIcon != null)
                    _inputIcon.color = _grayedColor;
                if (_abilityIcon != null)
                    _abilityIcon.color = _grayedColor;
            }
            else
            {
                // Smoothly approach target color
                float t = Mathf.Clamp01(dt * _iconRecoverSpeed);
                if (_inputIcon != null)
                    _inputIcon.color = Color.Lerp(_inputIcon.color, target, t);
                if (_abilityIcon != null)
                    _abilityIcon.color = Color.Lerp(_abilityIcon.color, target, t);
            }
        }

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            // Each SlotDisplay only cares about its own slot index
            if ((int)evt.Slot != _slotIndex) return;

            _instance = evt.Instance as SpellInstance;

            // Update the ability icon to reflect the current cast rune automatically.
            RefreshAbilityIcon();

            if (_inputIcon != null)
            {
                _inputIcon.color = _iconNormalColor;
            }

            if (_abilityIcon != null)
            {
                _abilityIcon.color = _iconNormalColor;
            }
        }

        private void RefreshAbilityIcon()
        {
            if (_abilityIcon == null)
                return;

            Sprite icon = GetCurrentCastRuneIcon();
            _abilityIcon.sprite = icon;
            _abilityIcon.enabled = icon != null;
        }

        private Sprite GetCurrentCastRuneIcon()
        {
            if (_instance == null)
                return null;

            // Only show the ability rune icon here (shield, dash, etc.).
            // Modifier rune icons are handled elsewhere and should not appear
            // on the ability icon.
            return _instance.Recipe.Ability?.StoneLessIcon;
        }

        private void SetSliderNull()
        {
            _cooldownSlider.value = 0f;
            if (_inputIcon != null)
                _inputIcon.color = _grayedColor;
            if (_abilityIcon != null)
            {
                _abilityIcon.sprite = null;
                _abilityIcon.enabled = false;
                _abilityIcon.color = _grayedColor;
            }
        }
    }
}