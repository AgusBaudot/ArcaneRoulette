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
        [Header("Hierarchy")]
        [Tooltip("The child RectTransform that holds the visuals. Do NOT assign the root GameObject.")]
        [SerializeField] private RectTransform _visualRoot;

        [Header("UI Elements")]
        [SerializeField] private Image _runeIcon;
        [SerializeField] private Image _glowImage;
        [SerializeField] private TextMeshProUGUI _runeNameText;
        [SerializeField] private TextMeshProUGUI _runeDescriptionText;

        [Header("Selection Feedback")]
        [SerializeField] private GameObject _selectedHighlight;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverDuration = 0.2f;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _hoverOffsetY = 10f;

        public RuneDefinitionSO Rune { get; private set; }
        private Button _button;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;
        
        private Action _onClicked;
        private Action _onHovered;
        private Action _onUnhovered;

        private void Awake()
        {
            _button = GetComponent<Button>();
            
            if (_visualRoot != null)
            {
                _originalPosition = _visualRoot.anchoredPosition;
                _originalScale = _visualRoot.localScale;
            }

            if (_selectedHighlight != null)
                _selectedHighlight.SetActive(false);
        }

        public void Init(RuneDefinitionSO rune, Action onClicked, Action onHovered = null, Action onUnhovered = null)
        {
            Rune = rune;
            _onClicked = onClicked;
            _onHovered = onHovered;
            _onUnhovered = onUnhovered;

            if (rune == null) return;

            if (_runeIcon != null)
            {
                _runeIcon.sprite = rune.Icon;
                _runeIcon.preserveAspect = true;
                _runeIcon.rectTransform.localScale = Vector3.one * rune.UIIconScale;
            }

            if (_glowImage != null)
            {
                _glowImage.sprite = rune.BackgroundGlow;
                _glowImage.enabled = false;
            }

            if (_runeNameText != null) _runeNameText.text = rune.Name;
            if (_runeDescriptionText != null) _runeDescriptionText.text = rune.Description;
        }

        public void SetSelected(bool selected)
        {
            if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            if (eventData.button == PointerEventData.InputButton.Left) _onClicked?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            
            if (Rune != null && _runeIcon != null)
            {
                _runeIcon.sprite = Rune.Highlighted;
                if (_glowImage != null) _glowImage.enabled = true;
            }

            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale * _scaleMultiplier, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _hoverDuration).SetUpdate(true);
            }

            _onHovered?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            
            if (Rune != null && _runeIcon != null)
            {
                _runeIcon.sprite = Rune.Icon;
                if (_glowImage != null) _glowImage.enabled = false;
            }

            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition, _hoverDuration).SetUpdate(true);
            }

            _onUnhovered?.Invoke();
        }
    }
}