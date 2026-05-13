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
        [SerializeField] [Range(0, 1)] private float _alpha = 0.5f;

        public int UpdatePriority => Foundation.UpdatePriority.Camera;

        //Track state to avoid GetComponent calls every frame and handle restoring
        private readonly HashSet<Renderer> _hiddenRenderers = new();
        private readonly HashSet<Renderer> _renderersHitThisFrame = new();

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
            
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, _sphereCastRadius, direction.normalized, direction.magnitude, _occluderLayer);

            foreach (RaycastHit hit in hits)
            {
                Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
                if (hitRenderer != null)
                {
                    _renderersHitThisFrame.Add(hitRenderer);

                    if (!_hiddenRenderers.Contains(hitRenderer))
                    {
                        Color c = hitRenderer.material.GetColor("_BaseColor");
                        hitRenderer.material.SetColor("_BaseColor", new Color(c.r, c.g, c.b, _alpha));
                        _hiddenRenderers.Add(hitRenderer);
                    }
                }
            }

            _hiddenRenderers.RemoveWhere(renderer =>
            {
                if (!_renderersHitThisFrame.Contains(renderer))
                {
                    if (renderer != null)
                    {
                        Color c = renderer.material.GetColor("_BaseColor");
                        renderer.material.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 1));
                    }

                    return true;
                }

                return false;
            });
        }

        private void RestoreAllRenderers()
        {
            foreach (Renderer r in _hiddenRenderers)
            {
                if (r != null)
                {
                    Color c = r.material.GetColor("_BaseColor");
                    r.material.SetColor("_BaseColor", new Color(c.r, c.g, c.b, 1));
                }
            }
            _hiddenRenderers.Clear();
        }
    }
}