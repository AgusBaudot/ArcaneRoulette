using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopBackButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [Header("Click Animation")]
        [SerializeField] private float _clickDuration = 0.3f;
        [SerializeField] private Ease _clickEase = Ease.OutBack;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverDuration = 0.15f;
        [SerializeField] private float _defaultZRotation = -25f;
        [SerializeField] private float _hoverZRotation = -20f;

        private Button _button;
        private Transform _parentTransform;
        private bool _isAnimatingClick;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _parentTransform = transform.parent;
        }

        private void OnDisable()
        {
            if (_parentTransform != null)
            {
                _parentTransform.DOKill();
                _parentTransform.localRotation = Quaternion.Euler(0f, 0f, _defaultZRotation);
            }
            _isAnimatingClick = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable || _isAnimatingClick) return;

            _parentTransform.DOKill();
            _parentTransform.DOLocalRotate(new Vector3(0f, 0f, _hoverZRotation), _hoverDuration)
                .SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_button.interactable || _isAnimatingClick) return;

            _parentTransform.DOKill();
            _parentTransform.DOLocalRotate(new Vector3(0f, 0f, _defaultZRotation), _hoverDuration)
                .SetUpdate(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable || eventData.button != PointerEventData.InputButton.Left) return;

            _isAnimatingClick = true;
            _parentTransform.DOKill();
            
            _parentTransform.DOLocalRotate(Vector3.zero, _clickDuration)
                .SetEase(_clickEase)
                .SetUpdate(true)
                .OnComplete(() => 
                {
                    _parentTransform.localRotation = Quaternion.Euler(0f, 0f, _defaultZRotation);
                    _isAnimatingClick = false;
                });
        }
    }
}