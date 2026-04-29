using TMPro;
using UnityEngine;
using Foundation;
using Core;

namespace UI
{
    // Singleton — lives on the Canvas. RuneTileUI calls Show/Hide.
    // Positions itself at a fixed pixel offset from the hovered tile's RectTransform.
    // Clamps to screen bounds so it never clips off-screen edges.
    public sealed class TooltipSystem : MonoBehaviour
    {
        public static TooltipSystem Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private RectTransform _panelRect;

        [Header("Text fields")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("Layout")]
        [Tooltip("Pixel offset from the tile's pivot in screen space.")]
        [SerializeField] private Vector2 _offset = new(10f, -10f);

        private bool _enabled = true;
        private Canvas _canvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _canvas = GetComponentInParent<Canvas>();
            _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Called by RuneTileUI.OnPointerEnter
        public void Show(RuneDefinitionSO rune, RectTransform tileRect)
        {
            if (rune == null || !_enabled)
                return;

            _nameText.text = rune.Name;
            _typeText.text = rune.Type;
            _descriptionText.text = rune.Description;

            _panel.SetActive(true);

            // Force layout rebuild so _panelRect.sizeDelta is accurate before clamping
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_panelRect);

            PositionTooltip(tileRect);
        }

        // Called by RuneTileUI.OnPointerExit
        public void Hide()
        {
            _panel.SetActive(false);
        }

        private void PositionTooltip(RectTransform tileRect)
        {
            // Convert tile's world position to screen point (null camera = Screen Space Overlay)
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, tileRect.position);
            screenPoint += _offset;

            // Convert screen point to local position inside the canvas
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.GetComponent<RectTransform>(),
                screenPoint,
                null,
                out Vector2 localPoint);

            _panelRect.localPosition = localPoint;

            ClampToScreen();
        }

        private void ClampToScreen()
        {
            // Ensure tooltip never escapes canvas bounds regardless of offset direction
            var canvasRect = _canvas.GetComponent<RectTransform>();
            Vector3 pos = _panelRect.localPosition;

            float canvasHalfW = canvasRect.rect.width * 0.5f;
            float canvasHalfH = canvasRect.rect.height * 0.5f;
            float panelHalfW = _panelRect.rect.width * 0.5f;
            float panelHalfH = _panelRect.rect.height * 0.5f;

            pos.x = Mathf.Clamp(pos.x, -canvasHalfW + panelHalfW, canvasHalfW - panelHalfW);
            pos.y = Mathf.Clamp(pos.y, -canvasHalfH + panelHalfH, canvasHalfH - panelHalfH);

            _panelRect.localPosition = pos;
        }
        
        public void ToggleEnabled()
        {
            _enabled = !_enabled;
            Hide();
        }

        // private static string GetRuneTypeLabel(RuneDefinitionSO rune)
        // {
        //     return rune switch
        //     {
        //         AbilityRuneSO  => "Ability",
        //         ElementRuneSO  => "Element",
        //         CastRuneSO     => "Cast",
        //         OnHitRuneSO    => "On Hit",
        //         _              => "Rune"
        //     };
        // }
    }
}