using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace World 
{
    /// <summary>Implemented by enemies that must be ticked every frame while mid-action (see AIBrain.NeedsFastTick).</summary>
    public interface IFastTickable
    {
        bool NeedsFastTick { get; }
    }

    public class CustomUpdateEnemyManager: MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;
        public static CustomUpdateEnemyManager Instance { get; private set; }

        [Tooltip("Random per-enemy decision interval (seconds) while idle/chasing. Enemies mid-action tick every frame.")]
        [SerializeField] private Vector2 _intervalRange = new Vector2(0.05f, 0.1f);

        private List<IEnemyUpdate> _enemyUpdatables = new List<IEnemyUpdate>();

        public void Awake()
        {
            Instance = this;
        }
        
        // ---- Register & UnRegister from UpdateManager ----
        public void Start()
        {
            if (UpdateManager.Instance != null)
            {
                UpdateManager.Instance.Register(this);
            }
            else
            {
                Debug.LogError($"<color=red>FATAL ERROR:</color> {gameObject.name} failed to register to UpdateManager!!!");
            }
        }
        
        public void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
        }

        // ---- Register & UnRegister enemies from this ---
        public void Register(IEnemyUpdate enemy)
        {
            if (_enemyUpdatables.Contains(enemy)) 
                return;
            enemy.interval = Random.Range(_intervalRange.x, _intervalRange.y);
            enemy.timer = Random.Range(0f, enemy.interval);
            _enemyUpdatables.Add(enemy);
        }
        
        public void Unregister(IEnemyUpdate enemy)
        {
            _enemyUpdatables.Remove(enemy);
        }

        // ---- Tick Method ----
        public void Tick(float dt)
        {
            TickEnemies(dt);
        }

        public void TickEnemies(float dt)
        {
            for (int i = _enemyUpdatables.Count - 1; i >= 0; i--) 
            {
                if (i >= _enemyUpdatables.Count) continue;
                IEnemyUpdate enemy = _enemyUpdatables[i];
                if (enemy == null) continue;

                try
                {
                    IFastTickable fast = enemy as IFastTickable;
                    bool wantsFast = fast != null && fast.NeedsFastTick;

                    enemy.timer += dt;
                    if (!wantsFast && enemy.timer < enemy.interval) continue;

                    enemy.timer = 0f;
                    enemy.Tick();

                    if (wantsFast && !fast.NeedsFastTick)
                        enemy.timer = enemy.interval;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CustomUpdateEnemyManager] Error en el tick del enemigo en indice {i}: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}