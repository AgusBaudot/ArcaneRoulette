using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Image))]
    public sealed class SwirlTransition : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");

        [SerializeField] private Ease _closeEase = Ease.InCubic;
        [SerializeField] private Ease _openEase = Ease.OutCubic;

        private Image _image;
        private Material _material;
        private Tween _tween;
        private float _progress;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _material = new Material(_image.material);
            _image.material = _material;

            if (!_material.HasProperty(ProgressId))
                Debug.LogError("[SwirlTransition] Assigned material doesn't use the UI/SpiralWipe shader.", this);

            SetProgress(0f);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            if (_material != null) Destroy(_material);
        }

        /// <summary>Spiral closes in from the screen edges (0 -> 1).</summary>
        public Tween Close(float duration) => Play(1f, duration, _closeEase, 0f);

        /// <summary>Exact reverse: spiral unwinds back out to the edges (1 -> 0).</summary>
        public Tween Open(float duration, float delay = 0f) => Play(0f, duration, _openEase, delay);

        /// <summary>Instantly fully covered (used at the start of a scene that opens with the swirl).</summary>
        public void SetCovered() => SetProgress(1f);

        private Tween Play(float target, float duration, Ease ease, float delay)
        {
            _tween?.Kill();
            _tween = DOTween.To(() => _progress, SetProgress, target, duration)
                .SetDelay(delay)
                .SetEase(ease)
                .SetUpdate(true)
                .SetTarget(this);
            return _tween;
        }

        private void SetProgress(float value)
        {
            _progress = value;
            _material.SetFloat(ProgressId, value);
            _image.raycastTarget = value > 0f; // blocks clicks while covering, never when idle
        }
    }
}