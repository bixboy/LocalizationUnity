using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;
using Metroma.CameraTool.Modifiers;

namespace Metroma.CameraTool.Timeline
{
    [Serializable]
    [DisplayName("Camera/⏳ Slow Motion")]
    [CustomStyle("CameraEventMarker")]
    public class CameraSlowMotionMarker : CameraMarkerBase
    {
        [Range(0.01f, 1f)]
        [Tooltip("The TimeScale to reach (0.1 = 10x slower).")]
        public float targetTimeScale = 0.2f;

        [Tooltip("Total duration of the slow-motion effect in real-time seconds.")]
        public float duration = 1.5f;

        [Tooltip("Defines the TimeScale profile: 0 = normal speed (1.0), 1 = slow speed (targetTimeScale).")]
        public AnimationCurve profileCurve = new AnimationCurve(
            new Keyframe(0, 0), 
            new Keyframe(0.2f, 1f), 
            new Keyframe(0.8f, 1f), 
            new Keyframe(1, 0)
        );

        public override void Execute(CameraTool tool)
        {
            if (tool != null)
                CameraTimeHandler.DoSlowMotion(tool, targetTimeScale, duration, profileCurve);
        }
    }
}
