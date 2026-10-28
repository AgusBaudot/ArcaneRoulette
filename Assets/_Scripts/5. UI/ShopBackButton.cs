using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ShopBackButton : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private Ease _ease = Ease.OutBack;

        private Button _button;
        private Transform _parentTransform;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _parentTransform = transform.parent;
            
            _button.onClick.AddListener(ResetParentRotation);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(ResetParentRotation);
        }

        private void ResetParentRotation()
        {
            _parentTransform.DOKill();
            
            _parentTransform.DOLocalRotate(Vector3.zero, _duration)
                .SetEase(_ease)
                .SetUpdate(true)
                .OnComplete(() => _parentTransform.localRotation = Quaternion.Euler(0f, 0f, -25f));
        }
    }
}