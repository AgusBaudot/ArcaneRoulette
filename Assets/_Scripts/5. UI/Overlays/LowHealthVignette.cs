using UnityEngine;
using UnityEngine.UI;
using Foundation;

namespace UI
{
    public class LowHealthVignette : MonoBehaviour
    {
        [SerializeField] private Image _lowHealthImage;
        [SerializeField] private string _cheatCode = "LOWHP";
        [SerializeField] private float _cheatCodeResetDelay = 4f;

        private Color _imageColor;
        private string _cheatBuffer = string.Empty;
        private float _bufferTimer;
        private bool _cheatActive;

        private void Awake()
        {
            if (_lowHealthImage == null)
            {
                Debug.LogError("LowHealthVignette: Low health image reference is missing.");
                enabled = false;
                return;
            }

            _imageColor = _lowHealthImage.color;
            _imageColor.a = 0f;
            _lowHealthImage.color = _imageColor;
        }

        private void OnEnable()
        {
            GameStateManager.RunState.OnHpChanged += UpdateLowHealthImage;
            UpdateLowHealthImage(GameStateManager.RunState.CurrentHp, GameStateManager.RunState.MaxHp);
        }

        private void OnDisable()
        {
            GameStateManager.RunState.OnHpChanged -= UpdateLowHealthImage;
            _cheatActive = false;

            if (_lowHealthImage != null)
            {
                _imageColor.a = 0f;
                _lowHealthImage.color = _imageColor;
            }
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(_cheatCode))
                return;

            if (!string.IsNullOrEmpty(Input.inputString))
            {
                foreach (char c in Input.inputString)
                {
                    if (char.IsControl(c))
                        continue;

                    _cheatBuffer += char.ToUpperInvariant(c);
                    if (_cheatBuffer.Length > _cheatCode.Length)
                    {
                        _cheatBuffer = _cheatBuffer.Substring(_cheatBuffer.Length - _cheatCode.Length);
                    }
                }

                _bufferTimer = _cheatCodeResetDelay;

                if (_cheatBuffer == _cheatCode.ToUpperInvariant())
                {
                    TriggerCheat();
                    _cheatBuffer = string.Empty;
                }
            }

            if (_bufferTimer > 0f)
            {
                _bufferTimer -= Time.unscaledDeltaTime;
                if (_bufferTimer <= 0f)
                {
                    _cheatBuffer = string.Empty;
                }
            }
        }

        private void TriggerCheat()
        {
            _cheatActive = !_cheatActive;
            Debug.Log($"LowHealthVignette cheat {(_cheatActive ? "activated" : "deactivated")}.");
            UpdateLowHealthImage(GameStateManager.RunState.CurrentHp, GameStateManager.RunState.MaxHp);
        }

        private void UpdateLowHealthImage(float currentHp, float maxHp)
        {
            if (_cheatActive)
            {
                _imageColor.a = 1f;
            }
            else
            {
                float healthPercent = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
                _imageColor.a = 1f - healthPercent;
            }

            _lowHealthImage.color = _imageColor;
        }
    }
}
