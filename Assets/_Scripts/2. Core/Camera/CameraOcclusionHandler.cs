using System;
using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace Core
{
    public class CameraOcclusionHandler : MonoBehaviour, IUpdatable
    {
        [Header("References")] [SerializeField]
        private Transform _playerTransform;

        [Header("Settings")] [Tooltip("Layer assigned to walls/objects that can hide the player.")] [SerializeField]
        private LayerMask _occluderLayer;

        [Tooltip("Using a sphere cast prevents thin objects from flickering at the edges.")] [SerializeField]
        private float _sphereCastRadius = 0.5f;

        [Tooltip("Offset to aim the the player's chest rather than their feet.")] [SerializeField]
        private Vector3 _targetOffset = new(0f, 1f, 0f);

        [Tooltip("The target alpha value when fully occluding the player.")] [SerializeField, Range(0, 1)]
        private float _targetAlpha = 0.5f;

        [Tooltip("How fast the alpha transitions (units per second).")] [SerializeField]
        private float _fadeSpeed = 5f;

        public int UpdatePriority => Foundation.UpdatePriority.Camera;

        private static readonly int OpacityPropertyId = Shader.PropertyToID("_Opacity");

        //Track state to avoid GetComponent calls every frame and handle restoring
        private readonly Dictionary<Renderer, float> _trackedRenderers = new();
        private readonly HashSet<Renderer> _renderersHitThisFrame = new();
        
        private readonly List<Renderer> _keysSnapshot = new();

        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
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
            if (_playerTransform == null)
                return;

            _renderersHitThisFrame.Clear();
            Vector3 direction = _playerTransform.position + _targetOffset - transform.position;

            RaycastHit[] hits = Physics.SphereCastAll(transform.position, _sphereCastRadius, direction.normalized,
                direction.magnitude, _occluderLayer);

            foreach (RaycastHit hit in hits)
            {
                Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
                
                if (hitRenderer == null) 
                    continue;
                
                _renderersHitThisFrame.Add(hitRenderer);

                _trackedRenderers.TryAdd(hitRenderer, 1);
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

                bool isHit = _renderersHitThisFrame.Contains(r);
                float targetAlpha = isHit ? _targetAlpha : 1f;

                float newAlpha = Mathf.MoveTowards(_trackedRenderers[r], targetAlpha, _fadeSpeed * deltaTime);

                if (!isHit && newAlpha >= 1f)
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
                if (r != null)
                {
                    r.SetPropertyBlock(null);
                }
            }

            _trackedRenderers.Clear();
        }
    }
}