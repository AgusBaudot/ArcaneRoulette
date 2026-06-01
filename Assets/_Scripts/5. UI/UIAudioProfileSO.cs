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
        public AudioEventSO TabSwitch;

        [Header("Crafting")] public AudioEventSO CraftSuccess;
        public AudioEventSO CraftFail;
        public AudioEventSO RuneEquip;
        public AudioEventSO RuneUnequip;

        [Header("Run")] public AudioEventSO RoomCleared;
    }
}