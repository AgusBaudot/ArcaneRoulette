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
        [SerializeField] private Image _abilityIcon;         // optional — spell icon
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
            if (_abilityIcon != null)
                _iconNormalColor = _abilityIcon.color;

            SetSliderNull();
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
            if (_abilityIcon != null)
            {
                // target is interpolation between grayed and normal based on fill (0..1)
                var target = Color.Lerp(_grayedColor, _iconNormalColor, _cooldownSlider.value);

                // If completely empty, set gray immediately
                if (_cooldownSlider.value <= 0f)
                {
                    _abilityIcon.color = _grayedColor;
                }
                else
                {
                    // Smoothly approach target color
                    float t = Mathf.Clamp01(dt * _iconRecoverSpeed);
                    _abilityIcon.color = Color.Lerp(_abilityIcon.color, target, t);
                }
            }
        }

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            // Each SlotDisplay only cares about its own slot index
            if ((int)evt.Slot != _slotIndex) return;

            _instance = evt.Instance as SpellInstance;

            // Optionally set sprite here if available on the instance (left commented because
            // `SpellInstance` internals vary). Ensure icon color is reset to the normal color
            // so it can be grayed by cooldown logic when needed.
            // if (_abilityIcon != null)
            //     _abilityIcon.sprite = _instance?.IconSprite;

            if (_abilityIcon != null)
            {
                _abilityIcon.color = _iconNormalColor;
            }
        }

        private void SetSliderNull()
        {
            _cooldownSlider.value = 0f;
            if (_abilityIcon != null)
                _abilityIcon.color = _grayedColor;
        }
    }
}