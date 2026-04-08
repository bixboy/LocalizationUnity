using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;
using Metroma.CameraTool.Modifiers;

namespace Metroma.CameraTool.Timeline
{
    [Serializable]
    [DisplayName("Camera/📍 Temporary Offset")]
    [CustomStyle("CameraEventMarker")]
    public class CameraOffsetMarker : CameraMarkerBase
    {
        [Header("Movement (Phase 1)")]
        [Tooltip("Local position offset to apply.")]
        public Vector3 positionOffset;

        [Tooltip("Local rotation offset (Euler) to apply.")]
        public Vector3 rotationOffset;

        [Tooltip("How long the offset lasts before starting the return phase.")]
        public float duration = 1f;

        [Tooltip("Curve for the initial movement (0 to 1).")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Return (Phase 2)")]
        [Tooltip("How long it takes to return to the original position once Phase 1 ends.")]
        public float returnDuration = 0.4f;

        [Tooltip("Curve for the return movement.")]
        public AnimationCurve returnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("If true, the returnCurve value will be inverted (1 - value). This allows using standard 0-1 Unity presets for the return phase.")]
        public bool invertReturnCurve = true;

        public override void Execute(CameraTool tool)
        {
            if (tool.TargetCamera != null)
            {
                if (positionOffset.sqrMagnitude > 0.0001f)
                    tool.TargetCamera.AddPositionOffset(positionOffset, duration, curve, returnDuration, returnCurve, invertReturnCurve);
                
                if (rotationOffset.sqrMagnitude > 0.0001f)
                    tool.TargetCamera.AddRotationOffset(rotationOffset, duration, curve, returnDuration, returnCurve, invertReturnCurve);
            }
        }
    }
}
