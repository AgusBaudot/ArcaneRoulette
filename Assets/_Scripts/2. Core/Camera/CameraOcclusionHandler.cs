using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    public class ScreenThresholdOcclusion : MonoBehaviour, IUpdatable
    {
        [Header("Settings")] 
        [Tooltip("0.5 is half screen, 0.25 is bottom fourth.")]
        [SerializeField, Range(0f, 1f)]
        private float _viewportThreshold = 0.33f;

        [Tooltip("The target alpha value when below the threshold.")] 
        [SerializeField, Range(0, 1)]
        private float _targetAlpha = 0.2f;

        [Tooltip("How fast the alpha transitions (units per second).")] 
        [SerializeField]
        private float _fadeSpeed = 5f;

        public int UpdatePriority => Foundation.UpdatePriority.Camera;

        private static readonly int OpacityPropertyId = Shader.PropertyToID("_Opacity");

        private Camera _mainCamera;
        private MaterialPropertyBlock _mpb;
        
        // Tracks current alpha of objects currently being faded
        private readonly Dictionary<Renderer, float> _trackedRenderers = new();
        private readonly List<Renderer> _keysSnapshot = new();

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            // Cache camera using your project's custom utility if available, 
            // or standard Camera.main for isolated testing.
            _mainCamera = Camera.main; 
        }

        private void OnEnable()
        {
            UpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            RestoreAllRenderers();
        }

        public void Tick(float deltaTime)
        {
            if (_mainCamera == null) return;

            foreach (IOccluder occluder in OcclusionRegistry.Occluders)
            {
                Renderer r = occluder.Renderer;
                if (r == null) continue;

                Vector3 viewportPos = _mainCamera.WorldToViewportPoint(occluder.Position);
                
                bool isBelowThreshold = viewportPos.y < _viewportThreshold && viewportPos.z > 0f;

                if (isBelowThreshold)
                {
                    _trackedRenderers.TryAdd(r, 1f);
                }
                else if (!_trackedRenderers.ContainsKey(r))
                {
                    continue;
                }
            }
            
            _keysSnapshot.Clear();
            _keysSnapshot.AddRange(_trackedRenderers.Keys);

            foreach (Renderer r in _keysSnapshot)
            {
                if (r == null)
                {
                    _trackedRenderers.Remove(r);
                    continue;
                }

                Vector3 viewPos = _mainCamera.WorldToViewportPoint(r.transform.position);
                bool isBelow = viewPos.y < _viewportThreshold && viewPos.z > 0f;

                float targetAlpha = isBelow ? _targetAlpha : 1f;
                float currentAlpha = _trackedRenderers[r];
                float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, _fadeSpeed * deltaTime);

                if (!isBelow && newAlpha >= 1f)
                {
                    r.SetPropertyBlock(null);
                    _trackedRenderers.Remove(r);
                }
                else
                {
                    _trackedRenderers[r] = newAlpha;
                    r.GetPropertyBlock(_mpb);
                    _mpb.SetFloat(OpacityPropertyId, newAlpha);
                    r.SetPropertyBlock(_mpb);
                }
            }
        }

        private void RestoreAllRenderers()
        {
            foreach (Renderer r in _trackedRenderers.Keys)
            {
                if (r != null) r.SetPropertyBlock(null);
            }
            _trackedRenderers.Clear();
        }
    }
}