using System;
using DG.Tweening;
using Foundation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// Sits on a Filter Tab object. Handles DOTween hover/click feedback 
    /// and active state elevation, bypassing Unity's Button drag threshold.
    /// </summary>
    public sealed class RuneFilterTab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [Header("Hierarchy")]
        [Tooltip("The child RectTransform that holds the visuals. Do NOT assign the root GameObject.")]
        [SerializeField] private RectTransform _visualRoot;

        [Header("Configuration")]
        [SerializeField] private RuneFilter _filterType;

        [Header("Animation")]
        [SerializeField] private float _hoverDuration = 0.2f;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _hoverOffsetY = 10f;

        private Action<RuneFilterTab> _onTabClicked;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;
        private bool _isActive;

        public RuneFilter FilterType => _filterType;

        private void Awake()
        {
            if (_visualRoot != null)
            {
                _originalPosition = _visualRoot.anchoredPosition;
                _originalScale = _visualRoot.localScale;
            }
        }

        public void Init(Action<RuneFilterTab> onTabClicked)
        {
            _onTabClicked = onTabClicked;
        }

        /// <summary>
        /// Visually elevates/scales the tab if active, or returns it to baseline if inactive.
        /// </summary>
        public void SetActiveState(bool isActive)
        {
            _isActive = isActive;
            
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                
                if (_isActive)
                {
                    _visualRoot.DOScale(_originalScale * _scaleMultiplier, _hoverDuration).SetUpdate(true);
                    _visualRoot.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _hoverDuration).SetUpdate(true);
                }
                else
                {
                    _visualRoot.DOScale(_originalScale, _hoverDuration).SetUpdate(true);
                    _visualRoot.DOAnchorPos(_originalPosition, _hoverDuration).SetUpdate(true);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isActive || eventData.button != PointerEventData.InputButton.Left) 
                return;

            if (_visualRoot != null)
            {
                _visualRoot.DOKill(true);
                _visualRoot.DOPunchScale(Vector3.one * -0.1f, 0.15f, 1).SetUpdate(true);
            }

            _onTabClicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isActive) 
                return;

            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale * _scaleMultiplier, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _hoverDuration).SetUpdate(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isActive) 
                return;

            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.DOScale(_originalScale, _hoverDuration).SetUpdate(true);
                _visualRoot.DOAnchorPos(_originalPosition, _hoverDuration).SetUpdate(true);
            }
        }

        private void OnDisable()
        {
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                
                _visualRoot.localScale = _isActive ? _originalScale * _scaleMultiplier : _originalScale;
                _visualRoot.anchoredPosition = _isActive ? _originalPosition + new Vector2(0f, _hoverOffsetY) : _originalPosition;
            }
        }
    }
}