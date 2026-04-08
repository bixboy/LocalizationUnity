using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Metroma.CameraTool.Timeline
{
    /// <summary>
    /// Runtime data for a single CameraTool clip on the Timeline.
    /// Holds the start/end spline progress, an easing curve, and a LookAt weight.
    /// </summary>
    [Serializable]
    public class CameraToolBehaviour : PlayableBehaviour
    {
        [Range(0f, 1f)]
        public float startProgress;

        [Range(0f, 1f)]
        public float endProgress = 1f;

        public AnimationCurve easingCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Range(0f, 1f)]
        [Tooltip("Blend between spline rotation (0) and LookAt target (1) for this clip.")]
        public float lookAtWeight = 1f;
    }
}
