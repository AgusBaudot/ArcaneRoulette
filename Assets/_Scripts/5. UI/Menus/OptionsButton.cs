using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class OptionsButton : MonoBehaviour
    {
        private void Start() => GetComponent<Button>().onClick.AddListener(
            () => EventBus.Publish(new OnOptionsClickedEvent()));
    }
}