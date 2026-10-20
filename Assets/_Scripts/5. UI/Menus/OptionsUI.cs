using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class OptionsUI : BaseUIPanel
    {
        [Header("Animation")]
        [Tooltip("Assign the actual menu window child object here.")]
        [SerializeField] private RectTransform _visualRoot;
        
        [Tooltip("How long the slide animation takes.")]
        [SerializeField] private float _animationDuration = 0.4f;
        [Tooltip("The strength of the bounce/elastic effect (DOTween OutBack ease).")]
        [SerializeField] private float _overshootAmount = 1.2f;
        [Tooltip("How far right (in Canvas units) the panel starts/ends.")]
        [SerializeField] private float _hiddenOffsetX = 1920f;

        [Header("References")]
        [SerializeField] private Button _closeButton;

        private Vector2 _originalPosition;

        protected override void Awake()
        {
            base.Awake();
            
            if (_closeButton != null)
                _closeButton.onClick.AddListener(RequestClose);

            if (_visualRoot != null)
            {
                _originalPosition = _visualRoot.anchoredPosition;
                _visualRoot.anchoredPosition = new Vector2(_originalPosition.x + _hiddenOffsetX, _originalPosition.y);
            }
        }

        public override void Show()
        {
            base.Show(); 
            
            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                
                _visualRoot.anchoredPosition = new Vector2(_originalPosition.x + _hiddenOffsetX, _originalPosition.y);

                _visualRoot.DOAnchorPos(_originalPosition, _animationDuration)
                    .SetEase(Ease.OutBack, _overshootAmount)
                    .SetUpdate(true);
            }
        }

        public override void Hide()
        {
            base.Hide();

            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                
                _visualRoot.DOAnchorPos(new Vector2(_originalPosition.x + _hiddenOffsetX, _originalPosition.y), _animationDuration)
                    .SetEase(Ease.InBack, _overshootAmount)
                    .SetUpdate(true);
            }
        }
    }
}