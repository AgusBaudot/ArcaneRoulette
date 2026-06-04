using UnityEngine;
using Foundation;
using Core;

public class Portal : MonoBehaviour
{
    [SerializeField] private float _radius;

    private void Start()
    {
        var col = GetComponent<SphereCollider>();
        col.radius = _radius / 2;
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<PlayerHurtBox>(out _))
            return;

        Debug.LogWarning("Clean this up later.");
        EventBus.Publish(new StartRunEvent());
        UnityEngine.SceneManagement.SceneManager.LoadScene("Core loop");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent<PlayerHurtBox>(out _))
            return;

        //Leave this here in case the player has to press an input to enter.
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
