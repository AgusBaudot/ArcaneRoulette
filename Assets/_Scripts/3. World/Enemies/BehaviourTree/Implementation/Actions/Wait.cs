using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public class Wait : IStrategy
    {
        private float _duration;
        private float _timer;
        private bool _started = false;
        public Wait( float duration)
        {
            _duration = duration;
        }
        public NodeState Process()
        {
            if (!_started)
            {
                _timer = _duration;
                _started = true;
            }
            _timer -= Time.deltaTime;
            if (_timer > 0f)
                return NodeState.Running;

            return NodeState.Success;
        }
        public void Reset()
        {
            _started = false;
            _timer = 0f;
        }
    }
}


