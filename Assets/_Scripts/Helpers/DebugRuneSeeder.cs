using UnityEngine;
using Foundation;
using Core;

public class DebugRuneSeeder : MonoBehaviour
{
    [Header("Ability Runes")]
    [SerializeField] private ProjectileAbilityRune _projectileRune;
    [SerializeField] private DashAbilityRune       _dashRune;
    [SerializeField] private ShieldAbilityRune     _shieldRune;

    [Header("Cast Runes")]
    [SerializeField] private PiercingCastRune      _piercingRune;
    [SerializeField] private BounceCastRune        _bounceRune;
    [SerializeField] private AmplifyCastRune       _amplifyRune;

    [Header("Test Configuration")]
    [SerializeField] private SlotIndex _testSlot      = SlotIndex.BasicAttack;
    [SerializeField] private TestRecipe _testRecipe   = TestRecipe.Projectile_Piercing;
    [SerializeField] private int _runeInventoryCount  = 9; // enough for any combo

    private SpellCrafter _crafter;

    public enum TestRecipe
    {
        Projectile_Plain,
        Projectile_Piercing,
        Projectile_Piercing_x2,
        Projectile_Bounce,
        Projectile_Amplify,
        Projectile_Piercing_Bounce,
        Dash_Plain,
        Dash_Piercing,
        Dash_Bounce,
        Dash_Amplify,
        Shield_Plain,
        Shield_Bounce,
        Shield_Amplify,
    }

    private void Start()
    {
        _crafter = GetComponent<SpellCrafter>();
        
        SeedInventory();
        CraftTestRecipe();
    }

    private void SeedInventory()
    {
        var state = GameStateManager.RunState;
        
        //Seed everything generously - we're testing combos, not scarcity
        state.AddRune(_projectileRune, _runeInventoryCount);
        state.AddRune(_dashRune, _runeInventoryCount);
        state.AddRune(_shieldRune, _runeInventoryCount);
        state.AddRune(_piercingRune, _runeInventoryCount);
        state.AddRune(_bounceRune, _runeInventoryCount);
        state.AddRune(_amplifyRune, _runeInventoryCount);
        
        //Always seed all three abilities into their correct slots regardless of test
        _crafter.TryCreate(new SpellRecipe(_dashRune, null, null), SlotIndex.Dash, out _);
        _crafter.TryCreate(new SpellRecipe(_shieldRune, null, null), SlotIndex.Shield, out _);
    }

    private void CraftTestRecipe()
    {
        var modifiers = new ModifierRuneSO[SpellRecipe.MODIFIER_SLOTS];

        switch (_testRecipe)
        {
            case TestRecipe.Projectile_Plain:
                break;
            
            case TestRecipe.Projectile_Piercing:
                modifiers[0] = _piercingRune;
                break;

            case TestRecipe.Projectile_Piercing_x2:
                modifiers[0] = _piercingRune;
                modifiers[1] = _piercingRune; // two slots, stackCount = 2
                break;

            case TestRecipe.Projectile_Bounce:
                modifiers[0] = _bounceRune;
                break;

            case TestRecipe.Projectile_Amplify:
                modifiers[0] = _amplifyRune;
                break;

            case TestRecipe.Projectile_Piercing_Bounce:
                modifiers[0] = _piercingRune;
                modifiers[1] = _bounceRune;
                break;

            case TestRecipe.Dash_Plain:
                _crafter.TryCreate(new SpellRecipe(_dashRune, null, null), SlotIndex.Dash, out _);
                return;

            case TestRecipe.Dash_Piercing:
                _crafter.TryCreate(
                    new SpellRecipe(_dashRune, null, new ModifierRuneSO[] { _piercingRune }),
                    SlotIndex.Dash, out _);
                return;

            case TestRecipe.Dash_Bounce:
                _crafter.TryCreate(
                    new SpellRecipe(_dashRune, null, new ModifierRuneSO[] { _bounceRune }),
                    SlotIndex.Dash, out _);
                return;

            case TestRecipe.Dash_Amplify:
                _crafter.TryCreate(
                    new SpellRecipe(_dashRune, null, new ModifierRuneSO[] { _amplifyRune }),
                    SlotIndex.Dash, out _);
                return;

            case TestRecipe.Shield_Plain:
                return; // already seeded in SeedInventory

            case TestRecipe.Shield_Bounce:
                _crafter.TryCreate(
                    new SpellRecipe(_shieldRune, null, new ModifierRuneSO[] { _bounceRune }),
                    SlotIndex.Shield, out _);
                return;

            case TestRecipe.Shield_Amplify:
                _crafter.TryCreate(
                    new SpellRecipe(_shieldRune, null, new ModifierRuneSO[] { _amplifyRune }),
                    SlotIndex.Shield, out _);
                return;
        }
        
        //Projectile recipes all land here
        _crafter.TryCreate(new SpellRecipe(_projectileRune, null, modifiers), SlotIndex.BasicAttack, out _);
    }
}