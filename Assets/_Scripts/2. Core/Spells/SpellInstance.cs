using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation;

namespace Core
{
    /// <summary>
    /// Runtime snapshot of an equipped spell. Pure data, no behaviour.
    /// Created by AttunementSystem from a SpellDefinition SO.
    /// Foundation holds this so VolatileRunState can reference it legally.
    /// </summary>

    //SpellCrafter is the only factory. Never construct directly.
    //For hold abilities SpellCrafter produces HoldSpellInstance instead.
    public class SpellInstance : IAbility, ISpellSlot, ISpellSource, ISpellEventSource
    {
        private readonly SpellRecipe _recipe;
        private readonly List<CastRuneSO> _castRunes;
        private readonly List<OnHitRuneSO> _onHitRunes;
        private readonly int[] _castCounts;
        private readonly int[] _onHitCounts;
        
        //Cast rune unsubscription closures - populates by SubscribeRunes(),
        //drained by Cleanup(). Each Action removes exactly one delegate
        //from one ability hook event.
        private readonly List<Action> _cleanupActions = new();

        private float _cooldownRemaining;

        public float CooldownRemaining => _cooldownRemaining;
        public float CooldownDuration => _recipe.Ability.CooldownDuration;
        public float CooldownProgress => CooldownDuration > 0
            ? 1f - (_cooldownRemaining / CooldownDuration)
            : 1f;
        
        //ISpellEventSource implementation
        public event Action<ProjectileFireArgs> OnBeforeFire;
        public event Action<DashActivationArgs> OnBeforeActivate;
        public event Action<ShieldActivationArgs> OnBeforeStartHold;

        public SpellRecipe Recipe => _recipe;
        public AbilityType AbilityType => _recipe.Ability.Type;
        public bool IsHoldAbility => false;
        public bool IsReady => _cooldownRemaining <= 0f;
        public ElementType SpellElement
            => _recipe.HasElement ? _recipe.Element.Element : ElementType.Neutral;
        
        public virtual ShieldInstanceState ShieldState => null;
        public virtual float DisplayProgress => CooldownProgress;

        internal SpellInstance(SpellRecipe recipe)
        {
            _recipe = recipe;
            BuildDedupedLists(recipe,
                out _castRunes, out _castCounts,
                out _onHitRunes, out _onHitCounts);
            
            //Subscribe cast runes once for this instance's lifetime.
            //Hooks fire inside each ability's Activate/StartHold.
            SubscribeRunes();
        }
        
        // ── Rune subscription ────────────────────────────────────────────────
        private void SubscribeRunes()
        {
            for (int i = 0; i < _castRunes.Count; i++)
                _castRunes[i].Subscribe(_recipe.Ability, this, _castCounts[i], _cleanupActions);
        }
 
        // Called by SpellCrafter.Dismantle before slot deallocation.
        // Drains _cleanupActions — each closure unsubscribes one delegate.
        // Safe to call more than once (list is cleared after drain).
        internal void Cleanup()
        {
            foreach (var action in _cleanupActions) action();
            _cleanupActions.Clear();
        }
        
        // ── Tick ─────────────────────────────────────────────────────────────
        
        public virtual void Tick(float dt)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= dt;
        }
 
        // ── IAbility ─────────────────────────────────────────────────────────
        
        public void Activate(MonoBehaviour runner)
        {
            if (!IsReady) return;
            
            //Cast rune hooks were subscribed at construction and fire inside 
            //the ability's Activate via the args object it allocates.
            var ctx = BuildCastContext(runner);
            
            _recipe.Ability.Activate(ctx); //ability reads its own fields, ctx.Source available

            _cooldownRemaining = _recipe.Ability.CooldownDuration;
        }
        
        // ── OnHit pipeline ────────────────────────────────────────

        public void TriggerOnHit(Vector3 position, GameObject target, MonoBehaviour runner)
            => TriggerOnHit(position, target, runner, _recipe.Ability.Type);

