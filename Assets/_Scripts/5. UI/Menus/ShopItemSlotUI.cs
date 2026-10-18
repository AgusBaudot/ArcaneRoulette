using Foundation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class ShopItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _glowImage;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private GameObject _soldOutOverlay;

        [SerializeField] private AudioEventSO _failSound;
        [SerializeField] private AudioEventSO _successSound;

        [Header("Hover Animation")]
        [SerializeField] private float _hoverDuration = 0.2f;
        [SerializeField] private float _scaleMultiplier = 1.1f;
        [SerializeField] private float _hoverOffsetY = 10f;

        private RectTransform _rectTransform;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;

        private ShopUI _parent;
        private int _index;
        private int _cost;
        private bool _isRune;
        private bool _isSold;

        private RuneDefinitionSO _cachedRune;
        
        // private ArtifactDefinitionSO _cachedArtifact; // Uncomment once artifacts are implemented

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalPosition = _rectTransform.anchoredPosition;
            _originalScale = _rectTransform.localScale;
        }

        public void SetupRune(RuneDefinitionSO rune, bool isSold, int cost, int index, ShopUI parent)
        {
            _cachedRune = rune;
            _isRune = true;

            if (_icon != null && rune != null)
            {
                _icon.sprite = rune.Icon;
                _icon.preserveAspect = true;
                _icon.rectTransform.localScale = Vector3.one * rune.UIIconScale;
            }
            
            if (_glowImage != null)
            {
                _glowImage.sprite = rune != null ? rune.BackgroundGlow : null;
                _glowImage.enabled = false;
            }

            InternalSetup(isSold, cost, index, parent);
        }

        // Uncomment once artifacts are implemented
        // public void SetupArtifact(ArtifactDefinitionSO artifact, bool isSold, int cost, int index, ShopUI parent)
        // {
        //     _cachedArtifact = artifact;
        //     _isRune = false;
        //     InternalSetup(isSold, cost, index, parent);
        // }
        // Finish uncomment once artifacts are implemented

        private void InternalSetup(bool isSold, int cost, int index, ShopUI parent)
        {
            _isSold = isSold;
            _cost = cost;
            _index = index;
            _parent = parent;

            _priceText.text = cost.ToString();

            if (isSold) MarkSold();
            else _soldOutOverlay.SetActive(false);
        }

        public void MarkSold()
        {
            _isSold = true;
            _soldOutOverlay.SetActive(true);
            _priceText.rectTransform.parent.gameObject.SetActive(false);
            _glowImage.enabled = false;

            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale, 0.1f).SetUpdate(true);

            EventBus.Publish(new AudioPlayRequest { Event = _successSound });
        }

        public void RejectPurchase()
        {
            _rectTransform.DOShakePosition(0.3f, 10f, 20, 90f, false, true).SetUpdate(true);
            EventBus.Publish(new AudioPlayRequest { Event = _failSound });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isSold) return;

            if (_isRune && _cachedRune != null && _icon != null) 
            {
                _icon.sprite = _cachedRune.Highlighted;
                _glowImage.enabled = true;
            }

            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale * _scaleMultiplier, _hoverDuration).SetUpdate(true);
            _rectTransform.DOAnchorPos(_originalPosition + new Vector2(0f, _hoverOffsetY), _hoverDuration).SetUpdate(true);

            if (GameStateManager.RunState.Currency < _cost)
            {
                _priceText.color = Color.red;
            }

            if (_isRune) TooltipSystem.Instance?.Show(_cachedRune, GetComponent<RectTransform>());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isSold) return;

            if (_isRune && _cachedRune != null && _icon != null) 
            {
                _icon.sprite = _cachedRune.Icon;
                _glowImage.enabled = false;
            }

            _rectTransform.DOKill();
            _rectTransform.DOScale(_originalScale, _hoverDuration).SetUpdate(true);
            _rectTransform.DOAnchorPos(_originalPosition, _hoverDuration).SetUpdate(true);
            
            _priceText.color = Color.white;

            TooltipSystem.Instance?.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isSold || eventData.button != PointerEventData.InputButton.Left) return;

            _parent.AttemptPurchase(_index, _cost, _isRune, this);
        }
    }
}