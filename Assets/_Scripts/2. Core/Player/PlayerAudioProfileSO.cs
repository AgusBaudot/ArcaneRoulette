using Foundation;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(menuName = "ScriptableObjects/PlayerAudioProfileSO")]
    public class PlayerAudioProfileSO : ScriptableObject
    {
        [Header("Movement")]
        public AudioEventSO Footsteps;
        
        [Header("Health")]
        public AudioEventSO TakeDamage;
        public AudioEventSO Death;
        public AudioEventSO Heal;
    }
}