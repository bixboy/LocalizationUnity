using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Metroma.Camera.Modifiers;

namespace Metroma.Camera.Timeline
{
    /// <summary>
    /// Timeline marker: changes the Field of View of the target camera.
    /// </summary>
    [Serializable]
    [DisplayName("Camera/📐 FOV Change")]
    [CustomStyle("CameraFOVMarker")]
    public class CameraFOVMarker : CameraMarkerBase
    {
        [Tooltip("Target Field of View value.")]
        [Range(10f, 120f)]
        [SerializeField] private float targetFOV = 60f;

        [Tooltip("Transition duration in seconds. 0 = instant.")]
        [Min(0f)]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("Easing curve for the FOV transition.")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float TargetFOV => targetFOV;
        public float Duration => duration;
        public AnimationCurve Curve => curve;

        public override void Execute(CameraTool tool)
        {
            if (tool.TargetCamera != null)
            {
                CameraModifiers.DoFOVTransition(tool.TargetCamera, targetFOV, duration, curve);
            }
        }
    }
}
