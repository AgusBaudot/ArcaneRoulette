using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Foundation;

namespace Core
{
    public class LowHealthVignette : MonoBehaviour
    {
        [Header("Vignette Source")]
        [SerializeField] private Volume _volume;

        [Header("Low Health Trigger")]
        [Range(0f, 1f)] [SerializeField] private float _criticalHealthThreshold = 0.25f;

        [Header("Flash Settings")]
        [SerializeField] private Color _criticalColor = Color.red;
        [SerializeField] private float _flashFrequency = 2f;
        [SerializeField] private float _minIntensity = 0.35f;
        [SerializeField] private float _maxIntensity = 0.8f;

        [Header("Edge Appearance")]
        [Range(0f, 1f)] [SerializeField] private float _baseSmoothness = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _criticalSmoothness = 0.15f;

        private Vignette _vignette;
        private float _flashTimer;
        private bool _isCritical;
        private float _baseIntensity;
        private Color _baseColor;
        private bool _subscribedToRunState;

        private void Reset()
        {
            _volume = GetComponent<Volume>();
        }

        private void Awake()
        {
            if (_volume == null)
                _volume = GetComponent<Volume>();

            if (_volume == null)
                _volume = FindObjectOfType<Volume>();

            if (!TryGetVignette())
            {
                Debug.LogWarning("LowHealthVignette: no Vignette found in assigned Volume/profile.");
            }

            if (_vignette != null)
            {
                _baseIntensity = _vignette.intensity.value;
                _baseColor = _vignette.color.value;
                _vignette.smoothness.value = _baseSmoothness;
                Debug.Log("LowHealthVignette: Vignette located and initialized.");
            }
        }

        private void OnEnable()
        {
            TrySubscribeToRunState();
        }

        private void Start()
        {
            // Ensure we subscribe even if RunState was not ready at OnEnable
            TrySubscribeToRunState();
        }

        private void TrySubscribeToRunState()
        {
            if (_subscribedToRunState) return;
            if (GameStateManager.RunState == null) return;

            GameStateManager.RunState.OnHpChanged += HandleHpChanged;
            _subscribedToRunState = true;
            Debug.Log("LowHealthVignette: subscribed to RunState.OnHpChanged");
            HandleHpChanged(GameStateManager.RunState.CurrentHp, GameStateManager.RunState.MaxHp);
        }

        private void OnDisable()
        {
            if (GameStateManager.RunState != null)
                GameStateManager.RunState.OnHpChanged -= HandleHpChanged;

            ResetVignette();
        }

        private void Update()
        {
            if (!_isCritical || _vignette == null)
                return;

            _flashTimer += Time.unscaledDeltaTime;
            float pulse = Mathf.PingPong(_flashTimer * _flashFrequency, 1f);
            _vignette.intensity.value = Mathf.Lerp(_minIntensity, _maxIntensity, pulse);
            _vignette.color.value = _criticalColor;
            _vignette.smoothness.value = _criticalSmoothness;
        }

        private void HandleHpChanged(float currentHp, float maxHp)
        {
            if (_vignette == null)
                return;

            float normalizedHp = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            bool shouldBeCritical = normalizedHp <= _criticalHealthThreshold && normalizedHp > 0f;

            if (shouldBeCritical && !_isCritical)
            {
                _isCritical = true;
                _flashTimer = 0f;
            }
            else if (!shouldBeCritical && _isCritical)
            {
                _isCritical = false;
                ResetVignette();
            }
        }

        private void ResetVignette()
        {
            if (_vignette == null)
                return;

            _vignette.color.value = _baseColor;
            _vignette.intensity.value = _baseIntensity;
            _vignette.smoothness.value = _baseSmoothness;
        }

        private bool TryGetVignette()
        {
            if (_volume == null || _volume.profile == null)
                return false;

            return _volume.profile.TryGet(out _vignette);
        }
    }
}
