using System;
using UnityEngine;
using UnityEngine.UI;
using Foundation;

namespace UI
{
    /// <summary>
    /// Sits on a Filter Tab object. Handles the visual sprite swap 
    /// between the active and inactive states.
    /// </summary>
    public sealed class RuneFilterTab : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _tabBackgroundImage; // The background that changes
        [SerializeField] private Button _button;

        [Header("Configuration")]
        [SerializeField] private RuneFilter _filterType;
        [SerializeField] private Sprite _unselectedSprite;
        [SerializeField] private Sprite _selectedSprite;

        public RuneFilter FilterType => _filterType;

        /// <summary>
        /// Called by the orchestrator on Awake to wire up the click event.
        /// </summary>
        public void Init(Action<RuneFilterTab> onTabClicked)
        {
            _button.onClick.AddListener(() => onTabClicked(this));
        }

        /// <summary>
        /// Visually toggles the tab and prevents clicking if already active.
        /// </summary>
        public void SetActiveState(bool isActive)
        {
            // _tabBackgroundImage.sprite = isActive ? _selectedSprite : _unselectedSprite;
            //
            // // Disable interaction if it's already the active tab to prevent spam-clicking
            // _button.interactable = !isActive; 
        }
    }
}