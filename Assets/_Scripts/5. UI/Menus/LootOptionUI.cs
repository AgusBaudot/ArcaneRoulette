using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Foundation;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public sealed class LootOptionUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Elements")]
        [SerializeField] private Image _runeIcon;
        [SerializeField] private TextMeshProUGUI _runeNameText;
        [SerializeField] private TextMeshProUGUI _runeDescriptionText;

        [Header("Selection Feedback")]
        [SerializeField] private GameObject _selectedHighlight;

        public RuneDefinitionSO Rune { get; private set; }

        private Button _button;
        
        // ── Callbacks ────────────────────────────────────────────────────────
        private Action _onClicked;
        private Action _onHovered;
        private Action _onUnhovered;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(false);
        }

        public void Init(RuneDefinitionSO rune, Action onClicked, Action onHovered = null, Action onUnhovered = null)
        {
            Rune = rune;
            
            _onClicked = onClicked;
            _onHovered = onHovered;
            _onUnhovered = onUnhovered;

            if (rune == null)
                return;

            if (_runeIcon != null)
            {
                _runeIcon.sprite = rune.Icon;
                _runeIcon.preserveAspect = true;
                _runeIcon.rectTransform.localScale = Vector3.one * rune.UIIconScale;
            }

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

        // ── Input Routing ────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
                _onClicked?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            
            if (Rune != null && _runeIcon != null) _runeIcon.sprite = Rune.Highlighted;

            transform.DOKill();
            transform.DOScale(1.65f, 0.2f).SetUpdate(true);

            _onHovered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            
            if (Rune != null && _runeIcon != null) _runeIcon.sprite = Rune.Icon;

            transform.DOKill();
            transform.DOScale(1.5f, 0.2f).SetUpdate(true);

            _onUnhovered?.Invoke();
        }
    }
}