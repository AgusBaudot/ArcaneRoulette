using UnityEngine;
using Foundation;
using Core;

public sealed class DebugRuneSeeder : MonoBehaviour
{
    [Header("Ability Runes")]
    [SerializeField] private ProjectileAbilityRune  _projectileRune;
    [SerializeField] private DashAbilityRune        _dashRune;
    [SerializeField] private ShieldAbilityRune      _shieldRune;

    [Header("Cast Runes")]
    [SerializeField] private PiercingCastRune       _piercingRune;
    [SerializeField] private BounceCastRune         _bounceRune;
    [SerializeField] private AmplifyCastRune        _amplifyRune;

    [Header("OnHit Runes")]
    [SerializeField] private AoEOnHitRune           _aoeRune;
    [SerializeField] private KnockbackOnHitRune     _knockbackRune;
    [SerializeField] private DoTOnHitRune           _dotRune;

    [Header("Inventory Seed Count")]
    [SerializeField] private int _runeInventoryCount = 9;

    [Header("Slot Configuration")]
    [SerializeField] private SlotConfig _slot0;
    [SerializeField] private SlotConfig _slot1;
    [SerializeField] private SlotConfig _slot2;

    private SpellCrafter _crafter;

    // ── Slot config ──────────────────────────────────────────────────────────

    [System.Serializable]
    public sealed class SlotConfig
    {
        public AbilityRune  Ability;
        public ElementRune  Element;
        [Space]
        public ModifierRune Modifier0;
        public ModifierRune Modifier1;
        public ModifierRune Modifier2;
        public ModifierRune Modifier3;
        public ModifierRune Modifier4;
    }

    // Enums drive the inspector dropdowns.
    // Adding a new rune SO means adding one entry here and one case below — nothing else.

    public enum AbilityRune
    {
        None,
        Projectile,
        Dash,
        Shield
    }

    public enum ElementRune
    {
        None,
        Fire,
        Water,
        Lightning
    }

    public enum ModifierRune
    {
        None,
        Piercing,
        Bounce,
        Amplify,
        AoE,
        Knockback,
        DoT
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        _crafter = GetComponent<SpellCrafter>();
        SeedInventory();
        CraftSlot(SlotIndex.Slot0, _slot0);
        CraftSlot(SlotIndex.Slot1, _slot1);
        CraftSlot(SlotIndex.Slot2, _slot2);
    }

    // ── Inventory ────────────────────────────────────────────────────────────

    private void SeedInventory()
    {
        var s = GameStateManager.RunState;

        // Ability runes
        s.AddRune(_projectileRune, _runeInventoryCount);
        s.AddRune(_dashRune,       _runeInventoryCount);
        s.AddRune(_shieldRune,     _runeInventoryCount);

        // Cast runes
        s.AddRune(_piercingRune, _runeInventoryCount);
        s.AddRune(_bounceRune,   _runeInventoryCount);
        s.AddRune(_amplifyRune,  _runeInventoryCount);

        // OnHit runes
        s.AddRune(_aoeRune,       _runeInventoryCount);
        s.AddRune(_knockbackRune, _runeInventoryCount);
        s.AddRune(_dotRune,       _runeInventoryCount);
    }

    // ── Slot crafting ────────────────────────────────────────────────────────

    private void CraftSlot(SlotIndex slot, SlotConfig config)
    {
        var ability = ResolveAbility(config.Ability);
        if (ability == null) return; // empty slot — leave it unequipped

        var element = ResolveElement(config.Element);

        var modifiers = new ModifierRuneSO[SpellRecipe.MODIFIER_SLOTS];
        modifiers[0] = ResolveModifier(config.Modifier0);
        modifiers[1] = ResolveModifier(config.Modifier1);
        modifiers[2] = ResolveModifier(config.Modifier2);
        modifiers[3] = ResolveModifier(config.Modifier3);
        modifiers[4] = ResolveModifier(config.Modifier4);

        var recipe = new SpellRecipe(ability, element, modifiers);

        if (!_crafter.TryCreate(recipe, slot, out _))
            Debug.LogWarning($"DebugRuneSeeder: TryCreate failed for {slot} " +
                             $"— insufficient inventory or invalid recipe.");
    }

    // ── Resolvers ────────────────────────────────────────────────────────────

    private AbilityRuneSO ResolveAbility(AbilityRune rune) => rune switch
    {
        AbilityRune.Projectile => _projectileRune,
        AbilityRune.Dash       => _dashRune,
        AbilityRune.Shield     => _shieldRune,
        _                      => null
    };

    private ElementRuneSO ResolveElement(ElementRune rune) => rune switch
    {
        ElementRune.Fire      => null, // assign SO refs when ElementRuneSO assets exist
        ElementRune.Water     => null,
        ElementRune.Lightning => null,
        _                     => null
    };

    private ModifierRuneSO ResolveModifier(ModifierRune rune) => rune switch
    {
        ModifierRune.Piercing  => _piercingRune,
        ModifierRune.Bounce    => _bounceRune,
        ModifierRune.Amplify   => _amplifyRune,
        ModifierRune.AoE       => _aoeRune,
        ModifierRune.Knockback => _knockbackRune,
        ModifierRune.DoT       => _dotRune,
        _                      => null
    };
}