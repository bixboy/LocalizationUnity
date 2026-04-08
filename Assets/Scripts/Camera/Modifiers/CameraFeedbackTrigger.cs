using UnityEngine;

namespace Metroma.CameraTool.Modifiers
{
    /// <summary>
    /// A "Fire & Forget" component for Gameplay Programmers to trigger camera feedback
    /// (shakes, flashes, etc.) from any gameplay event without writing custom camera logic.
    /// </summary>
    public class CameraFeedbackTrigger : MonoBehaviour
    {
        [Header("Shake")]
        public CameraShakeProfile shakeProfile;
        public float shakeDuration = 0.5f;

        [Header("Distance Attenuation")]
        public bool useDistanceFalloff = true;
        public float maxRadius = 10f;
        [Range(1f, 3f)] public float falloffPower = 1.5f;

        [Header("Visual Effects")]
        public bool triggerFlash;
        public Color flashColor = Color.white;
        public float flashDuration = 0.3f;

        /// <summary>
        /// Call this method from code (e.g., when an explosion occurs)
        /// or from a UnityEvent (e.g., OnCollisionEnter).
        /// </summary>
        public void Trigger()
        {
            var activeTool = CameraTool.Active;
            if (activeTool == null || activeTool.TargetCamera == null)
            {
                Debug.LogWarning($"[CameraFeedbackTrigger] No active CameraTool found to receive feedback.", this);
                return;
            }

            var cam = activeTool.TargetCamera;

            // 1. Shake
            if (useDistanceFalloff)
            {
                cam.DoShakeAt(transform.position, shakeProfile, shakeDuration, maxRadius, falloffPower);
            }
            else if (shakeProfile != null)
            {
                cam.DoShake(shakeProfile, shakeDuration);
            }

            // 2. Flash
            if (triggerFlash)
                CameraModifiers.DoFlash(cam, flashColor, flashDuration);
        }

        private void OnDrawGizmosSelected()
        {
            if (useDistanceFalloff)
            {
                Gizmos.color = new Color(1f, 0.4f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, maxRadius);
            }
        }
    }
}
