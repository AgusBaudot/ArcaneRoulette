using System.Collections;
using Foundation;
using UnityEngine;

namespace Core
{
    public sealed class DoTComponent : MonoBehaviour
    {
        private IDamageable _target;
        private int _damagePerTick;
        private float _interval;
        private float _remainingDuration;
        private ElementType _element;
        private Coroutine _tickRoutine;

        public void Apply(IDamageable target, int damagePerTick, float tickInterval, float duration,
            ElementType element)
        {
            _target = target;
            _damagePerTick = damagePerTick;
            _interval = tickInterval;
            _remainingDuration = duration; //refresh on re-apply, never accumulate
            _element = element;

            if (_tickRoutine == null)
                _tickRoutine = StartCoroutine(TickRoutine());
        }

        private IEnumerator TickRoutine()
        {
            while (_remainingDuration > 0f)
            {
                yield return CoroutineUtils.GetWait(_interval);
                _remainingDuration -= _interval;

                if (_target == null)
                    break;
                
                var batch = new DamageBatch();
                batch.Deal(_target, gameObject, _damagePerTick, _element);
                batch.Commit(Helpers.Combat.NoFeedback);
            }
            
            _tickRoutine = null;
            Destroy(this);
        }
    }
}