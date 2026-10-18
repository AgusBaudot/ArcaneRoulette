using Foundation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace UI
{
    public sealed class ShopItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private GameObject _soldOutOverlay;

        [SerializeField] private AudioEventSO _failSound;
        [SerializeField] private AudioEventSO _successSound;

        private ShopUI _parent;
        private int _index;
        private int _cost;
        private bool _isRune;
        private bool _isSold;

        private RuneDefinitionSO _cachedRune;
        // private ArtifactDefinitionSO _cachedArtifact; // Uncomment once artifacts are implemented

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

            transform.DOKill();
            transform.DOScale(1f, 0.1f).SetUpdate(true);

            EventBus.Publish(new AudioPlayRequest { Event = _successSound });
        }

        public void RejectPurchase()
        {
            transform.DOShakePosition(0.3f, 10f, 20, 90f, false, true).SetUpdate(true);
            EventBus.Publish(new AudioPlayRequest { Event = _failSound });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isSold) return;

            if (_isRune && _cachedRune != null && _icon != null) 
            {
                _icon.sprite = _cachedRune.Highlighted;
            }

            transform.DOKill();
            transform.DOScale(1.25f, 0.2f).SetUpdate(true);

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
            }

            transform.DOKill();
            transform.DOScale(1f, 0.2f).SetUpdate(true);
            
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