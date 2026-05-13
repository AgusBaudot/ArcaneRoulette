using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Foundation;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public sealed class LootOptionUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image _runeIcon;
        [SerializeField] private TextMeshProUGUI _runeNameText;
        [SerializeField] private TextMeshProUGUI _runeDescriptionText;

        [Header("Selection Feedback")]
        [SerializeField] private GameObject _selectedHighlight;

        public RuneDefinitionSO Rune { get; private set; }

        private Button _button;
        private Action _onClicked;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(false);
        }

        public void Init(RuneDefinitionSO rune, Action onClicked)
        {
            Rune = rune;
            _onClicked = onClicked;

            if (rune == null)
                return;

            if (_runeIcon != null)
                _runeIcon.sprite = rune.Icon;

            if (_runeNameText != null)
                _runeNameText.text = rune.Name;

            if (_runeDescriptionText != null)
                _runeDescriptionText.text = rune.Description;
        }

        public void SetSelected(bool selected)
        {
            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
                _onClicked?.Invoke();
        }
    }
}