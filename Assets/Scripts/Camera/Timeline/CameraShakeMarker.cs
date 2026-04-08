using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Metroma.CameraTool.Modifiers;

namespace Metroma.CameraTool.Timeline
{
    /// <summary>
    /// Timeline marker: triggers a camera shake effect.
    /// </summary>
    [Serializable]
    [DisplayName("Camera/💥 Camera Shake")]
    [CustomStyle("CameraShakeMarker")]
    public class CameraShakeMarker : CameraMarkerBase
    {
        [Tooltip("Optional preset profile. If assigned, intensity and roughness below are ignored.")]
        [SerializeField] private CameraShakeProfile profile;

        [Tooltip("Max intensity of the shake (units).")]
        [Min(0f)]
        [SerializeField] private float intensity = 0.5f;

        [Tooltip("Shake duration in seconds.")]
        [Min(0.01f)]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Shake roughness (frequency). Higher = more jittery.")]
        [Min(0.01f)]
        [SerializeField] private float roughness = 1f;

        [Tooltip("Fade out the shake at the end of the duration.")]
        [SerializeField] private bool fadeOut = true;

        public float Intensity => intensity;
        public float Duration => duration;
        public float Roughness => roughness;
        public bool FadeOut => fadeOut;

        public override void Execute(CameraTool tool)
        {
            if (tool.TargetCamera != null)
            {
                if (profile != null)
                {
                    CameraModifiers.DoShake(tool.TargetCamera, profile, duration);
                }
                else
                {
                    CameraModifiers.DoShake(tool.TargetCamera, intensity, duration, roughness, fadeOut);
                }
            }
        }
    }
}
