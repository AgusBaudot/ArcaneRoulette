using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopBackButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Transform _visualRoot;

        [Header("Click Animation")]
        [SerializeField] private float _clickDuration = 0.3f;
        [SerializeField] private Ease _clickEase = Ease.OutBack;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverDuration = 0.15f;
        [SerializeField] private float _defaultZRotation = -25f;
        [SerializeField] private float _hoverZRotation = -20f;

        private Button _button;
        private bool _isAnimatingClick;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.localRotation = Quaternion.Euler(0f, 0f, _defaultZRotation);
            }
            _isAnimatingClick = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable || _isAnimatingClick || _visualRoot == null) return;

            _visualRoot.DOKill();
            _visualRoot.DOLocalRotate(new Vector3(0f, 0f, _hoverZRotation), _hoverDuration)
                .SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_button.interactable || _isAnimatingClick || _visualRoot == null) return;

            _visualRoot.DOKill();
            _visualRoot.DOLocalRotate(new Vector3(0f, 0f, _defaultZRotation), _hoverDuration)
                .SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_button.interactable || eventData.button != PointerEventData.InputButton.Left || _visualRoot == null) return;

            _isAnimatingClick = true;
            _visualRoot.DOKill();
            
            _visualRoot.DOLocalRotate(Vector3.zero, _clickDuration)
                .SetEase(_clickEase)
                .SetUpdate(true)
                .OnComplete(() => 
                {
                    _visualRoot.localRotation = Quaternion.Euler(0f, 0f, _defaultZRotation);
                    _isAnimatingClick = false;
                });
        }
    }
}