using System;
using UnityEngine;

namespace Foundation
{
     /// <summary>
     /// All subtle state that exists only for the duration of one run.
     /// Dies on death or win. Never serialized. Never referenced by Meta.
     /// </summary>
     public class VolatileRunState
     {
         //HP
         public float MaxHp { get; private set; }
         public float CurrentHp { get; private set; }
         public event Action<float, float> OnHpChanged;
         
         //Currency (in-run only)
         public int Currency { get; private set; }
         public event Action<int> OnCurrencyChanged;
         
         //Active spell slots
         //Index maps to SlotIndex enum: 0 = BasicAttack, 1 = Dash, 2 = Shield
         private readonly SpellInstance[] _slots = new SpellInstance[3];
         public SpellInstance GetSlot(SlotIndex slot) => _slots[(int)slot];
         public event Action<SlotIndex, SpellInstance> OnSlotChanged; //slot, new instance
         
         //Floor / Room progress (read by UI, written by RoomManager via EventBus)
         public int CurrentFloor { get; private set; }
         public int CurrentRoom { get; private set; }
         public event Action<int, int> OnRoomChanged; //Floor, room
         
         //Modifier pipeline (owned here, populated by InventorySystem)
         public event Action<DamageContext> OnCalculateDamageOut; //outgoing
         public event Action<DamageContext> OnCalculateDamageIn; //incoming
         public event Action<KillContext> OnKill;
         public event Action<SpellContext> OnSpellCast;
         public event Action<DashContext> OnDash;
         
         //Methods
         public VolatileRunState(float maxHp)
         {
             MaxHp = maxHp;
             CurrentHp = maxHp;
         }

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

         public void AddCurrency(int amount)
         {
             Currency += amount;
             OnCurrencyChanged?.Invoke(Currency);
         }

         public void SetSlot(SlotIndex slot, SpellInstance instance)
         {
             _slots[(int)slot] = instance;
             OnSlotChanged?.Invoke(slot, instance);
         }

         public void SetRoom(int floor, int room)
         {
             CurrentFloor = floor;
             CurrentRoom = room;
             OnRoomChanged?.Invoke(floor, room);
         }
         
         //Pipeline fire methods - called by CombatSystem / DamageSystem
         public void FireDamageOut(DamageContext ctx) => OnCalculateDamageOut?.Invoke(ctx);
         public void FireDamageIn(DamageContext ctx) => OnCalculateDamageIn?.Invoke(ctx);
         public void FireSkill(KillContext ctx) => OnKill?.Invoke(ctx);
         public void FireSpellCast(SpellContext ctx) => OnSpellCast?.Invoke(ctx);
         public void FireDash(DashContext ctx) => OnDash?.Invoke(ctx);
         
         //Called by GameStateManager(? at run end - wipes all subscriptions
         public void Reset()
         {
             OnHpChanged = null;
             OnCurrencyChanged = null;
             OnSlotChanged = null;
             OnRoomChanged = null;
             OnCalculateDamageOut = null;
             OnCalculateDamageIn = null;
             OnKill = null;
             OnSpellCast = null;
             OnDash = null;
             //Am I forgetting one of them? Check if all of them are present here.
         }
     }
}