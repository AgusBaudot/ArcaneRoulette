using UnityEngine;
using System.Collections;

public class ExplodingObject : MonoBehaviour
{
    [Header("Times")]
    public float countdownTimer = 10f; // Tiempo de espera antes de explotar
    public float sinkDelay = 0.5f;     // Segundos que espera en el piso antes de hundirse

    [Header("Force and Gravity")]
    public float explosionForce = 400f;
    public float explosionRadius = 5f;
    public float extraGravity = 50f;   // Gravedad extra para que caiga rápido

    [Header("Disappearing Effeccts")]
    public float airShrinkSpeed = 2f;  // Velocidad a la que se encogen mientras vuelan
    public float sinkDepth = 1.5f;     // Profundidad que se hunde en el piso
    public float sinkSpeed = 6f;       // Velocidad a la que atraviesa el suelo

    private Rigidbody[] fragments;

    void Awake()
    {
        fragments = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in fragments)
        {
            rb.isKinematic = true;
        }
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(countdownTimer);
        Explode();
    }

    void Explode()
    {
        Vector3 explosionPos = transform.position;

        foreach (Rigidbody rb in fragments)
        {
            rb.isKinematic = false;
            rb.AddExplosionForce(explosionForce, explosionPos, explosionRadius);
            rb.AddTorque(Random.insideUnitSphere * 40f, ForceMode.Impulse);

            FragmentBehavior piece = rb.gameObject.AddComponent<FragmentBehavior>();
            piece.Setup(extraGravity, airShrinkSpeed, sinkDelay, sinkDepth, sinkSpeed);
        }
    }
}

public class FragmentBehavior : MonoBehaviour
{
    private float extraGravity;
    private float airShrinkSpeed;
    private float sinkDelay;
    private float sinkDepth;
    private float sinkSpeed;

    private Rigidbody rb;
    private bool hasLanded = false;

    public void Setup(float gravity, float shrinkSpeedAir, float delay, float depth, float speed)
    {
        extraGravity = gravity;
        airShrinkSpeed = shrinkSpeedAir;
        sinkDelay = delay;
        sinkDepth = depth;
        sinkSpeed = speed;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Mientras esté en el aire volando, se achica progresivamente
        if (!hasLanded)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * airShrinkSpeed);
        }
    }

    void FixedUpdate()
    {
        // Gravedad pesada en el aire
        if (!hasLanded && rb != null && !rb.isKinematic)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // En cuanto toca el piso, detiene el achicamiento aéreo y programa el hundimiento
        if (!hasLanded)
        {
            hasLanded = true;
            StartCoroutine(SinkAndDestroy());
        }
    }

    IEnumerator SinkAndDestroy()
    {
        // Espera en el piso con la escala reducida que le haya quedado al caer
        yield return new WaitForSeconds(sinkDelay);

        // Apaga colisiones y física para atravesar el piso
        rb.isKinematic = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (Vector3.down * sinkDepth);
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * sinkSpeed;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}   