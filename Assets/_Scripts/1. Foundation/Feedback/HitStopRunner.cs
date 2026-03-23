using System.Collections;
using UnityEngine;

namespace Foundation
{
    public static class HitStop
    {
        private static HitStopRunner _runner;
        private static Coroutine _current;

        public static void Apply(float duration, float slowScale = 0f)
        {
            EnsureRunner();
            if (_current != null) _runner.StopCoroutine(_current);
            _current = _runner.StartCoroutine(Run(duration, slowScale));
        }

        private static IEnumerator Run(float duration, float slowScale)
        {
            Time.timeScale = slowScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _current = null;
        }

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("[HitStop]");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<HitStopRunner>();
        }
    }
    
    //Exists solely to own coroutines. Internal, never references directly.
    internal class HitStopRunner : MonoBehaviour {}
}
