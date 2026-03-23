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
        RunState.AddRune(_projectileRune, 1);
        RunState.AddRune(_dashRune, 1);
        RunState.AddRune(_shieldRune, 1);
        Debug.Log(GameStateManager.RunState.AvailableCount(_projectileRune));
        _spellCrafter.TryCreate(new SpellRecipe(_projectileRune, null, null), SlotIndex.BasicAttack, out _);
        _spellCrafter.TryCreate(new SpellRecipe(_dashRune, null, null), SlotIndex.Dash, out _);
        bool second = _spellCrafter.TryCreate(new SpellRecipe(_projectileRune, null, null), SlotIndex.Shield, out _);
        Debug.Log(second);
        _spellCrafter.Dismantle(SlotIndex.BasicAttack);
        bool retry = _spellCrafter.TryCreate(new SpellRecipe(_projectileRune, null, null), SlotIndex.Shield, out _);
        Debug.Log(retry);
    }
}