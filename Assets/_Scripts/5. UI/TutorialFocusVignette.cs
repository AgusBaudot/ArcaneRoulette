using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace UI
{
    [RequireComponent(typeof(Image))]
    public sealed class TutorialFocusVignette : MonoBehaviour, ICanvasRaycastFilter
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _focusDuration = 0.8f;
        [SerializeField] private float _targetRadiusPct = 0.05f;
        [SerializeField] private Ease _focusEase = Ease.OutCubic;

        private Material _mat;
        private RectTransform _target;
        private Canvas _canvas;
        private Tween _tween;

        private static readonly int CenterID = Shader.PropertyToID("_Center");
        private static readonly int RadiusID = Shader.PropertyToID("_Radius");
        private static readonly int AspectRatioID = Shader.PropertyToID("_AspectRatio");

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            _canvas = GetComponentInParent<Canvas>();
            
            _mat = new Material(_image.material);
            _image.material = _mat;
            
            gameObject.SetActive(false);
        }

        public void FocusOn(RectTransform targetElement)
        {
            _target = targetElement;
            gameObject.SetActive(true);
            _image.raycastTarget = true; 
            
            UpdateShaderUniforms();

            _mat.SetFloat(RadiusID, 2.0f);
            
            _tween?.Kill();
            _tween = _mat.DOFloat(_targetRadiusPct, RadiusID, _focusDuration)
                .SetEase(_focusEase)
                .SetUpdate(true); 
        }

        public void Hide()
        {
            _tween?.Kill();
            _tween = _mat.DOFloat(2.0f, RadiusID, _focusDuration * 0.5f)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() => 
                {
                    gameObject.SetActive(false);
                    _target = null;
                });
        }

        private void Update()
        {
            if (_target != null)
            {
                UpdateShaderUniforms();
            }
        }

        private void UpdateShaderUniforms()
        {
            float aspect = (float)Screen.width / Screen.height;
            _mat.SetFloat(AspectRatioID, aspect);

            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, _target.position);
            
            Vector2 uvCenter = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            _mat.SetVector(CenterID, uvCenter);
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (_target == null) return false;

            Vector2 clickUV = new Vector2(sp.x / Screen.width, sp.y / Screen.height);
            Vector2 centerUV = _mat.GetVector(CenterID);
            float aspect = _mat.GetFloat(AspectRatioID);
            
            Vector2 diff = clickUV - centerUV;
            diff.x *= aspect;
            
            float dist = diff.magnitude;
            float currentRadius = _mat.GetFloat(RadiusID);

            return dist > currentRadius;
        }

        private void OnDisable() => _tween?.Kill();
        private void OnDestroy() { if (_mat != null) Destroy(_mat); }
    }
}