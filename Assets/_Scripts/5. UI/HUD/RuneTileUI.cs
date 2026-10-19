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
    public sealed class RuneTileUI : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hierarchy")]
        [Tooltip("The child RectTransform that holds the visuals. Do NOT assign the root GameObject.")]
        [SerializeField] private RectTransform _visualRoot;

        [Header("UI Elements")]
        [SerializeField] private Image _icon;
        [SerializeField] private Image _glowImage;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _highlight;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverDuration = 0.2f;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _hoverOffsetY = 10f;

        private Button _button;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;
        private Action<PointerEventData.InputButton> _onClick;
        private RuneDefinitionSO _currentRune;

        private void Awake()
        {
            _button = GetComponent<Button>();
            
            if (_visualRoot != null)
            {
                _originalPosition = _visualRoot.anchoredPosition;
                _originalScale = _visualRoot.localScale;
            }
        }

        public void Init(Action<PointerEventData.InputButton> onClick)
        {
            _onClick = onClick;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && !_button.interactable) return;
            
            _onClick?.Invoke(eventData.button);
            TooltipSystem.Instance?.Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_currentRune == null) return;

            if (_icon != null)
            {
                _icon.sprite = _currentRune.Highlighted;
                _glowImage.gameObject.SetActive(true);
                _glowImage.enabled = true;
            }
            
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale * _scaleMultiplier, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _hoverDuration).SetUpdate(true);
            }
            
            TooltipSystem.Instance?.Show(_currentRune, GetComponent<RectTransform>());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_currentRune != null && _icon != null)
            {
                _icon.sprite = _currentRune.Icon;
                _glowImage.enabled = false;
            }
            
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition, _hoverDuration).SetUpdate(true);
            }
            
            TooltipSystem.Instance?.Hide();
        }

        public void Refresh(RuneDefinitionSO rune, bool highlighted)
        {
            if (_currentRune != null && rune == null) TooltipSystem.Instance?.Hide();

            _currentRune = rune;
            bool hasRune = rune != null;

            if (_icon != null)
            {
                _icon.sprite = hasRune ? rune.Icon : null;
                _icon.enabled = hasRune;
                
                if (hasRune)
                {
                    _icon.preserveAspect = true;
                    _icon.rectTransform.localScale = Vector3.one * rune.UIIconScale;
                }
            }

            if (_glowImage != null)
            {
                _glowImage.sprite = hasRune ? rune.BackgroundGlow : null;
                _glowImage.enabled = false;
            }

            if (_countText != null) _countText.enabled = false;

            if (_highlight != null) _highlight.SetActive(highlighted);
            
            if (_button != null) _button.interactable = true;
        }
    }
}