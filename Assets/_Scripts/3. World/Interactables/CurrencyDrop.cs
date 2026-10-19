using System.Collections;
using Foundation;
using UnityEngine;
using DG.Tweening;

namespace World
{
    public sealed class CurrencyDrop : MonoBehaviour, IPoolable
    {
        [Header("Physical Drop Settings")]
        [SerializeField] private float _jumpPower = 6f;
        [SerializeField] private float _gravity = 20f;
        [SerializeField] private float _horizontalForce = 3f;
        [SerializeField] private int _bounces = 2;
        [SerializeField, Range(0f, 1f)] private float _bounciness = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _friction = 0.6f;

        [Header("Flight Settings")]
        [SerializeField] private float _flyDuration = 0.5f;
        [Tooltip("Viewport coordinate of the HUD counter. E.g., Top-Right 1920x1080 offset (-75, -325) = (0.96, 0.70)")]
        [SerializeField] private Vector2 _hudViewportTarget = new Vector2(0.96f, 0.70f);

        [Header("Feedback")]
        [SerializeField] private AudioEventSO _gainSound;

        private static readonly int StartTimeID = Shader.PropertyToID("_StartTime");
        private MaterialPropertyBlock _mpb;
        private MeshRenderer _meshRenderer;
        private Coroutine _dropRoutine;

        private void Awake()
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        public void OnSpawn()
        {
            transform.localScale = Vector3.one;

            if (_meshRenderer != null)
            {
                _meshRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(StartTimeID, Time.time);
                _meshRenderer.SetPropertyBlock(_mpb);
            }
        }

        public void InitDrop(Vector3 origin)
        {
            if (_dropRoutine != null) StopCoroutine(_dropRoutine);
            _dropRoutine = StartCoroutine(DropRoutine(origin));
        }

        private IEnumerator DropRoutine(Vector3 origin)
        {
            Vector3 pos = origin;
            transform.position = pos;

            // Generate an outward push on the XZ plane to prevent stacked drops
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            Vector3 velocity = new Vector3(randomCircle.x, 0f, randomCircle.y) * Random.Range(_horizontalForce * 0.5f, _horizontalForce);
            
            // Initial upward burst
            velocity.y = Random.Range(_jumpPower * 0.8f, _jumpPower * 1.2f); 

            int remainingBounces = _bounces;
            float groundY = origin.y; 

            while (remainingBounces > 0)
            {
                velocity.y -= _gravity * Time.deltaTime;
                pos += velocity * Time.deltaTime;

                if (pos.y <= groundY && velocity.y < 0)
                {
                    pos.y = groundY;
                    velocity.y = Mathf.Abs(velocity.y) * _bounciness;
                    
                    velocity.x *= _friction;
                    velocity.z *= _friction;
                    
                    remainingBounces--;
                    if (velocity.y < 0.5f) remainingBounces = 0;
                }

                transform.position = pos;
                yield return null;
            }

            // Brief pause so the player registers the physical landing
            yield return CoroutineUtils.GetWait(0.15f);

            FlyToUI();
        }

        private void FlyToUI()
        {
            Camera cam = CameraUtils.GetCamera();
            if (cam == null)
            {
                Collect();
                return;
            }

            // Shrink as it approaches the "UI counter slot"
            transform.DOScale(0f, _flyDuration).SetEase(Ease.InBack);

            // Capture the exact rest position on the floor so it stays pinned there 
            // even if the camera moves, preventing the start of the trail from dragging.
            Vector3 startWorldPos = transform.position;
            float t = 0f;

            DOTween.To(() => t, x => t = x, 1f, _flyDuration)
                .SetTarget(transform)
                .SetEase(Ease.InQuad)
                .OnUpdate(() =>
                {
                    if (cam != null)
                    {
                        // Dynamically calculate the 3D position of the 2D UI slot this exact frame
                        Vector3 targetWorldPos = cam.ViewportToWorldPoint(new Vector3(
                            _hudViewportTarget.x, 
                            _hudViewportTarget.y, 
                            cam.nearClipPlane + 2f));
                            
                        transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
                    }
                })
                .OnComplete(Collect);
        }

        private void Collect()
        {
            GameStateManager.RunState.AddCurrency(1);

            if (_gainSound != null)
            {
                EventBus.Publish(new AudioPlayRequest { Event = _gainSound, WorldPosition = transform.position });
            }

            Helpers.ProjFactory.Despawn(gameObject);
        }

        public void OnDespawn()
        {
            if (_dropRoutine != null)
            {
                StopCoroutine(_dropRoutine);
                _dropRoutine = null;
            }
            transform.DOKill(); 
        }
    }
}