using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Foundation;

namespace UI
{
    /// <summary>
    /// Represents a single selectable "Boon" option in the LootSelectionUI.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LootOptionUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image _runeIcon;
        [SerializeField] private TextMeshProUGUI _runeNameText;
        [SerializeField] private TextMeshProUGUI _runeDescriptionText;

        private Button _button;
        private Action _onSelected;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Init(RuneDefinitionSO rune, Action onSelected)
        {
            _onSelected = onSelected;

            // Update visuals based on the rune data
            if (rune != null)
            {
                _runeIcon.sprite = rune.Icon;
                _runeNameText.text = rune.name; // O el campo de nombre que uses en tu SO
                // _runeDescriptionText.text = rune.Description; 
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;

            // Only trigger on Left Click
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                _onSelected?.Invoke();
            }
        }
    }
}
