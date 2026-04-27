using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayButton : MonoBehaviour
    {
        private void Start() => GetComponent<Button>().onClick.AddListener(
            () => EventBus.Publish(new OnPlayClickedEvent()));
    }
}