using UnityEngine;

namespace Foundation
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }
        
        [SerializeField] private float _maxOffset = 0.25f;
        [SerializeField] private float _traumaDecay = 2.2f;
        [SerializeField] private float _noiseFrequency = 20f;
        
        //Trauma values to start with: 0.15f for a standard hit, 0.4f for a boss hit, 0.6f for player death.
        private float _trauma;
        private Vector3 _originLocalPos;

        private void Awake()
        {
            Instance = this;
            _originLocalPos = transform.localPosition;
        }

        /// <summary>
        /// Trauma is clamped 0-1. Shake intensity = trauma^2. Stack additive calls.
        /// </summary>
        /// <param name="amount"></param>
        public static void AddTrauma(float amount)
        {
            if (Instance == null) return;
            Debug.Log("Called camera shake");
            Instance._trauma = Mathf.Clamp01(Instance._trauma + amount);
        }

        private void LateUpdate()
        {
            if (_trauma <= 0f) return;

            float shake = _trauma * _trauma; // squared = punchy falloff.
            float t = Time.unscaledTime * _noiseFrequency; // unscaled: shakes during hitstop.

            transform.localPosition = _originLocalPos + new Vector3(
                _maxOffset * (Mathf.PerlinNoise(t, 0f) * 2f - 1f) * shake,
                _maxOffset * (Mathf.PerlinNoise(0f, t) * 2f - 1f) * shake,
                0f
            );

            _trauma = Mathf.Max(0f, _trauma - _traumaDecay * Time.unscaledDeltaTime);

            if (_trauma <= 0f)
                transform.localPosition = _originLocalPos;
        }
    }
}