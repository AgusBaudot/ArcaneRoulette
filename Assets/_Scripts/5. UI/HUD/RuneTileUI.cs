using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Foundation;

namespace UI
{
    /// <summary>
    /// Single rune tile. Purely visual — displays what it's told.
    /// Init once, Refresh whenever state changes. No drag, no drop.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class RuneTileUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private GameObject highlight;

        private Button _button;
        private Action _onClick;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Init(Action onClick)
        {
            _onClick = onClick;
        }

        /// <summary>
        /// Call whenever the tile's represented rune or state changes.
        /// </summary>
        public void Refresh(RuneDefinitionSO rune, int count, bool highlighted)
        {
            bool hasRune = rune != null;

            icon.sprite = hasRune ? rune.Icon : null;
            icon.enabled = hasRune;
            icon.preserveAspect = true;

            countText.text = hasRune && count > 1 ? count.ToString() : "";
            countText.enabled = hasRune && count > 1;

            highlight.SetActive(highlighted);

            // Slot is clickable even when empty — clicking empty slot
            // with a pending rune assigns it; without one, does nothing.
            _button.interactable = true;
        }
    }
}