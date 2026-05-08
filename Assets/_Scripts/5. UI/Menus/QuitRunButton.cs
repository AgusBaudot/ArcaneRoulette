using UnityEngine;
using UnityEngine.UI;
using Foundation;

public class QuitRunButton : MonoBehaviour
{
     private void Start() => GetComponent<Button>().onClick.AddListener(
            () => EventBus.Publish(new OnRunQuitEvent()));
}
