using Foundation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meta
{
    [RequireComponent(typeof(Selectable))]
    public sealed class UIAudioTrigger : MonoBehaviour,
        IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        [SerializeField] private AudioEventSO _onHover;
        [SerializeField] private AudioEventSO _onClick;
        [SerializeField] private AudioEventSO _onExit;   // optional, most UI skips this

        public void OnPointerEnter(PointerEventData _) => Publish(_onHover);
        public void OnPointerClick(PointerEventData _) => Publish(_onClick);
        public void OnPointerExit(PointerEventData _)  => Publish(_onExit);

        private static void Publish(AudioEventSO e)
        {
            if (e == null)
                return;
            
            EventBus.Publish(new AudioPlayRequest { Event = e });
        }
    }
}