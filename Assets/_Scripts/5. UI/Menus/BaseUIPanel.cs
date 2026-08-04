using System;
using DG.Tweening;
using UnityEngine;

public abstract class BaseUIPanel : MonoBehaviour
{
    public event Action OnCloseRequested;
    
    private CanvasGroup _canvasGroup;
    private Tween _fadeTween;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    public virtual void Show()
    {
        _fadeTween?.Kill();
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        _fadeTween = _canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
    }

    public virtual void Hide()
    {
        _fadeTween?.Kill();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _fadeTween = _canvasGroup.DOFade(0f, 0.2f).SetUpdate(true);
    }

    protected void RequestClose()
    {
        OnCloseRequested?.Invoke();
    }
}