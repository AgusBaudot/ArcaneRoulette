using System;
using DG.Tweening;
using UnityEngine;

namespace World
{
    [RequireComponent(typeof(EnemyHealth), typeof(EnemyController))]
    public class EnemyDeathVFX : MonoBehaviour, IEnemyComponent
    {
        [SerializeField] private SpriteRenderer[] _renderers;
        [SerializeField] private float _dissolveDuration = 1.0f;
        
        private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
        private EnemyController _controller;
        private Animator _animator;

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
            _animator = GetComponent<Animator>();
            
            GetComponent<EnemyHealth>().OnDeath += HandleDeath;
        }

        public void InitComponent(EnemyStats stats, Blackboard bb)
        { }

        public void ResetComponent()
        {
            _animator.speed = 1f;
            
            foreach (var r in _renderers)
            {
                if (r.material.HasProperty(DissolveAmountID))
                {
                    r.material.SetFloat(DissolveAmountID, 0f);
                }
            }
        }

        private void HandleDeath()
        {
            _animator.speed = 0f;
            
            if (_renderers.Length == 0)
            {
                PoolEnemy.Instance.Release(_controller.Type, _controller);
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].material.DOFloat(1f, DissolveAmountID, _dissolveDuration).SetEase(Ease.InSine);
            }

            _renderers[0].material.DOFloat(1f, DissolveAmountID, _dissolveDuration).SetEase(Ease.InSine)
                .OnComplete(() => 
                {
                    PoolEnemy.Instance.Release(_controller.Type, _controller);
                });
        }
    }
}