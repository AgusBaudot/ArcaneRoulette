using UnityEngine;
using UnityEngine.UI;
using Foundation;

namespace UI
{
    public class LowHealthVignette : MonoBehaviour
    {
        #region Variables & State

        [SerializeField] private Image _vignetteImage;
        [SerializeField, Range(0f, 1f)] private float _criticalHealthThreshold = 0.25f;

        private bool _forceShowVignette;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (GameStateManager.RunState != null)
            {
                GameStateManager.RunState.OnHpChanged += UpdateVignette;
                UpdateVignette(GameStateManager.RunState.CurrentHp, GameStateManager.RunState.MaxHp);
            }
        }

        private void OnDisable()
        {
            if (GameStateManager.RunState != null)
                GameStateManager.RunState.OnHpChanged -= UpdateVignette;
        }

        #endregion

        #region Vignette Update

        private void UpdateVignette(float currentHp, float maxHp)
        {
            if (_vignetteImage == null)
                return;

            if (_forceShowVignette)
            {
                _vignetteImage.enabled = true;
                return;
            }

            float normalizedHealth = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            _vignetteImage.enabled = normalizedHealth <= _criticalHealthThreshold;
        }

        #endregion
    }
}
