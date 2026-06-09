using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class Wait : IStrategy
    {
        Func<float> _duration;
        private float _timer;
        private bool _started = false;
        public Wait( Func<float> duration)
        {
            _duration = duration;
        }
        public Node.NodeState Process()
        {
            if (!_started)
            {
                _timer = _duration();
                _started = true;
            }
            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return Node.NodeState.Running;

            return Node.NodeState.Success;
        }
        public void Reset()
        {
            _started = false;
            _timer = 0f;
        }
    }
}


