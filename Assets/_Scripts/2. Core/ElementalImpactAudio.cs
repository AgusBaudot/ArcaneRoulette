using System;
using Foundation;

namespace Core
{
    [Serializable]
    public struct ElementalImpactAudio
    {
        public ElementType Element;
        public AudioEventSO ImpactSound;
    }
}