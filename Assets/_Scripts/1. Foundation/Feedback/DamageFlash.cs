using System.Collections;
using UnityEngine;

namespace Foundation
{
    public class DamageFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _renderers;
        [SerializeField] private float _duration = 0.07f;
        
        [Header("Shader Integration")]
        [Tooltip("The reference name of the float property in your Shader Graph")]
        [SerializeField] private string _flashProperty = "_FlashAmount";

        public void Flash() => Flash(_duration);

        /// <summary>
        /// Flash with an explicit override duration.
        /// </summary>
        /// <param name="duration"></param>
        public void Flash(float duration)
        {
            if (!isActiveAndEnabled)
                return;
            
            StopAllCoroutines();
            StartCoroutine(DoFlash(duration));
        }

        private IEnumerator DoFlash(float duration)
        {
            SetFlashAmount(1f);

            yield return new WaitForSecondsRealtime(duration);
            
            SetFlashAmount(0f);
        }

        public void OnDisable()
        {
            StopAllCoroutines();
            SetFlashAmount(0f);
        }

        private void SetFlashAmount(float amount)
        {
            foreach (var r in _renderers)
            {
                if (r != null && r.material != null)
                {
                    r.material.SetFloat(_flashProperty, amount);
                }
            }
        }
    }
}