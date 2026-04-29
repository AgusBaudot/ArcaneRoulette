using System.Collections.Generic;
using UnityEngine;
using Foundation;
using Core;

public sealed class StartRuneSeeder : MonoBehaviour
{
    [Header("Ability Runes")] 
    [SerializeField] private ProjectileAbilityRune _projectileRune;
    [SerializeField] private DashAbilityRune _dashRune;
    [SerializeField] private ShieldAbilityRune _shieldRune;

    [Header("Element Runes")] 
    [SerializeField] private ElementRuneSO _fireRune;
    [SerializeField] private ElementRuneSO _waterRune;
    [SerializeField] private ElementRuneSO _lightningRune;
    [SerializeField] private ElementRuneSO _earthRune;

    [Header("Cast Runes")]
    [SerializeField] private PiercingCastRune _piercingRune;
    [SerializeField] private BounceCastRune _bounceRune;
    [SerializeField] private AmplifyCastRune _amplifyRune;
    [SerializeField] private HomingCastRune _homingRune;

    [Header("OnHit Runes")] 
    [SerializeField] private AoEOnHitRune _aoeRune;
    [SerializeField] private KnockbackOnHitRune _knockbackRune;
    [SerializeField] private DoTOnHitRune _dotRune;
    [SerializeField] private DebuffOnHitRune _debuffRune;

    [Header("Inventory Seed Count")]
    [SerializeField] private int _abilityRuneCount = 1;
    [SerializeField] private int _otherRuneCount = 5;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start() => SeedInventory();

    // ── Inventory ────────────────────────────────────────────────────────────

    private void SeedInventory()
    {
        var s = GameStateManager.RunState;

        // Ability runes
        s.AddRune(_projectileRune, _abilityRuneCount);
        s.AddRune(_dashRune, _abilityRuneCount);
        s.AddRune(_shieldRune, _abilityRuneCount);
        
        //Seed other runes
        List<RuneDefinitionSO> otherRunes = new()
        {
            _fireRune, _waterRune, _lightningRune, _earthRune,
            _piercingRune, _bounceRune, _amplifyRune, _homingRune,
            _aoeRune, _knockbackRune, _dotRune, _debuffRune
        };

        for (int i = 0; i < _otherRuneCount; i++)
        {
            int randomIndex = Random.Range(0, otherRunes.Count);
            s.AddRune(otherRunes[randomIndex]);
        }
    }
}