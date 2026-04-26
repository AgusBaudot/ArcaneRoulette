using System.Collections;
using UnityEngine;

namespace Foundation
{
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _renderers;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _duration = 0.07f;

        private Color[] _originalColors;

        private void Awake()
        {
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _originalColors[i] = _renderers[i].color;
        }

        public void Flash()
        {
            StopAllCoroutines();
            StartCoroutine(DoFlash());
        }

        public void Flash(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(DoFlash());
        }

        private IEnumerator DoFlash(float duration = 0)
        {
            foreach (var r in _renderers) r.color = _flashColor;
            yield return new WaitForSecondsRealtime(duration == 0 ? _duration : duration); // real time - survives hitstop
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].color = _originalColors[i];
        }
    }
}