        // For cases where the physical object triggering OnHit should be treated as another ability type
        // (e.g. shield-reflected projectiles should behave like Projectile hits).
        public void TriggerOnHit(
            Vector3 position,
            GameObject target,
            MonoBehaviour runner,
            AbilityType abilityTypeForContext)
            => TriggerOnHit(position, target, runner, abilityTypeForContext, excludeBounceCastRune: false);

        public void TriggerOnHit(
            Vector3 position,
            GameObject target,
            MonoBehaviour runner,
            AbilityType abilityTypeForContext,
            bool excludeBounceCastRune,
            Vector3 attackerDirection = default)
        {
            int[] castCounts = _castCounts;
            if (excludeBounceCastRune)
            {
                // On reflected projectiles we must treat BounceCastRune as "not present"
                // for OnHit rune logic that may inspect cast stacks.
                castCounts = (int[])_castCounts.Clone();
                for (int i = 0; i < _castRunes.Count; i++)
                {
                    if (_castRunes[i] is BounceCastRune)
                        castCounts[i] = 0;
                }
            }
            
            //The callback captures 'this' and 'runner' - secondary hits re-enter TriggerOnHit
            //on this same SpellInstance, propagating the full OnHit rune chain.
            Action<Vector3, GameObject, Vector3> secondary = (pos, tgt, secDir)
                => TriggerOnHit(pos, tgt, runner, abilityTypeForContext, excludeBounceCastRune, secDir);

            var ctx = SpellContext.ForHit(
                abilityTypeForContext, castCounts, _onHitCounts, position, target, runner,
                _recipe.HasElement ? _recipe.Element.Element : ElementType.Neutral,
                secondary, _recipe.Ability, this, attackerDirection);
            FireOnHitRunes(ctx);
        }

        // ── Shared helpers ───────────────────────────────────────────────────

        //Constructs SpellContext for Activate/StartHold.
        protected SpellContext BuildCastContext(MonoBehaviour runner)
            => SpellContext.ForCast(
                _recipe.Ability.Type, _castCounts, _onHitCounts,
                runner, SpellElement, _recipe.Ability, this);

        protected void FireOnHitRunes(SpellContext ctx)
        {
            for (int i = 0; i < _onHitRunes.Count; i++)
                _onHitRunes[i].Apply(ctx, _onHitCounts[i]);
        }

        // ── Rune list construction ────────────────────────────────────────────

        // Deduplicates modifier slots by SO reference (same asset = same rune).
        // Slot order is irrelevant per spec; only identity and count matter.
        private static void BuildDedupedLists(
            SpellRecipe recipe,
            out List<CastRuneSO> castRunes, out int[] castCounts,
            out List<OnHitRuneSO> onHitRunes, out int[] onHitCounts)
        {
            var castDict = new Dictionary<CastRuneSO, int>();
            var onHitDict = new Dictionary<OnHitRuneSO, int>();

            foreach (var modifier in recipe.Modifiers)
            {
                if (modifier == null) continue;

                if (modifier is CastRuneSO cast)
                    castDict[cast] = castDict.TryGetValue(cast, out var cc) ? cc + 1 : 1;
                
                else if (modifier is OnHitRuneSO onHit)
                    onHitDict[onHit] = onHitDict.TryGetValue(onHit, out var oc) ? oc + 1 : 1;
            }

            castRunes = new List<CastRuneSO>(castDict.Keys);
            castCounts = new int[castRunes.Count];
            for (int i = 0; i < castRunes.Count; i++)
                castCounts[i] = castDict[castRunes[i]];

            onHitRunes = new List<OnHitRuneSO>(onHitDict.Keys);
            onHitCounts = new int[onHitRunes.Count];
            for (int i = 0; i < onHitRunes.Count; i++)
                onHitCounts[i] = onHitDict[onHitRunes[i]];
        }
        
        public void RaiseBeforeFire(ProjectileFireArgs args) => OnBeforeFire?.Invoke(args);

        public void RaiseBeforeActivate(DashActivationArgs args) => OnBeforeActivate?.Invoke(args);

        public void RaiseBeforeStartHold(ShieldActivationArgs args) => OnBeforeStartHold?.Invoke(args);
    }
}