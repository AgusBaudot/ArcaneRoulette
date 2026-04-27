using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ExitButton : MonoBehaviour
    {
        private void Start() => GetComponent<Button>().onClick.AddListener(
            () => EventBus.Publish(new OnExitClickedEvent()));
    }
}