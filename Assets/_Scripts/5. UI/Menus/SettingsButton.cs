using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingsButton : MonoBehaviour
    {
        private void Start() => GetComponent<Button>().onClick.AddListener(
            () => EventBus.Publish(new OnSettingsClickedEvent()));
    }
}