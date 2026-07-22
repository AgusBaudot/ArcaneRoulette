using UnityEngine;
using UnityEngine.UI;
using Foundation;

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
        
        private ISpellSlot _slotData; 
        
        private Color _iconNormalColor = Color.white;
        private Color _fillNormalColor = Color.white;

        private void Awake()
        {
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);

            if (_inputIcon != null) _iconNormalColor = _inputIcon.color;
            if (_fillImage != null) _fillNormalColor = _fillImage.color;

            SetSliderNull();
        }

        private void Start()
        {
            var runState = GameStateManager.RunState;
            if (runState != null)
            {
                _slotData = runState.GetSlot((SlotIndex)_slotIndex);
                if (_slotData != null)
                {
                    RefreshAbilityIcon();
                    SetIconTint(_slotData.IsReady);
                }
            }
        }

        private void OnDestroy() => EventBus.Unsubscribe<SpellEquippedEvent>(OnSpellEquipped);
        private void OnEnable() => UpdateManager.Instance.Register(this);
        private void OnDisable() => UpdateManager.Instance?.Unregister(this);

        public void Tick(float dt)
        {
            if (_slotData == null)
            {
                SetSliderNull();
                return;
            }

            if (_fillImage != null)
                _fillImage.fillAmount = _slotData.DisplayProgress;
                
            SetIconTint(_slotData.IsReady);
        }

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            if ((int)evt.Slot != _slotIndex) return;

            _slotData = evt.Instance;

            RefreshAbilityIcon();
            SetIconTint(_slotData != null && _slotData.IsReady);
        }

        private void SetIconTint(bool isReady)
        {
            var iconTint = isReady ? _iconNormalColor : _grayedColor;

            if (_inputIcon != null) _inputIcon.color = iconTint;
            if (_abilityIcon != null) _abilityIcon.color = iconTint;
            if (_fillImage != null) _fillImage.color = isReady ? _fillNormalColor : _grayedColor;
        }

        private void RefreshAbilityIcon()
        {
            if (_abilityIcon == null) return;

            Sprite icon = _slotData?.Icon; 
            _abilityIcon.sprite = icon;
            _abilityIcon.enabled = icon != null;
        }

        private void SetSliderNull()
        {
            if (_fillImage != null)
            {
                _fillImage.fillAmount = 0f;
                _fillImage.color = _grayedColor;
            }
            if (_inputIcon != null) _inputIcon.color = _grayedColor;
            
            if (_abilityIcon != null)
            {
                _abilityIcon.sprite = null;
                _abilityIcon.enabled = false;
                _abilityIcon.color = _grayedColor;
            }
        }
    }
}