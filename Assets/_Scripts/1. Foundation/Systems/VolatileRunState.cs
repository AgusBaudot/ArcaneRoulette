using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation
{
    /// <summary>
    /// All volatile state that exists only for the duration of one run.
    /// Dies on death or win. Never serialized. Never referenced by Meta.
    /// </summary>
    public class VolatileRunState
    {
        // ── HP ──────────────────────────────────────────────────────────────────

        public float MaxHp     { get; private set; }
        public float CurrentHp { get; private set; }
        public event Action<float, float> OnHpChanged;

        // ── Currency (in-run only) ───────────────────────────────────────────

        public int Currency { get; private set; }
        public event Action<int> OnCurrencyChanged;

        // ── Spell slots ─────────────────────────────────────────────────────
        // Index maps to SlotIndex enum: 0 = BasicAttack, 1 = Dash, 2 = Shield.
        // NOTE: SpellInstance is Core — this field is a pre-existing band violation.
        // Needs a dedicated fix (interface slot contract in Foundation, or move
        // VolatileRunState to Core). Leaving unchanged to avoid scope creep.

        private readonly ISpellSlot[] _slots = new ISpellSlot[3];
        public ISpellSlot GetSlot(SlotIndex slot) => _slots[(int)slot];
        public event Action<SlotIndex, ISpellSlot> OnSlotChanged; // slot, new instance

        // ── Rune inventory ──────────────────────────────────────────────────
        // runeInventory  — total owned this run (drops + debug seeding)
        // runeAllocated  — currently locked into a SpellRecipe via SpellCrafter
        // No OnChange events yet. Dev 3 polls both dictionaries on panel open.

        private readonly Dictionary<RuneDefinitionSO, int> _runeInventory = new();
        private readonly Dictionary<RuneDefinitionSO, int> _runeAllocated = new();

        // Read-only views — CraftingUI polls these directly, no event needed yet.
        public IReadOnlyDictionary<RuneDefinitionSO, int> RuneInventory  => _runeInventory;
        public IReadOnlyDictionary<RuneDefinitionSO, int> RuneAllocated  => _runeAllocated;

        // ── Floor / Room progress ────────────────────────────────────────────

        public int CurrentFloor { get; private set; }
        public int CurrentRoom  { get; private set; }
        public event Action<int, int> OnRoomChanged; // floor, room

        // ── Modifier pipeline ────────────────────────────────────────────────
        // Owned here, subscribed to by InventorySystem (artifact hooks).

        public event Action<DamageContext> OnCalculateDamageOut;
        public event Action<DamageContext> OnCalculateDamageIn;
        public event Action<KillContext>   OnKill;
        public event Action<SpellContext>  OnSpellCast;
        public event Action<DashContext>   OnDash;

        // ── Construction ─────────────────────────────────────────────────────

        public VolatileRunState(float maxHp)
        {
            MaxHp     = maxHp;
            CurrentHp = maxHp;
        }

        // ── HP ───────────────────────────────────────────────────────────────

        public void SetHp(float value)
        {
            CurrentHp = Mathf.Clamp(value, 0, MaxHp);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void SetMaxHp(float value, bool healDelta = true)
        {
            float delta = value - MaxHp;
            MaxHp = Mathf.Max(1f, value);
            if (healDelta && delta > 0)
                SetHp(CurrentHp + delta);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        // ── Currency ─────────────────────────────────────────────────────────

        public void AddCurrency(int amount)
        {
            Currency += amount;
            OnCurrencyChanged?.Invoke(Currency);
        }

        // ── Spell slots ──────────────────────────────────────────────────────

        public void SetSlot(SlotIndex slot, ISpellSlot instance)
        {
            _slots[(int)slot] = instance;
            OnSlotChanged?.Invoke(slot, instance);
        }

        // ── Rune inventory ───────────────────────────────────────────────────
        // AddRune      — called by room reward / debug seeder
        // AllocateRune / DeallocateRune — called exclusively by SpellCrafter
        //                                 on TryCreate / Dismantle
        
        public void AddRune(RuneDefinitionSO rune, int count = 1)
        {
            _runeInventory.TryGetValue(rune, out int current);
            _runeInventory[rune] = current + count;
            // No event fire — UI polls on panel open (prototype shortcut)
        }

        public void AllocateRune(RuneDefinitionSO rune, int count = 1)
        {
            _runeAllocated.TryGetValue(rune, out int current);
            _runeAllocated[rune] = current + count;
        }

        public void DeallocateRune(RuneDefinitionSO rune, int count = 1)
        {
            if (!_runeAllocated.TryGetValue(rune, out int current)) return;
            _runeAllocated[rune] = Mathf.Max(0, current - count);
        }

        // AvailableCount is the single source of truth for "can I use one more
        // of this rune". TryGetValue defaults to 0 for unknown keys — a rune
        // not yet seen this run is correctly treated as unavailable.
        public int AvailableCount(RuneDefinitionSO rune)
        {
            _runeInventory.TryGetValue(rune, out int owned);
            _runeAllocated.TryGetValue(rune, out int allocated);
            return owned - allocated;
        }

        // ── Room ─────────────────────────────────────────────────────────────

        public void SetRoom(int floor, int room)
        {
            CurrentFloor = floor;
            CurrentRoom  = room;
            OnRoomChanged?.Invoke(floor, room);
        }

        // ── Pipeline fire methods ────────────────────────────────────────────
        // Called by CombatSystem / DamageSystem, never by runes directly.

        public void FireDamageOut(DamageContext ctx) => OnCalculateDamageOut?.Invoke(ctx);
        public void FireDamageIn(DamageContext ctx)  => OnCalculateDamageIn?.Invoke(ctx);
        public void FireKill(KillContext ctx)         => OnKill?.Invoke(ctx);
        public void FireSpellCast(SpellContext ctx)   => OnSpellCast?.Invoke(ctx);
        public void FireDash(DashContext ctx)         => OnDash?.Invoke(ctx);

        // ── Reset ────────────────────────────────────────────────────────────
        // Called by GameStateManager on run end. Nulls all events so no
        // subscriber from the dead run can leak into the next one.
        // Clears dictionaries rather than replacing them to reuse allocations.

        public void Reset()
        {
            // HP / economy
            OnHpChanged       = null;
            OnCurrencyChanged = null;

            // Spell slots
            OnSlotChanged = null;
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = null;

            // Rune inventory — clear contents, keep the Dictionary allocations
            _runeInventory.Clear();
            _runeAllocated.Clear();

            // Room progress
            OnRoomChanged = null;

            // Modifier pipeline
            OnCalculateDamageOut = null;
            OnCalculateDamageIn  = null;
            OnKill               = null;
            OnSpellCast          = null;
            OnDash               = null;
        }
    }
}