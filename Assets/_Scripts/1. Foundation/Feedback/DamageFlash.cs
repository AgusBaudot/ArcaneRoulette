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
        
        /// <summary>
        /// Flash using the component's serialized duration.
        /// </summary>
        public void Flash() => Flash(_duration);

        /// <summary>
        /// Flash with an explicit override duration.
        /// </summary>
        /// <param name="duration"></param>
        public void Flash(float duration)
        {
            StopAllCoroutines();
            StartCoroutine(DoFlash(duration));
        }

        private IEnumerator DoFlash(float duration)
        {
            foreach (var r in _renderers)
                r.color = _flashColor;
            
            yield return new WaitForSecondsRealtime(duration); // real time - survives hitstop
            
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].color = _originalColors[i];
        }
    }
}
