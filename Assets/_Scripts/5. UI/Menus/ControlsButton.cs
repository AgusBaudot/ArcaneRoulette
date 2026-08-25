using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ControlsButton : MonoBehaviour
    {
        private void Start() => GetComponent<Button>().onClick.AddListener(
            () => EventBus.Publish(new OnControlsClickedEvent()));
    }
}