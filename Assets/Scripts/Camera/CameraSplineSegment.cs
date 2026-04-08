using System;
using UnityEngine;

namespace Metroma.CameraTool
{

    [Serializable]
    public class CameraSplineSegment
    {
        [Tooltip("Auto-generated label: 'Node X → Y'")]
        public string label;

        [Tooltip("Duration in seconds for this segment.")]
        [Min(0.1f)]
        public float duration = 1f;

        [Tooltip("Easing curve for camera movement within this segment.")]
        public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Wait time (seconds) at the destination node before moving to the next segment.")]
        [Min(0f)]
        public float waitAtEnd;
    }
}
