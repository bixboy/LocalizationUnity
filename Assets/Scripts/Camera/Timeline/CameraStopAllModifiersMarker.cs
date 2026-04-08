using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Metroma.CameraTool.Modifiers;

namespace Metroma.CameraTool.Timeline
{
    [Serializable]
    [DisplayName("Camera/🛑 Stop all modifiers")]
    [CustomStyle("CameraStopAllMarker")]
    public class CameraStopAllModifiersMarker : CameraMarkerBase
    {
        public override void Execute(CameraTool tool)
        {
            if (tool.TargetCamera != null)
            {
                CameraModifiers.StopAllCameraModifiers(tool.TargetCamera);
            }
        }
    }
}
