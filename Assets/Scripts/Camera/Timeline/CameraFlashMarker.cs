using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;
using Metroma.Camera.Modifiers;

namespace Metroma.Camera.Timeline
{
    /// <summary>
    /// Timeline marker: triggers a momentary screen flash.
    /// Demonstrates the simplicity of the new Marker system.
    /// </summary>
    [Serializable]
    [DisplayName("Camera/🎬 Screen Flash")]
    [CustomStyle("CameraFlashMarker")]
    public class CameraFlashMarker : CameraMarkerBase
    {
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float duration = 0.5f;

        public override void Execute(CameraTool tool)
        {
            if (tool.TargetCamera != null)
            {
                CameraModifiers.DoFlash(tool.TargetCamera, flashColor, duration);
            }
        }
    }
}
