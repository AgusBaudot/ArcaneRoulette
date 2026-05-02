using System;
using Core;
using Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Action Bar HUD that displays equipped spells with live cooldown progress.
    /// - Subscribes to SpellEquippedEvent to update icons when spells change
    /// - Updates cooldown progress every frame via CooldownRemaining
    /// - Shows ability, element, and modifier rune icons for each slot
    /// </summary>
    public sealed class ActionBarHUD : MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.UI;
        
        [Serializable]
        public class SlotDisplay
        {
            [Header("Slot References")]
            public SlotIndex SlotIndex;
            
            [Header("Icon Display")]
            public Image AbilityIcon;
            public Image ElementIcon;
            public Image[] ModifierIcons = new Image[SpellRecipe.MODIFIER_SLOTS];
            
            [Header("Cooldown Visuals")]
            public Image CooldownOverlay;
            public TextMeshProUGUI CooldownText;
            public Image ReadyIndicator;
            
            // ── Runtime state ────────────────────────────────────────────
            private SpellInstance _currentSpell;
            
            public void UpdateSpell(SpellInstance spell)
            {
                _currentSpell = spell;
                RefreshDisplay();
            }
            
            public void UpdateCooldown()
            {
                if (_currentSpell == null)
                    return;
                
                float cooldownRemaining = _currentSpell.CooldownRemaining;
                float cooldownProgress = _currentSpell.CooldownProgress;
                
                // Update cooldown overlay (fills as cooldown completes)
                if (CooldownOverlay != null)
                    CooldownOverlay.fillAmount = cooldownProgress;
                
                // Update cooldown text
                if (CooldownText != null)
                {
                    if (_currentSpell.IsReady)
                    {
                        CooldownText.text = "";
                        CooldownText.enabled = false;
                    }
                    else
                    {
                        CooldownText.text = Mathf.Max(0f, cooldownRemaining).ToString("F1");
                        CooldownText.enabled = true;
                    }
                }
                
                // Update ready indicator
                if (ReadyIndicator != null)
                    ReadyIndicator.enabled = _currentSpell.IsReady;
            }
            
            private void RefreshDisplay()
            {
                if (_currentSpell == null)
                {
                    ClearDisplay();
                    return;
                }
                
                var recipe = _currentSpell.Recipe;
                
                // Display ability icon
                if (AbilityIcon != null)
                {
                    AbilityIcon.sprite = recipe.Ability?.Icon;
                    AbilityIcon.enabled = recipe.Ability != null;
                }
                
                // Display element icon
                if (ElementIcon != null)
                {
                    ElementIcon.sprite = recipe.Element?.Icon;
                    ElementIcon.enabled = recipe.Element != null;
                }
                
                // Display modifier icons
                var modifiers = recipe.Modifiers;
                for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
                {
                    if (i < ModifierIcons.Length && ModifierIcons[i] != null)
                    {
                        var modifier = i < modifiers.Count ? modifiers[i] : null;
                        ModifierIcons[i].sprite = modifier?.Icon;
                        ModifierIcons[i].enabled = modifier != null;
                    }
                }
                
                UpdateCooldown();
            }
            
            private void ClearDisplay()
            {
                if (AbilityIcon != null)
                {
                    AbilityIcon.sprite = null;
                    AbilityIcon.enabled = false;
                }
                
                if (ElementIcon != null)
                {
                    ElementIcon.sprite = null;
                    ElementIcon.enabled = false;
                }
                
                for (int i = 0; i < SpellRecipe.MODIFIER_SLOTS; i++)
                {
                    if (i < ModifierIcons.Length && ModifierIcons[i] != null)
                    {
                        ModifierIcons[i].sprite = null;
                        ModifierIcons[i].enabled = false;
                    }
                }
                
                if (CooldownOverlay != null)
                    CooldownOverlay.fillAmount = 0f;
                
                if (CooldownText != null)
                {
                    CooldownText.text = "";
                    CooldownText.enabled = false;
                }
                
                if (ReadyIndicator != null)
                    ReadyIndicator.enabled = false;
            }
        }
        
        [SerializeField] private SlotDisplay[] _slotDisplays = new SlotDisplay[3];
        
        private VolatileRunState _runState;
        private readonly SpellInstance[] _currentSpells = new SpellInstance[3];

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void OnEnable()
        {
            // Subscribe to spell equipped events
            EventBus.Subscribe<SpellEquippedEvent>(OnSpellEquipped);
            UpdateManager.Instance.Register(this);
            
            // Initialize current spells from RunState
            if (_runState == null)
                _runState = GameStateManager.RunState;
            
            if (_runState != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var spell = _runState.GetSlot((SlotIndex)i) as SpellInstance;
                    if (spell != null)
                        _currentSpells[i] = spell;
                    
                    if (i < _slotDisplays.Length && _slotDisplays[i] != null)
                        _slotDisplays[i].UpdateSpell(spell);
                }
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SpellEquippedEvent>(OnSpellEquipped);
            UpdateManager.Instance?.Unregister(this);
        }
        
        public void Tick(float dt)
        {
            // Update cooldown progress for all slots every frame
            for (int i = 0; i < 3; i++)
            {
                if (i < _slotDisplays.Length && _slotDisplays[i] != null && _currentSpells[i] != null)
                    _slotDisplays[i].UpdateCooldown();
            }
        }

        // ── Event Handling ───────────────────────────────────────────────────

        private void OnSpellEquipped(SpellEquippedEvent evt)
        {
            int slotIndex = (int)evt.Slot;
            
            if (slotIndex >= 0 && slotIndex < 3)
            {
                _currentSpells[slotIndex] = evt.Instance as SpellInstance;
                
                if (slotIndex < _slotDisplays.Length && _slotDisplays[slotIndex] != null)
                    _slotDisplays[slotIndex].UpdateSpell(_currentSpells[slotIndex]);
            }
        }
    }
}
