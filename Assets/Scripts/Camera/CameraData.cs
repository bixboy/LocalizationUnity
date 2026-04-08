using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;

namespace Metroma.CameraTool
{
    /// <summary> Defines a static camera view with position, rotation, FOV and distance offset. </summary>
    [System.Serializable]
    public struct CameraPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fov;
        public float distance;

        public static CameraPose Lerp(CameraPose a, CameraPose b, float t)
        {
            return new CameraPose
            {
                position = Vector3.LerpUnclamped(a.position, b.position, t),
                rotation = Quaternion.SlerpUnclamped(a.rotation, b.rotation, t),
                fov = Mathf.LerpUnclamped(a.fov, b.fov, t),
                distance = Mathf.LerpUnclamped(a.distance, b.distance, t)
            };
        }
    }

    /// <summary> Defines a cinematic chapter with its own Timeline and pacing. </summary>
    [System.Serializable]
    public class CameraChapter
    {
        public string name;
        public TimelineAsset timeline;
        
        [Tooltip("The rail index where this chapter starts its journey.")]
        public int startRailIndex = 0;
        
        [Tooltip("Custom color for this chapter's gizmos.")]
        public Color debugColor = Color.cyan;

        [HideInInspector]
        public bool isExpanded = true;

        /// <summary> List of pacing segments specific to this chapter. </summary>
        public List<CameraSplineSegment> segments = new List<CameraSplineSegment>();
    }

    /// <summary> Current state of the camera controller. </summary>
    public enum CameraState
    {
        FollowRail,      // Normal movement along spline
        Transitioning,   // Blending from current to a StaticPose
        StaticPose,      // Holding on a static position (mini-game)
        ReturningToRail  // Blending from a StaticPose back to spline
    }

    /// <summary> Action to perform on the Timeline director when reaching a transition end. </summary>
    public enum DirectorAction
    {
        None,   // Keep playing (or keep paused if already paused)
        Pause,  // Pause at current frame
        Stop    // Stop and unload (recommended for chapter changes)
    }
}
