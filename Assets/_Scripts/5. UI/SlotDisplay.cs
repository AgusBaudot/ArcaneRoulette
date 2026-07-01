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
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _grayedColor = Color.gray;
        private SpellInstance _instance;
        private Color _iconNormalColor = Color.white;
        private Color _fillNormalColor = Color.white;

        private void Awake()
        {
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);

            // Slider starts full — no spell equipped yet means nothing on cooldown
            // record normal icon color and ensure icon is grayed out until a spell is equipped
            if (_inputIcon != null)
                _iconNormalColor = _inputIcon.color;
            if (_fillImage != null)
                _fillNormalColor = _fillImage.color;

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
                    SetIconTint(IsSpellReady());
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
            // fillAmount maps directly — no math needed
            if (_fillImage != null)
                _fillImage.fillAmount = _instance.DisplayProgress;
            SetIconTint(IsSpellReady());
        }

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            // Each SlotDisplay only cares about its own slot index
            if ((int)evt.Slot != _slotIndex) return;

            _instance = evt.Instance as SpellInstance;

            // Update the ability icon to reflect the current cast rune automatically.
            RefreshAbilityIcon();
            SetIconTint(IsSpellReady());
        }

        private bool IsSpellReady()
        {
            if (_instance == null)
                return false;

            if (_instance is HoldSpellInstance hold)
                return !hold.Energy.IsBroken && hold.Energy.Current > 0f;

            return _instance.DisplayProgress >= 1f;
        }

        private void SetIconTint(bool isReady)
        {
            var iconTint = isReady ? _iconNormalColor : _grayedColor;

            if (_inputIcon != null)
                _inputIcon.color = iconTint;

            if (_abilityIcon != null)
                _abilityIcon.color = iconTint;

            if (_fillImage != null)
            {
                var fillTint = isReady ? _fillNormalColor : _grayedColor;
                _fillImage.color = fillTint;
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
            if (_fillImage != null)
                _fillImage.fillAmount = 0f;
            if (_inputIcon != null)
                _inputIcon.color = _grayedColor;
            if (_abilityIcon != null)
            {
                _abilityIcon.sprite = null;
                _abilityIcon.enabled = false;
                _abilityIcon.color = _grayedColor;
            }
            if (_fillImage != null)
                _fillImage.color = _grayedColor;
        }
    }
}