using UnityEngine;
using Foundation;
using UnityEngine.UI;

public class ResumeButton : MonoBehaviour
{
    private void Start() => GetComponent<Button>().onClick.AddListener(
            () => EventBus.Publish(new OnGameResumedEvent()));
}
