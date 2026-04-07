using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;

namespace Metroma.Camera.Timeline
{
    [Serializable]
    [DisplayName("Camera/🎬 Pose Transition")]
    [CustomStyle("CameraEventMarker")]
    public class CameraPoseMarker : CameraMarkerBase
    {
        [Header("Target Pose")]
        public Vector3 position;
        public Vector3 rotationEuler;
        public float fov = 60f;
        public float distance = 0f;

        public float duration = 1.0f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public DirectorAction action = DirectorAction.None;

        public override void Execute(CameraTool tool)
        {
            if (tool == null) return;

            CameraPose target = new CameraPose
            {
                position = this.position,
                rotation = Quaternion.Euler(rotationEuler),
                fov = this.fov,
                distance = this.distance
            };

            tool.TransitionToPose(target, duration, curve, action);
        }
    }
}
