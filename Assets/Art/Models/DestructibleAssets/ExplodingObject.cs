using UnityEngine;
using System.Collections;

public class ExplodingObject : MonoBehaviour
{
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    public float shrinkDelay = 0.5f; // Tiempo antes de empezar a desaparecer
    public float shrinkSpeed = 5f;  // Velocidad a la que se encogen

    void Start()
    {
        Vector3 explosionPos = transform.position;
        Rigidbody[] fragments = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in fragments)
        {
            // Aplica el estallido instantáneo tipo cartoon
            rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius);

            // Inicia la rutina para desaparecer cada pieza
            StartCoroutine(ShrinkAndDestroy(rb.gameObject));
        }
    }

    IEnumerator ShrinkAndDestroy(GameObject fragment)
    {
        yield return new WaitForSeconds(shrinkDelay);

        Vector3 originalScale = fragment.transform.localScale;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * shrinkSpeed;
            fragment.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(fragment); // Elimina la pieza de la memoria
    }
}