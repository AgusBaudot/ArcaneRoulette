using UnityEngine;
using System.Collections; // Required for IEnumerator

namespace World
{
    public class ExplodingObject : MonoBehaviour
    {
        [Header("Times")]
        public float countdownTimer = 0f;
        public float minAirTime = 0.2f;

        [Header("Force and Gravity")]
        public float explosionForce = 300f;
        public float explosionRadius = 3f;
        public float extraGravity = 25f;

        [Header("Disappearing Effects")]
        public float airShrinkSpeed = 1.5f;
        public float groundShrinkSpeed = 5f;

        private Rigidbody[] fragments;

        void Awake()
        {
            fragments = GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rb in fragments)
            {
                rb.isKinematic = true;
            }
        }

        public void TriggerExplosion(Vector3 hitPosition)
        {
            StartCoroutine(ExplosionRoutine(hitPosition));
        }

        private IEnumerator ExplosionRoutine(Vector3 hitPosition)
        {
            if (countdownTimer > 0f)
            {
                // We use our existing custom YieldInstruction cache if possible, 
                // but standard WaitForSeconds is fine here for prototyping.
                yield return new WaitForSeconds(countdownTimer);
            }

            foreach (Rigidbody rb in fragments)
            {
                rb.isKinematic = false;
                
                // Push fragments away from the actual hit position!
                rb.AddExplosionForce(explosionForce, hitPosition, explosionRadius);
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
            targetAirScale = transform.localScale * 0.6f;
        }

        void Update()
        {
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
}