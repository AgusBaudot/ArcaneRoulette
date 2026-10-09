using Foundation;
using UnityEngine;
using DG.Tweening;

namespace UI
{
    /// <summary>
    /// Fades out the entire HUD root when the player clears a floor and takes a portal.
    /// Attach this directly to the HUD_Panel root GameObject.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class HUDTransitionFader : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 0.3f;
        
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<FloorClearedEvent>(HandleFloorCleared);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FloorClearedEvent>(HandleFloorCleared);
        }

        private void HandleFloorCleared(FloorClearedEvent _)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(0f, _fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => _canvasGroup.interactable = false);
        }
    }
}