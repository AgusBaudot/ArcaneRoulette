using System;
using DG.Tweening;
using Foundation;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class RoomTransitionFader : MonoBehaviour
    {
        [SerializeField] private float _fadeDuration = 0.35f;
        
        private CanvasGroup _canvasGroup;
        private bool _isTransitioning;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RoomTransitionRequestEvent>(HandleTransitionRequest);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RoomTransitionRequestEvent>(HandleTransitionRequest);
        }

        private void HandleTransitionRequest(RoomTransitionRequestEvent evt)
        {
            if (_isTransitioning)
                return;
            
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1, _fadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
            {
                EventBus.Publish(new RoomTransitionExecuteEvent(evt.SourceIndex, evt.Direction));

                _canvasGroup.DOFade(0f, _fadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                {
                    _canvasGroup.blocksRaycasts = false;
                    _isTransitioning = false;
                });
            });
        }
    }
}