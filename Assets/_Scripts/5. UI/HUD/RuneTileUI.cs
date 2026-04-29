using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Foundation;

namespace UI
{
    /// <summary>
    /// Single rune tile. Purely visual — displays what it's told.
    /// Init once, Refresh whenever state changes. No drag, no drop.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RuneTileUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _highlight;

        private Button _button;
        private Action<PointerEventData.InputButton> _onClick;

        private RuneDefinitionSO _currentRune;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Init(Action<PointerEventData.InputButton> onClick)
        {
            _onClick = onClick;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;
            
            _onClick?.Invoke(eventData.button);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_currentRune == null)
                return;
            
            TooltipSystem.Instance?.Show(_currentRune, GetComponent<RectTransform>());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipSystem.Instance?.Hide();
        }

        /// <summary>
        /// Call whenever the tile's represented rune or state changes.
        /// </summary>
        public void Refresh(RuneDefinitionSO rune, bool highlighted)
        {
            _currentRune = rune;
            
            bool hasRune = rune != null;

            _icon.sprite = hasRune ? rune.Icon : null;
            _icon.enabled = hasRune;
            _icon.preserveAspect = true;

            if (_countText != null)
                _countText.enabled = false;

            _highlight.SetActive(highlighted);

            // Slot is clickable even when empty — clicking empty slot
            // with a pending rune assigns it; without one, does nothing.
            _button.interactable = true;
        }
    }
}