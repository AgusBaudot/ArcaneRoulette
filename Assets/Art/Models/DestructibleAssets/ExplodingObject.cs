using UnityEngine;
using System.Collections;
using Foundation;

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
        [Tooltip("Controls how quickly the fragments scale down to 60% of their original size while they are actively flying through the air immediately after the break.")]
        public float airShrinkSpeed = 1.5f;
        [Tooltip("Controls the rapid interpolation from the piece's current size down to exactly zero the moment it physically collider with the floor, right before the object is deleted.")]
        public float groundShrinkSpeed = 5f;
        [Tooltip("Forces shards to shrink and delete after this many seconds, even if they never hit the ground.")]
        public float failsafeDestroyTime = 3f;

        private Rigidbody[] fragments;

        void Awake()
        {
            fragments = GetComponentsInChildren<Rigidbody>(true);

            foreach (Rigidbody rb in fragments)
            {
                rb.isKinematic = true;
            }
        }

        public void TriggerExplosion(Vector3 hitPosition)
        {
            StartCoroutine(ExplosionRoutine(hitPosition));
        }
        
        public void TriggerCrumble()
        {
            StartCoroutine(CrumbleRoutine());
        }

        private IEnumerator ExplosionRoutine(Vector3 hitPosition)
        {
            if (countdownTimer > 0f)
            {
                yield return CoroutineUtils.GetWait(countdownTimer);
            }

            foreach (Rigidbody rb in fragments)
            {
                rb.isKinematic = false;
                
                rb.AddExplosionForce(explosionForce, hitPosition, explosionRadius);
                rb.AddTorque(Random.insideUnitSphere * 40f, ForceMode.Impulse);

                FragmentBehavior piece = rb.gameObject.AddComponent<FragmentBehavior>();
                piece.Setup(extraGravity, airShrinkSpeed, groundShrinkSpeed, minAirTime, failsafeDestroyTime);
            }
        }

        private IEnumerator CrumbleRoutine()
        {
            if (countdownTimer > 0f)
            {
                yield return CoroutineUtils.GetWait(countdownTimer);
            }

            foreach (Rigidbody rb in fragments)
            {
                rb.isKinematic = false;
                
                rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

                FragmentBehavior piece = rb.gameObject.AddComponent<FragmentBehavior>();
                piece.Setup(extraGravity, airShrinkSpeed, groundShrinkSpeed, minAirTime, failsafeDestroyTime);
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

        public void Setup(float gravity, float shrinkAir, float shrinkGround, float airTime, float maxLife)
        {
            extraGravity = gravity;
            airShrinkSpeed = shrinkAir;
            groundShrinkSpeed = shrinkGround;
            minAirTime = airTime;
            rb = GetComponent<Rigidbody>();

            spawnTime = Time.time;
            targetAirScale = transform.localScale * 0.6f;
            
            StartCoroutine(FailsafeRoutine(maxLife));
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

        private IEnumerator FailsafeRoutine(float maxLife)
        {
            yield return CoroutineUtils.GetWait(maxLife);
            
            if (!hasLanded)
            {
                hasLanded = true;
                StartCoroutine(FinishShrinkingAndDestroy());
            }
        }

        private IEnumerator FinishShrinkingAndDestroy()
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