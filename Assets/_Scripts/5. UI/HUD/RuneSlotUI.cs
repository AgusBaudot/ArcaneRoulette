using UnityEngine;
using UnityEngine.UI;
using Foundation;

public class RuneSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text amountText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image highlightImage;
    
    private RuneDefinitionSO _currentRune;
    private int _availableCount;
    private System.Action<RuneDefinitionSO> _onSelected;
    private System.Action<RuneSlotUI> _onCraftSlotClicked;
    private bool _isSelected;
    private bool _isInventorySlot;

    private void Awake()
    {
        if (slotButton != null)
            slotButton.onClick.AddListener(OnSlotClicked);
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
                iconImage.sprite = null;
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
            iconImage.sprite = _currentRune.Icon;
        
        if (amountText != null)
            amountText.text = _availableCount > 0 ? _availableCount.ToString() : "0";
        
        if (slotButton != null)
            slotButton.interactable = _availableCount > 0;
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
