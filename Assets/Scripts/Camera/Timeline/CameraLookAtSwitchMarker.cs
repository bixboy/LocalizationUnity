using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Metroma.Camera.Timeline
{
    /// <summary>
    /// Timeline marker: switches the LookAt target to another Transform
    /// from the CameraTool's lookAtTargets list.
    /// </summary>
    [Serializable]
    [DisplayName("Camera/🎯 LookAt Switch")]
    [CustomStyle("CameraLookAtSwitchMarker")]
    public class CameraLookAtSwitchMarker : CameraMarkerBase
    {
        [Tooltip("Index into CameraTool.lookAtTargets list. -1 = disable LookAt.")]
        [SerializeField] private int targetIndex;

        [Tooltip("Transition duration in seconds. 0 = instant.")]
        [Min(0f)]
        [SerializeField] private float transitionDuration = 0.5f;

        public int TargetIndex => targetIndex;
        public float TransitionDuration => transitionDuration;

        public override void Execute(CameraTool tool)
        {
            tool.HandleLookAtSwitch(this);
        }
    }
}
