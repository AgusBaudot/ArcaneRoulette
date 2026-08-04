using Foundation;
using UnityEngine;

namespace UI
{
    [CreateAssetMenu(menuName = "ScriptableObjects/UI Audio Profile", fileName = "UIAudioProfileSO")]
    public sealed class UIAudioProfileSO : ScriptableObject
    {
        [Header("Navigation")] public AudioEventSO MenuOpen;
        public AudioEventSO MenuClose;
        public AudioEventSO CarouselSlide;
        [Tooltip("Played when the player clicks on a filter. Clicking on the current filter doesn't play any sound.")]
        public AudioEventSO TabSwitch;

        [Header("Crafting")] 
        [Tooltip("Played when the player successfully equips a rune.")]
        public AudioEventSO RuneEquip;
        [Tooltip("Played when the player tried to equip a rune, but failed.")]
        public AudioEventSO RuneFail;
        [Tooltip("Played when the player clicks on a rune already equipped.")]
        public AudioEventSO RuneUnequip;
    }
}