using System;
using DG.Tweening;
using Foundation;
using UnityEngine;

public abstract class BaseUIPanel : MonoBehaviour
{
    public event Action OnCloseRequested;
    
    [Header("Base Panel Settings")]
    [Tooltip("How long the background takes to fade in/out.")]
    [SerializeField] protected float _fadeDuration = 0.2f;

    [Tooltip("Optional: If assigned, ONLY this group fades. The root stays fully opaque so sliding windows don't turn transparent.")]
    [SerializeField] protected CanvasGroup _backgroundFadeGroup;

    protected CanvasGroup _canvasGroup;
    private Tween _fadeTween;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        
        _canvasGroup.alpha = _backgroundFadeGroup != null ? 1f : 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        if (_backgroundFadeGroup != null)
        {
            _backgroundFadeGroup.alpha = 0f;
        }
    }

    public virtual void Show()
    {
        _fadeTween?.Kill();
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        if (_backgroundFadeGroup != null)
            _fadeTween = _backgroundFadeGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
        else
            _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
    }

    public virtual void Hide()
    {
        _fadeTween?.Kill();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        if (_backgroundFadeGroup != null)
            _fadeTween = _backgroundFadeGroup.DOFade(0f, _fadeDuration).SetUpdate(true);
        else
            _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration).SetUpdate(true);
    }

    protected void RequestClose()
    {
        // EventBus.Publish(new OnSettingsUIClosedEvent());
        OnCloseRequested?.Invoke();
    }
}