using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class HUDHealthDisplay : MonoBehaviour
    {
        [SerializeField] private Transform _heartsContainer;
        [Tooltip("0 = 1/4, 1 = 1/2, 2 = 3/4, 3 = Full")]
        [SerializeField] private Sprite[] _heartSprites;

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
            for (int i = 0; i < _heartsContainer.childCount; i++)
            {
                Transform child = _heartsContainer.GetChild(i);
        
                // Only activate children up to the currently needed visible hearts
                bool active = i < Mathf.CeilToInt(currentHp / 4);
                child.gameObject.SetActive(active);

                if (active)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        // Calculate how much HP belongs in this specific heart (from 1 to 4)
                        int hpInThisHeart = Mathf.Clamp((int)currentHp - (i * 4), 1, 4);

                        // Subtract 1 because your array is size 4 (1 HP = index 0, 4 HP = index 3)
                        img.sprite = _heartSprites[hpInThisHeart - 1];
                    }
                }
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
}