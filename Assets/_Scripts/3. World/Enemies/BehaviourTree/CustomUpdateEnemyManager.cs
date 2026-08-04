using System.Collections.Generic;
using Foundation;
using UnityEngine;

namespace World 
{
    public class CustomUpdateEnemyManager: MonoBehaviour, IUpdatable
    {
        public int UpdatePriority => Foundation.UpdatePriority.AI;
        public static CustomUpdateEnemyManager Instance { get; private set; }

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
            //Esto lo puedo cambiar por un parametro que (si el enemigo es mas importante) actualice mas rapido o mas lento segun el intervalo.
            enemy.interval = Random.Range(0.1f, 0.2f);
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
            for (int i = 0; i <_enemyUpdatables.Count; i++) 
            {
                if (_enemyUpdatables[i] == null) continue;

                try
                {
                    _enemyUpdatables[i].timer += dt;

                    if (_enemyUpdatables[i].timer >= _enemyUpdatables[i].interval)
                    {
                        _enemyUpdatables[i].timer = 0f;
                        _enemyUpdatables[i].Tick();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[CustomUpdateEnemyManager] Error en el tick del enemigo en indice {i}: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}
