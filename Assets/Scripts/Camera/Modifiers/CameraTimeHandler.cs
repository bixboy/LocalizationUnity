using UnityEngine;
using System.Collections;

namespace Metroma.Camera.Modifiers
{
    /// <summary>
    /// Professional time management service for cinematic effects.
    /// Handles smooth Time.timeScale transitions independently of the game clock.
    /// </summary>
    public static class CameraTimeHandler
    {
        public static event System.Action<bool, float> OnSlowMoStateChanged;

        private static Coroutine _activeCoroutine;

        /// <summary>
        /// Triggers a slow-motion phase using a curve to define the timeScale profile over time.
        /// </summary>
        /// <param name="host">The MonoBehaviour to run the coroutine on (e.g. CameraTool.Active).</param>
        /// <param name="targetScale">The minimum Time.timeScale reached at curve value 1.0.</param>
        /// <param name="duration">Total length of the effect in real-time seconds.</param>
        /// <param name="profileCurve">Curve where 0.0 = Time.timeScale 1.0, and 1.0 = targetScale.</param>
        public static void DoSlowMotion(MonoBehaviour host, float targetScale, float duration, AnimationCurve profileCurve)
        {
            if (host == null) return;
            if (_activeCoroutine != null) host.StopCoroutine(_activeCoroutine);
            _activeCoroutine = host.StartCoroutine(SlowMotionRoutine(targetScale, duration, profileCurve));
        }

        private static IEnumerator SlowMotionRoutine(float targetScale, float duration, AnimationCurve profileCurve)
        {
            float time = 0f;
            OnSlowMoStateChanged?.Invoke(true, 1.0f);

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                
                float curveImpact = profileCurve != null ? profileCurve.Evaluate(t) : 0f;
                float currentScale = Mathf.Lerp(1.0f, targetScale, curveImpact);
                Time.timeScale = currentScale;
                
                yield return null;
            }

            Time.timeScale = 1.0f;
            _activeCoroutine = null;
            OnSlowMoStateChanged?.Invoke(false, 1.0f);
        }

        /// <summary> Forcefully restores the time scale to normal. </summary>
        public static void ResetTimeScale()
        {
            Time.timeScale = 1.0f;
        }
    }
}
