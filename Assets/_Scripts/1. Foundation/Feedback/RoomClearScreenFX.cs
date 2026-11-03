using UnityEngine;
using UnityEngine.Rendering;

namespace Foundation
{
    public class RoomClearScreenFX : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.Camera;

        [Header("References")]
        [SerializeField] private Volume _sustainVolume;
        [SerializeField] private Volume _flashVolume;
        [SerializeField] private Camera _camera;

        [Header("Sustain (follows the slow-mo)")]
        [Tooltip("Seconds (unscaled) for the effect to fade in at the start.")]
        [SerializeField, Min(0.001f)] private float _attackTime = 0.08f;
        [Tooltip("Maps the raw 1->0 slow-mo intensity to the volume weight on the way out.")]
        [SerializeField] private AnimationCurve _releaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Fraction of ortho size removed at full intensity (0.04 = 4% punch-in).")]
        [SerializeField, Range(0f, 0.2f)] private float _zoomAmount = 0.04f;

        [Header("Impact flash")]
        [SerializeField, Min(0.01f)] private float _flashDuration = 0.12f;
        [Tooltip("Higher = sharper decay.")]
        [SerializeField, Min(1f)] private float _flashFalloffPower = 2f;

        private float _sustain;
        private float _flashTime;
        private bool _flashActive;
        private bool _zoomActive;
        private float _baseOrthoSize;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
            ResetFX();
        }

        private void OnEnable()
        {
            HitStop.RoomClearStarted += OnRoomClearStarted;
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            HitStop.RoomClearStarted -= OnRoomClearStarted;
            UpdateManager.Instance?.Unregister(this);
            ResetFX();
        }

        private void OnRoomClearStarted()
        {
            _flashActive = true;
            _flashTime = 0f;
        }

        public void Tick(float deltaTime)
        {
            float dt = Time.unscaledDeltaTime;
            UpdateSustain(dt);
            UpdateFlash(dt);
        }

        private void UpdateSustain(float dt)
        {
            float target = _releaseCurve.Evaluate(HitStop.RoomClearIntensity);

            _sustain = target > _sustain
                ? Mathf.MoveTowards(_sustain, target, dt / _attackTime)
                : target;

            if (_sustainVolume != null)
                _sustainVolume.weight = _sustain;

            ApplyZoom();
        }

        private void UpdateFlash(float dt)
        {
            if (!_flashActive) return;

            _flashTime += dt / _flashDuration;

            if (_flashTime >= 1f)
            {
                _flashActive = false;
                if (_flashVolume != null) _flashVolume.weight = 0f;
                return;
            }

            if (_flashVolume != null)
                _flashVolume.weight = Mathf.Pow(1f - _flashTime, _flashFalloffPower);
        }

        private void ApplyZoom()
        {
            if (_camera == null || !_camera.orthographic) return;

            if (_sustain > 0f)
            {
                if (!_zoomActive)
                {
                    _baseOrthoSize = _camera.orthographicSize;
                    _zoomActive = true;
                }
                _camera.orthographicSize = _baseOrthoSize * (1f - _zoomAmount * _sustain);
            }
            else if (_zoomActive)
            {
                _camera.orthographicSize = _baseOrthoSize;
                _zoomActive = false;
            }
        }

        private void ResetFX()
        {
            _sustain = 0f;
            _flashActive = false;

            if (_sustainVolume != null) _sustainVolume.weight = 0f;
            if (_flashVolume != null) _flashVolume.weight = 0f;

            if (_zoomActive && _camera != null)
                _camera.orthographicSize = _baseOrthoSize;
            _zoomActive = false;
        }
    }
}