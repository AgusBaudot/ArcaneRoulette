using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Foundation;
using World;

namespace UI
{
    public sealed class MinimapBlockUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _playerIndicatorImage;
        
        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.3f;
        
        [Header("Cleared State Colors")]
        [SerializeField] private Color _clearedActiveColor = new(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color _clearedDimmedColor = new(0.3f, 0.3f, 0.3f, 1f);

        private VolatileRunState.RoomMapData _currentData;
        private RoomStyleConfig _currentStyle;

        public void Setup(VolatileRunState.RoomMapData data, RoomStyleConfig style)
        {
            _currentData = data;
            _currentStyle = style;
            UpdateVisuals(data, style);
        }

        public void UpdateVisuals(VolatileRunState.RoomMapData data, RoomStyleConfig style)
        {
            _currentData = data;
            _currentStyle = style;

            if (_iconImage != null && style.Icon != null)
            {
                _iconImage.sprite = style.Icon;
                _iconImage.enabled = true;
            }

            if (data.IsCleared && IsMutableRoom(data.Type))
            {
                _iconImage.DOFade(0f, _fadeDuration).SetUpdate(true);
            }
            else
            {
                _iconImage.DOFade(1f, _fadeDuration).SetUpdate(true);
            }
        }

        public void SetFocus(bool isCurrentRoom)
        {
            Color targetColor = isCurrentRoom ? _currentStyle.ActiveColor : _currentStyle.DimmedColor;
    
            if (_currentData.IsCleared && IsMutableRoom(_currentData.Type))
            {
                targetColor = isCurrentRoom ? _clearedActiveColor : _clearedDimmedColor;
            }

            _backgroundImage.DOColor(targetColor, _fadeDuration).SetUpdate(true);

            if (_playerIndicatorImage != null)
            {
                bool showIndicator = isCurrentRoom && _currentData.IsCleared && IsMutableRoom(_currentData.Type);
                _playerIndicatorImage.enabled = showIndicator;
            }
        }

        private bool IsMutableRoom(RoomType type)
        {
            return type == RoomType.Regular || type == RoomType.Resting || type == RoomType.Artifact;
        }
    }
}