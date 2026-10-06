using System.Collections;
using UnityEngine;

namespace Foundation
{
    public static class HitStop
    {
        private const float AUTHORING_FPS = 60f; 
        
        private static HitStopRunner _runner;
        private static Coroutine _current;

        public static void Apply(int frames, float slowScale = 0f)
        {
            if (frames <= 0)
                return;
            
            EnsureRunner();
            if (_current != null)
            {
                _runner.StopCoroutine(_current);
            }
            
            _current = _runner.StartCoroutine(Run(frames, slowScale));
        }

        private static IEnumerator Run(int frames, float slowScale)
        {
            Time.timeScale = slowScale;
            
            // Translate the authored 60FPS frames into real-time seconds.
            // This guarantees the pause feels mathematically identical at 30hz, 60hz, or 144hz.
            float duration = frames / AUTHORING_FPS;
            yield return new WaitForSecondsRealtime(duration);

            if (Helpers.Input.PlayerActions)
            {
                Time.timeScale = 1f;
            }

            _current = null;
        }

        private static void EnsureRunner()
        {
            if (_runner != null) 
                return;
            
            var go = new GameObject("[HitStop]");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<HitStopRunner>();
        }
    }
    
    //Exists solely to own coroutines. Internal, never references directly.
    internal class HitStopRunner : MonoBehaviour {}
}