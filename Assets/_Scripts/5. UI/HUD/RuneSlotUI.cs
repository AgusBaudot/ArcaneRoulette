using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Foundation;

public class RuneSlotUI : MonoBehaviour
{
    public enum VisualCategory
    {
        Inventory, // inventory tiles style themselves by rune type
        Ability,
        Element,
        Modifier
    }

    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image highlightImage;

    [Header("Visual Style (icon-only; no slot background)")]
    [SerializeField] private bool preserveIconAspect = true;
    [SerializeField] private bool tintByCategory = false;
    [SerializeField] private Color abilityTint = Color.white;
    [SerializeField] private Color elementTint = Color.white;
    [SerializeField] private Color modifierTint = Color.white;
    [SerializeField] private float abilityIconScale = 1.25f;
    [SerializeField] private float elementIconScale = 1.05f;
    [SerializeField] private float modifierIconScale = 0.9f;
    
    private RuneDefinitionSO _currentRune;
    private int _availableCount;
    private System.Action<RuneDefinitionSO> _onSelected;
    private System.Action<RuneSlotUI> _onCraftSlotClicked;
    private bool _isSelected;
    private bool _isInventorySlot;
    private VisualCategory _category = VisualCategory.Inventory;
    private Image _slotBackgroundImage;

    private void Awake()
    {
        // Auto-wire common child references so the prefab can stay minimal.
        if (iconImage == null)
        {
            var iconTf = transform.Find("Icon");
            if (iconTf != null) iconImage = iconTf.GetComponent<Image>();
        }
        if (amountText == null)
        {
            var amtTf = transform.Find("AmountText");
            if (amtTf != null) amountText = amtTf.GetComponent<TextMeshProUGUI>();
        }
        if (slotButton == null)
            slotButton = GetComponent<Button>();
        _slotBackgroundImage = GetComponent<Image>();
        if (_slotBackgroundImage != null)
        {
            // Art direction: slot background comes from panel art, not from tile prefab.
            var c = _slotBackgroundImage.color;
            // Keep it effectively invisible, but non-zero alpha so Unity raycasts
            // can still hit the slot during drag/drop.
            _slotBackgroundImage.color = new Color(c.r, c.g, c.b, 0.001f);
        }

        if (iconImage != null && preserveIconAspect)
            iconImage.preserveAspect = true;

        if (slotButton != null)
            slotButton.onClick.AddListener(OnSlotClicked);
    }

    public void SetCategory(VisualCategory category)
    {
        _category = category;
        ApplyStyle();
    }

    public void Setup(RuneDefinitionSO rune, int availableCount, System.Action<RuneDefinitionSO> onSelected)
    {
        _currentRune = rune;
        _availableCount = availableCount;
        _onSelected = onSelected;
        _isSelected = false;

        UpdateDisplay();
    }

    public void SetAsInventorySlot()
    {
        _isInventorySlot = true;
        SetCategory(VisualCategory.Inventory);
    }

    public void SetAsCraftingSlot(System.Action<RuneSlotUI> onCraftSlotClicked)
    {
        _isInventorySlot = false;
        _onCraftSlotClicked = onCraftSlotClicked;
        if (slotButton != null)
            slotButton.interactable = true;
    }

    private void UpdateDisplay()
    {
        if (_currentRune == null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (amountText != null)
                amountText.text = "";

            if (slotButton != null)
            {
                // Inventory slots require a rune to be interactable;
                // craft slots can remain interactable for receiving runes.
                slotButton.interactable = !_isInventorySlot;
            }
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = _currentRune.Icon;
            iconImage.enabled = true;
        }
        
        if (amountText != null)
            amountText.text = _availableCount > 0 ? _availableCount.ToString() : "0";
        
        if (slotButton != null)
        {
            // Inventory slots should be interactable only when available > 0.
            // Craft slots must stay interactable for drag/drop even when the rune
            // is already allocated (AvailableCount can be 0 while still selected).
            slotButton.interactable = _isInventorySlot ? _availableCount > 0 : true;
        }

        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (iconImage == null) return;

        VisualCategory cat = _category;
        if (cat == VisualCategory.Inventory && _currentRune != null)
        {
            // Inventory tiles style by rune type.
            if (_currentRune is AbilityRuneSO) cat = VisualCategory.Ability;
            else if (_currentRune is ElementRuneSO) cat = VisualCategory.Element;
            else if (_currentRune is ModifierRuneSO) cat = VisualCategory.Modifier;
        }

        float scale = 1f;
        Color tint = Color.white;

        switch (cat)
        {
            case VisualCategory.Ability:
                scale = abilityIconScale;
                tint = abilityTint;
                break;
            case VisualCategory.Element:
                scale = elementIconScale;
                tint = elementTint;
                break;
            case VisualCategory.Modifier:
                scale = modifierIconScale;
                tint = modifierTint;
                break;
        }

        iconImage.transform.localScale = Vector3.one * scale;
        iconImage.color = tintByCategory ? tint : Color.white;
    }

    private void OnSlotClicked()
    {
        if (_isInventorySlot)
        {
            if (_currentRune == null || _availableCount <= 0)
                return;

            SetSelected(!_isSelected);
            _onSelected?.Invoke(_isSelected ? _currentRune : null);
            return;
        }

        // Crafting panel slot
        _onCraftSlotClicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (highlightImage != null)
            highlightImage.gameObject.SetActive(_isSelected);
    }

    public RuneDefinitionSO GetCurrentRune() => _currentRune;
    public bool IsSelected() => _isSelected;
    public int GetAvailableCount() => _availableCount;
}
