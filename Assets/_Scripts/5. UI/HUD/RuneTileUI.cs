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
            
            // Hide tooltip immediately on click
            TooltipSystem.Instance?.Hide();
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
            // If rune is being cleared while tooltip might be showing, hide it
            if (_currentRune != null && rune == null)
            {
                TooltipSystem.Instance?.Hide();
            }

            _currentRune = rune;
            
            bool hasRune = rune != null;

            _icon.sprite = hasRune ? rune.Icon : null;
            _icon.enabled = hasRune;
            if (hasRune)
            {
                _icon.preserveAspect = true;
                _icon.rectTransform.localScale = Vector3.one * rune.UIIconScale;
            }

            if (_countText != null)
                _countText.enabled = false;

            _highlight.SetActive(highlighted);

            // Slot is clickable even when empty — clicking empty slot
            // with a pending rune assigns it; without one, does nothing.
            _button.interactable = true;
        }
    }
}