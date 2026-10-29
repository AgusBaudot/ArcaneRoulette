using UnityEngine;
using TMPro;
using DG.Tweening;
using Foundation;

namespace Core
{
    [RequireComponent(typeof(TextMeshPro))]
    public class FloatingTextVFX : MonoBehaviour, IPoolable
    {
        private TextMeshPro _tmp;
        private Sequence _sequence;

        private void Awake()
        {
            _tmp = GetComponent<TextMeshPro>();
            
            transform.rotation = Quaternion.Euler(28f, 0f, 0f);
        }

        public void Init(string text, float startDelay = 0f)
        {
            _tmp.text = text;
            _tmp.alpha = 0f;
            transform.localScale = Vector3.one;

            _sequence?.Kill();
            _sequence = DOTween.Sequence();

            if (startDelay > 0f)
            {
                _sequence.AppendInterval(startDelay);
            }

            _sequence.Append(_tmp.DOFade(1f, 0.1f));
            
            _sequence.Append(transform.DOMoveY(transform.position.y + 2.5f, 1.5f).SetEase(Ease.OutCubic));
            _sequence.Join(transform.DOScale(Vector3.one * 0.6f, 1.5f).SetEase(Ease.InCubic));
            _sequence.Join(_tmp.DOFade(0f, 1.5f).SetEase(Ease.InExpo));

            _sequence.OnComplete(() => Helpers.ProjFactory.Despawn(gameObject));
        }

        public void OnSpawn() 
        {
        }

        public void OnDespawn()
        {
            _sequence?.Kill();
        }
    }
}