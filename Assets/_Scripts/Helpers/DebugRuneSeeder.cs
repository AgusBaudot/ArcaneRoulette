using UnityEngine;
using Foundation;
using Core;

public class DebugRuneSeeder : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private ProjectileAbilityRune _projectileRune;
    [SerializeField] private DashAbilityRune _dashRune;
    [SerializeField] private ShieldAbilityRune _shieldRune;

    private VolatileRunState RunState => GameStateManager.RunState;
    private SpellCrafter _spellCrafter;

    private void Start()
    {
        _spellCrafter = GetComponent<SpellCrafter>();

        // Seed runes for testing inventory + crafting.
        RunState.AddRune(_projectileRune, 3);
        RunState.AddRune(_dashRune, 2);
        RunState.AddRune(_shieldRune, 1);

        Debug.Log($"DebugRuneSeeder: Projectile available = {GameStateManager.RunState.AvailableCount(_projectileRune)}");

        // Do not auto-craft at startup so runes remain in inventory for manual use.
    }
}