using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;
using Metroma.Camera.Modifiers;

namespace Metroma.Camera.Timeline
{
    [Serializable]
    [DisplayName("Camera/🎯 Auto-Focus Toggle")]
    [CustomStyle("CameraEventMarker")]
    public class CameraAutoFocusMarker : CameraMarkerBase
    {
        [Tooltip("Enable or disable real-time Depth of Field sync to the current target.")]
        public bool active = true;

        public override void Execute(CameraTool tool)
        {
            if (tool.TargetCamera != null)
            {
                CameraModifiers.SetAutoFocus(tool.TargetCamera, active, tool.CurrentLookAtTarget);
            }
        }
    }
}
