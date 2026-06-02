using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class LineOfSight : MonoBehaviour
    {
        public Transform reference;
        public float range;
        public LayerMask obsMask;
        public bool CheckRange(Transform target)
        {
            float distanceToTarget = (target.position - Origin).sqrMagnitude;
            return distanceToTarget <= range * range;
        }
        public bool CheckView(Transform target)
        {
            Vector3 dirToTarget = target.position - Origin;
            return !Physics.Raycast(Origin, dirToTarget.normalized, dirToTarget.magnitude, obsMask);
        }
        Vector3 Origin
        {
            get
            {
                if (reference == null) return transform.position;
                return reference.position;
            }
        }
        private void OnDrawGizmos()
        {
            Color myColor = Color.red;
            myColor.a = 0.5f;
            Gizmos.color = myColor;
            Gizmos.DrawWireSphere(Origin, range);
        }
    }

}
