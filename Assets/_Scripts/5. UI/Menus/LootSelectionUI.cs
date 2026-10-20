using System.Collections.Generic;
using DG.Tweening;
using Foundation;
using TMPro;
using World;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class LootSelectionUI : BaseUIPanel
    {
        [Header("UI Animation References")] 
        [SerializeField] private RectTransform _runeContainer;
        [SerializeField] private RectTransform _tooltip;
        [SerializeField] private RectTransform _confirm;

        [Header("Animation Settings")] 
        [Tooltip("How high up (in Canvas units) the elements start.")] 
        [SerializeField] private float _dropOffsetY = 1000f;
        [Tooltip("How long it takes for a single element to fall.")]
        [SerializeField] private float _dropDuration = 0.25f;
        [Tooltip("The strength of the Z-axis rotation stumble (degrees).")] 
        [SerializeField] private float _shakeStrength = 5f;
        [Tooltip("How long the stumble wobbles.")] 
        [SerializeField] private float _shakeDuration = 0.2f;

        [Header("Rune Display")] 
        [SerializeField] private LootOptionUI _lootOptionPrefab;

        [Header("Confirm")] 
        [SerializeField] private Button _confirmButton;

        [Header("Settings")]
        [Tooltip("How many runes to show (n).")] 
        [SerializeField] private int _runesToShow = 3;
        [Tooltip("How many the player may keep (m). Clamped to n at runtime.")]
        [SerializeField] private int _runesToSelect = 1;

        [Header("Drop Pool")]
        [SerializeField] private PickupDropPool _dropPool;
        
        [Header("Local Tooltip References")]
        [SerializeField] private GameObject _tooltipContainer;
        [SerializeField] private CanvasGroup _tooltipCanvasGroup;
        [SerializeField] private float _tooltipFadeDuration = 0.2f;
        [SerializeField] private TextMeshProUGUI _tooltipNameText;
        [SerializeField] private TextMeshProUGUI _tooltipTypeText;
        [SerializeField] private TextMeshProUGUI _tooltipDescriptionText;

        // ── Runtime ──────────────────────────────────────────────────────────

        private readonly List<LootOptionUI> _optionPool = new();
        private readonly List<int> _selectionOrder = new();
        private int _effectiveMax;

        private Vector2 _runeOriginalPos;
        private Vector2 _tooltipOriginalPos;
        private Vector2 _confirmOriginalPos;
        private Sequence _showSequence;

        // ── Unity ────────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _confirmButton.onClick.AddListener(OnConfirm);

            if (_runeContainer != null) _runeOriginalPos = _runeContainer.anchoredPosition;
            if (_tooltip != null) _tooltipOriginalPos = _tooltip.anchoredPosition;
            if (_confirm != null) _confirmOriginalPos = _confirm.anchoredPosition;

            for (int i = 0; i < _runesToShow; i++)
            {
                int capturedIndex = i;

                LootOptionUI option = Instantiate(_lootOptionPrefab, _runeContainer);

                option.Init(null, () => OnOptionClicked(capturedIndex));
                option.gameObject.SetActive(false);

                _optionPool.Add(option);
            }
            
            if (_tooltipCanvasGroup != null) _tooltipCanvasGroup.alpha = 0f;
        }

        // ── Show / Hide ───────────────────────────────────────────────────────

        public override void Show()
        {
            if (_dropPool == null) return;
    
            if (TooltipSystem.Instance != null)
                TooltipSystem.Instance.SetEnabled(false);

            ClearLocalTooltipInstantly();
    
            _selectionOrder.Clear();
            RuneDefinitionSO[] runes = _dropPool.GetRandomRunes(_runesToShow);
            _effectiveMax = Mathf.Min(_runesToSelect, runes.Length);

            for (int i = 0; i < _optionPool.Count; i++)
            {
                if (i < runes.Length)
                {
                    int capturedIndex = i;
                    RuneDefinitionSO capturedRune = runes[i];

                    _optionPool[i].Init(
                        capturedRune, 
                        () => OnOptionClicked(capturedIndex),
                        () => ShowLocalTooltip(capturedRune),
                        RestoreDefaultTooltip
                    );
            
                    _optionPool[i].SetSelected(false);
                    _optionPool[i].gameObject.SetActive(true);
                }
                else
                {
                    _optionPool[i].gameObject.SetActive(false);
                }
            }
    
            base.Show();

            _canvasGroup.interactable = false;
            _confirmButton.GetComponent<Image>().raycastTarget = false;

            _showSequence?.Kill();
            SnapToHiddenPosition();
            ResetRotations();

            _showSequence = DOTween.Sequence().SetUpdate(true);

            if (_runeContainer != null)
                _showSequence.Append(_runeContainer.DOAnchorPos(_runeOriginalPos, _dropDuration).SetEase(Ease.InQuad));
            if (_tooltip != null)
                _showSequence.Join(_tooltip
                    .DOAnchorPos(new Vector2(_tooltipOriginalPos.x, _runeOriginalPos.y), _dropDuration)
                    .SetEase(Ease.InQuad));
            if (_confirm != null)
                _showSequence.Join(_confirm
                    .DOAnchorPos(new Vector2(_confirmOriginalPos.x, _runeOriginalPos.y), _dropDuration)
                    .SetEase(Ease.InQuad));

            if (_runeContainer != null)
                _showSequence.Append(_runeContainer.DOPunchRotation(new Vector3(0, 0, _shakeStrength), _shakeDuration, 10, 0.5f));
            if (_tooltip != null)
                _showSequence.Join(_tooltip.DOAnchorPos(_tooltipOriginalPos, _dropDuration).SetEase(Ease.OutQuad));
            if (_confirm != null)
                _showSequence.Join(_confirm
                    .DOAnchorPos(new Vector2(_confirmOriginalPos.x, _tooltipOriginalPos.y), _dropDuration)
                    .SetEase(Ease.OutQuad));

            if (_tooltip != null)
                _showSequence.Append(_tooltip.DOPunchRotation(new Vector3(0, 0, _shakeStrength), _shakeDuration, 10, 0.5f));
            if (_confirm != null)
                _showSequence.Join(_confirm.DOAnchorPos(_confirmOriginalPos, _dropDuration).SetEase(Ease.OutQuad));

            if (_confirm != null)
                _showSequence.Append(_confirm.DOPunchRotation(new Vector3(0, 0, _shakeStrength), _shakeDuration, 10, 0.5f));
            
            _showSequence.OnComplete(() => 
            {
                _canvasGroup.interactable = true;
                _confirmButton.GetComponent<Image>().raycastTarget = true;
            });
        }

        public override void Hide()
        {
            base.Hide();
            
            _showSequence?.Kill();
            _selectionOrder.Clear();
    
            if (TooltipSystem.Instance != null)
                TooltipSystem.Instance.SetEnabled(true);
        }

        private void SnapToHiddenPosition()
        {
            float startingY = _runeOriginalPos.y + _dropOffsetY;

            if (_runeContainer != null) _runeContainer.anchoredPosition = new Vector2(_runeOriginalPos.x, startingY);
            if (_tooltip != null) _tooltip.anchoredPosition = new Vector2(_tooltipOriginalPos.x, startingY);
            if (_confirm != null) _confirm.anchoredPosition = new Vector2(_confirmOriginalPos.x, startingY);
        }

        private void ResetRotations()
        {
            if (_runeContainer != null) _runeContainer.localRotation = Quaternion.identity;
            if (_tooltip != null) _tooltip.localRotation = Quaternion.identity;
            if (_confirm != null) _confirm.localRotation = Quaternion.identity;
        }

        // ── Local Tooltip Logic ───────────────────────────────────────────────

        private void ShowLocalTooltip(RuneDefinitionSO rune)
        {
            if (rune == null) return;

            _tooltipNameText.text = rune.Name;
            _tooltipTypeText.text = rune.Type;
            _tooltipDescriptionText.text = rune.Description;
            
            if (_tooltipContainer != null) _tooltipContainer.SetActive(true);
            
            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.DOKill();
                _tooltipCanvasGroup.DOFade(1f, _tooltipFadeDuration).SetUpdate(true);
            }
        }

        private void ClearLocalTooltip()
        {
            if (_tooltipCanvasGroup != null)
            {
                _tooltipCanvasGroup.DOKill();
                _tooltipCanvasGroup.DOFade(0f, _tooltipFadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() => 
                    {
                        if (_tooltipContainer != null) _tooltipContainer.SetActive(false);
                        _tooltipNameText.text = string.Empty;
                        _tooltipTypeText.text = string.Empty;
                        _tooltipDescriptionText.text = string.Empty;
                    });
            }
            else
            {
                ClearLocalTooltipInstantly();
            }
        }
        
        private void ClearLocalTooltipInstantly()
        {
            if (_tooltipContainer != null) _tooltipContainer.SetActive(false);
            _tooltipNameText.text = string.Empty;
            _tooltipTypeText.text = string.Empty;
            _tooltipDescriptionText.text = string.Empty;
            if (_tooltipCanvasGroup != null) _tooltipCanvasGroup.alpha = 0f;
        }

        private void RestoreDefaultTooltip()
        {
            if (_selectionOrder.Count > 0)
            {
                int lastSelectedIndex = _selectionOrder[^1];
                ShowLocalTooltip(_optionPool[lastSelectedIndex].Rune);
            }
            else
            {
                ClearLocalTooltip();
            }
        }

        // ── Selection logic ───────────────────────────────────────────────────

        private void OnOptionClicked(int index)
        {
            if (_selectionOrder.Contains(index))
            {
                _selectionOrder.Remove(index);
            }
            else
            {
                if (_selectionOrder.Count >= _effectiveMax)
                {
                    int evicted = _selectionOrder[0];
                    _selectionOrder.RemoveAt(0);
                    _optionPool[evicted].SetSelected(false);
                }

                _selectionOrder.Add(index);
            }

            _optionPool[index].SetSelected(_selectionOrder.Contains(index));
        }

        // ── Confirm ───────────────────────────────────────────────────────────

        private void OnConfirm()
        {
            foreach (int index in _selectionOrder)
            {
                GameStateManager.RunState.AddRune(_optionPool[index].Rune);
                EventBus.Publish<RuneClaimedEvent>(new (_optionPool[index].Rune.Name));
            }

            RequestClose();
        }
    }
}