using UnityEngine;
using Foundation;

namespace World
{
    [DisallowMultipleComponent]
    public sealed class CurrencyDropSpawner : MonoBehaviour
    {
        [Tooltip("Assign the CurrencyDrop prefab here.")]
        [SerializeField] private CurrencyDrop _currencyDropPrefab;

        private void OnEnable()
        {
            EventBus.Subscribe<CurrencyDropEvent>(HandleCurrencyDrop);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CurrencyDropEvent>(HandleCurrencyDrop);
        }

        private void HandleCurrencyDrop(CurrencyDropEvent evt)
        {
            if (_currencyDropPrefab == null)
            {
                Debug.LogWarning("[CurrencyDropSpawner] Prefab is missing! Cannot spawn currency.");
                
                GameStateManager.RunState.AddCurrency(1);
                return;
            }

            CurrencyDrop drop = Helpers.ProjFactory.Spawn(
                _currencyDropPrefab, 
                evt.WorldPosition, 
                Quaternion.identity
            );

            drop.InitDrop(evt.WorldPosition);
        }
    }
}