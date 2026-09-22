using Foundation;
using UnityEngine;
using DG.Tweening;

namespace World
{
    [RequireComponent(typeof(EnemyController), typeof(EnemyHealth))]
    public class EnemyVFXController : MonoBehaviour, IEnemyComponent
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer[] _renderers;
        
        [Header("Materials")]
        [SerializeField] private Material _spawnMaterial;
        [SerializeField] private Material _baseGameplayMaterial;
        
        [Header("Durations & Tuning")]
        [SerializeField] private float _baseSpawnDuration = 3.0f;
        [SerializeField] private float _deathDuration = 1.0f;

        [Tooltip("If true, scales the duration so the visual speed matches the 1.5 baseline. If false, takes exactly Base Spawn Duration.")]
        [SerializeField] private bool _scaleDurationToMaintainSpeed = true;

        [Tooltip("The _SpawnProgress where the feet start. 0 for tight crops, >0 for empty bottom space.")]
        [SerializeField] private float _minSpawnProgress = 0f;
        
        [Tooltip("The _SpawnProgress where the head finishes. 1.5 for tight crops, <1.5 for empty top space.")]
        [SerializeField] private float _maxSpawnProgress = 1.5f;

        private static readonly int SpawnProgressID = Shader.PropertyToID("_SpawnProgress");
        private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
        
        private EnemyController _controller;
        private Animator _animator;

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
            _animator = GetComponentInChildren<Animator>();
            GetComponent<EnemyHealth>().OnDeath += HandleDeath;
        }

        public void InitComponent(EnemyStats stats, Blackboard blackboard) { }

        public void ResetComponent()
        {
            if (_animator != null) _animator.speed = 1f;

            foreach (var r in _renderers)
            {
                r.material = _spawnMaterial;
                Material mat = r.material;
                
                mat.DOKill();
                
                // 1. Instantly snap the start value to the feet
                mat.SetFloat(SpawnProgressID, _minSpawnProgress);

                // 2. Calculate the actual physical distance the wave needs to travel
                float travelDistance = _maxSpawnProgress - _minSpawnProgress;
                
                // 3. Apply the toggle logic
                float finalDuration = _scaleDurationToMaintainSpeed 
                    ? _baseSpawnDuration * (travelDistance / 1.5f) 
                    : _baseSpawnDuration;

                mat.DOFloat(_maxSpawnProgress, SpawnProgressID, finalDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => 
                    {
                        r.material = _baseGameplayMaterial;
                        if (r.material.HasProperty(DissolveAmountID))
                            r.material.SetFloat(DissolveAmountID, 0f);
                    });
            }
        }

        private void HandleDeath()
        {
            if (_animator != null) _animator.speed = 0f;

            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                var tween = r.material.DOFloat(1f, DissolveAmountID, _deathDuration).SetEase(Ease.InSine);
                
                if (i == 0) 
                {
                    tween.OnComplete(() => PoolEnemy.Instance.Release(_controller.Type, _controller));
                }
            }
        }
    }
}