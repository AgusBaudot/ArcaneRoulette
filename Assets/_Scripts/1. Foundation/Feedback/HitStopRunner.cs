using System.Collections;
using UnityEngine;

namespace Foundation
{
    public static class HitStop
    {
        private const float AUTHORING_FPS = 60f;

        /// <summary>1 during the slow-mo hold, then eases 1 -> 0 over the ramp. 0 otherwise.</summary>
        public static float RoomClearIntensity { get; private set; }

        /// <summary>Fired once at the moment the room-clear slow-mo begins.</summary>
        public static event System.Action RoomClearStarted;

        private static HitStopRunner _runner;
        private static Coroutine _current;
        private static bool _isRoomClearActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _runner = null;
            _current = null;
            _isRoomClearActive = false;
            RoomClearIntensity = 0f;
            RoomClearStarted = null;
        }

        public static void Apply(int frames, float slowScale = 0f)
        {
            if (frames <= 0 || _isRoomClearActive)
                return;

            EnsureRunner();
            if (_current != null)
            {
                _runner.StopCoroutine(_current);
            }

            _current = _runner.StartCoroutine(Run(frames, slowScale));
        }

        public static void PlayRoomClearSlowMo(float targetScale = 0.1f, float holdDurationRealtime = 0.4f, float rampDurationRealtime = 0.6f)
        {
            EnsureRunner();
            if (_current != null)
            {
                _runner.StopCoroutine(_current);
            }

            _current = _runner.StartCoroutine(RoomClearRoutine(targetScale, holdDurationRealtime, rampDurationRealtime));
        }

        private static IEnumerator Run(int frames, float slowScale)
        {
            Time.timeScale = slowScale;

            float duration = frames / AUTHORING_FPS;
            yield return new WaitForSecondsRealtime(duration);

            if (Helpers.Input.PlayerActions != null)
            {
                Time.timeScale = 1f;
            }

            _current = null;
        }

        private static IEnumerator RoomClearRoutine(float targetScale, float holdDuration, float rampDuration)
        {
            _isRoomClearActive = true;
            Time.timeScale = targetScale;

            RoomClearIntensity = 1f;
            RoomClearStarted?.Invoke();

            yield return new WaitForSecondsRealtime(holdDuration);

            float elapsed = 0f;
            while (elapsed < rampDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rampDuration);

                RoomClearIntensity = 1f - t;

                if (Helpers.Input.PlayerActions != null)
                {
                    Time.timeScale = Mathf.Lerp(targetScale, 1f, t);
                }
                yield return null;
            }

            if (Helpers.Input.PlayerActions != null)
            {
                Time.timeScale = 1f;
            }

            RoomClearIntensity = 0f;
            _isRoomClearActive = false;
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

    internal class HitStopRunner : MonoBehaviour {}
}