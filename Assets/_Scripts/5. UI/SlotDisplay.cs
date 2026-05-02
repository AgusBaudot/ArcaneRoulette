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

        private Slider _cooldownSlider;
        private SpellInstance _instance;

        private void Awake()
        {
            _cooldownSlider = GetComponent<Slider>();
            
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);

            // Slider starts full — no spell equipped yet means nothing on cooldown
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
        }

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            // Each SlotDisplay only cares about its own slot index
            if ((int)evt.Slot != _slotIndex) return;

            _instance = evt.Instance as SpellInstance;

            // if (_abilityIcon != null)
            //     _abilityIcon.sprite = _instance?.;  // null-safe — clears icon on dismantle
        }

        private void SetSliderNull() => _cooldownSlider.value = 0f;
    }
}