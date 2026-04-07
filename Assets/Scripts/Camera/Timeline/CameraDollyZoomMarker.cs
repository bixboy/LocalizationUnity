using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;
using Metroma.Camera.Modifiers;

namespace Metroma.Camera.Timeline
{
    [Serializable]
    [DisplayName("Camera/🎥 Dolly Zoom (Vertigo)")]
    [CustomStyle("CameraDollyZoomMarker")]
    public class CameraDollyZoomMarker : CameraMarkerBase
    {
        [Tooltip("Optional preset profile. If assigned, the curve below is ignored.")]
        [SerializeField] private CameraDollyProfile profile;

        [Tooltip("The camera will move forward by this amount (+ for push, - for pull).")]
        public float pushDistance = 5f;
        
        [Tooltip("The Field of View the camera will reach at the end.")]
        public float targetFOV = 90f;

        [Tooltip("Transition duration.")]
        public float duration = 2f;

        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public override void Execute(CameraTool tool)
        {
            if (tool.TargetCamera != null)
            {
                if (profile != null)
                    CameraModifiers.DoDollyZoom(tool.TargetCamera, pushDistance, targetFOV, duration, profile);
                else
                    CameraModifiers.DoDollyZoom(tool.TargetCamera, pushDistance, targetFOV, duration, curve);
            }
        }
    }
}
