using UnityEngine;
using System.Collections;

public class ExplodingObject : MonoBehaviour
{
    [Header("Times")]
    public float countdownTimer = 3f;   // Tiempo de espera antes de explotar
    public float minAirTime = 0.2f;     // Tiempo mínimo de vuelo obligatorio (evita autocolisión)

    [Header("Force and Gravity")]
    public float explosionForce = 300f; // Fuerza para separar los fragmentos
    public float explosionRadius = 3f;
    public float extraGravity = 25f;    // Gravedad moderada para darle tiro parabólico

    [Header("Disappearing Effects")]
    public float airShrinkSpeed = 1.5f; // Velocidad para reducir el 40% en el aire
    public float groundShrinkSpeed = 5f; // Velocidad de desintegración al tocar el piso

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
            piece.Setup(extraGravity, airShrinkSpeed, groundShrinkSpeed, minAirTime);
        }
    }
}

public class FragmentBehavior : MonoBehaviour
{
    private float extraGravity;
    private float airShrinkSpeed;
    private float groundShrinkSpeed;
    private float minAirTime;

    private Rigidbody rb;
    private Vector3 targetAirScale;
    private bool hasLanded = false;
    private float spawnTime;

    public void Setup(float gravity, float shrinkAir, float shrinkGround, float airTime)
    {
        extraGravity = gravity;
        airShrinkSpeed = shrinkAir;
        groundShrinkSpeed = shrinkGround;
        minAirTime = airTime;
        rb = GetComponent<Rigidbody>();

        spawnTime = Time.time;
        // Escala objetivo en aire (60% del tamaño original = reducción del 40%)
        targetAirScale = transform.localScale * 0.6f;
    }

    void Update()
    {
        // Se reduce hasta el 60% únicamente mientras vuela
        if (!hasLanded)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetAirScale, Time.deltaTime * airShrinkSpeed);
        }
    }

    void FixedUpdate()
    {
        if (!hasLanded && rb != null && !rb.isKinematic)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Ignora cualquier choque que ocurra dentro del tiempo de inmunidad inicial (choque entre piezas)
        if (Time.time - spawnTime < minAirTime) return;

        if (!hasLanded)
        {
            hasLanded = true;
            StartCoroutine(FinishShrinkingAndDestroy());
        }
    }

    IEnumerator FinishShrinkingAndDestroy()
    {
        Vector3 impactScale = transform.localScale;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * groundShrinkSpeed;
            transform.localScale = Vector3.Lerp(impactScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}