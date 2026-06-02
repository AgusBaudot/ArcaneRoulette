using UnityEngine;

namespace World 
{
    public class LineOfSight
    {
        private Transform _origin;
        private float _range;
        private LayerMask _obsMask;
        public void Init(Transform origin, float range, LayerMask obsMask)
        {
            _origin = origin;
            _range = range;
            _obsMask = obsMask;
        }

        public bool CheckRange(Transform target)
        {
            float distanceToTarget = (target.position - _origin.position).sqrMagnitude;
            return distanceToTarget <= _range * _range;
        }

        public bool CheckView(Transform target)
        {
            Vector3 dirToTarget = target.position - _origin.position;
            return !Physics.Raycast(_origin.position, dirToTarget.normalized, dirToTarget.magnitude, _obsMask);
        }
    }

}
