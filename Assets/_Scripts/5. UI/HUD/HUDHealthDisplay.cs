using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class HUDHealthDisplay : MonoBehaviour
    {
        [SerializeField] private RectMask2D _healthFillMask;

        private void OnEnable()
        {
            GameStateManager.RunState.OnHpChanged += UpdateUI;
            
            UpdateUI(GameStateManager.RunState.CurrentHp, GameStateManager.RunState.MaxHp);
        }

        private void OnDisable()
        {
            GameStateManager.RunState.OnHpChanged -= UpdateUI;
        }
        
        private void UpdateUI(float currentHp, float maxHp)
        {
            float normalizedHealth = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            float maskWidth = _healthFillMask.GetComponent<RectTransform>().rect.width;
            float rightPadding = Mathf.RoundToInt((1f - normalizedHealth) * maskWidth);
            _healthFillMask.padding = new Vector4(0f, 0f, rightPadding, 0f);
        }
            
            //IF WE EVER HAVE EMPTY HEARTS ---------------------------------------
            
            // int currentHp = (int)Current;
            //
            // // Divide max HP by 4 to get total containers (e.g., 12 max HP = 3 hearts)
            // int totalHeartContainers = Mathf.CeilToInt(GameStateManager.RunState.MaxHp / 4f);
            //
            // for (int i = 0; i < _heartsContainer.childCount; i++)
            // {
            //     Transform child = _heartsContainer.GetChild(i);
            //
            //     // Only activate children up to our max heart containers
            //     bool active = i < totalHeartContainers;
            //     child.gameObject.SetActive(active);
            //
            //     if (active)
            //     {
            //         Image img = child.GetComponent<Image>();
            //         if (img != null)
            //         {
            //             // Calculate how much HP belongs in this specific heart (from 0 to 4)
            //             int hpInThisHeart = Mathf.Clamp(currentHp - (i * 4), 0, 4);
            //
            //             img.sprite = _heartSprites[hpInThisHeart];
            //         }
            //     }
            // }
    }
    
}
